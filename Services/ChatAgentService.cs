using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using BoardGameLeague.Models;
using Microsoft.EntityFrameworkCore;

namespace BoardGameLeague.Services
{
    // Drives a bounded Gemini function-calling loop: offers a role-scoped set of tools,
    // executes whatever the model asks for directly against the DbContext (re-checking
    // permissions server-side regardless of which tools were offered), and feeds the
    // result back until Gemini produces a plain-text reply for the user.
    public class ChatAgentService : IChatAgentService
    {
        private const int MaxAgentIterations = 7;
        private const int MaxResultsPerCategory = 5;

        private readonly BoardGameLeagueDbContext _context;
        private readonly IGeminiClient _gemini;
        private readonly Microsoft.Extensions.Logging.ILogger<ChatAgentService> _logger;

        public ChatAgentService(BoardGameLeagueDbContext context, IGeminiClient gemini, Microsoft.Extensions.Logging.ILogger<ChatAgentService> logger)
        {
            _context = context;
            _gemini = gemini;
            _logger = logger;
        }

        public async Task<ChatResponse> HandleMessageAsync(ChatRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            var canManage = user.IsInRole("Admin") || user.IsInRole("Manager");
            var links = new List<ChatLinkDto>();
            var contents = BuildContents(request);
            var tools = new List<GeminiTool> { BuildToolset(canManage) };
            var systemInstruction = BuildSystemInstruction(canManage);

            for (var iteration = 0; iteration < MaxAgentIterations; iteration++)
            {
                var geminiRequest = new GeminiRequest
                {
                    Contents = contents,
                    Tools = tools,
                    SystemInstruction = systemInstruction,
                    GenerationConfig = new GeminiGenerationConfig()
                };

                GeminiCandidate? candidate;
                try
                {
                    candidate = await _gemini.GenerateAsync(geminiRequest, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Gemini call failed");
                    return new ChatResponse { Reply = "Sorry, I couldn't reach the assistant service right now. Please try again in a moment." };
                }

                if (candidate?.Content?.Parts == null || candidate.Content.Parts.Count == 0)
                {
                    _logger.LogWarning(
                        "Gemini returned no usable parts on iteration {Iteration}. FinishReason={FinishReason}",
                        iteration, candidate?.FinishReason ?? "(no candidate)");
                    return new ChatResponse { Reply = "Sorry, I didn't get a usable response. Could you rephrase that?" };
                }

                var functionCallPart = candidate.Content.Parts.FirstOrDefault(p => p.FunctionCall != null);
                if (functionCallPart?.FunctionCall != null)
                {
                    contents.Add(new GeminiContent { Role = "model", Parts = new List<GeminiPart> { functionCallPart } });

                    var (responseJson, callLinks) = await ExecuteFunctionAsync(functionCallPart.FunctionCall, canManage, cancellationToken);
                    links.AddRange(callLinks);

                    contents.Add(new GeminiContent
                    {
                        Role = "function",
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart
                            {
                                FunctionResponse = new GeminiFunctionResponse
                                {
                                    Name = functionCallPart.FunctionCall.Name,
                                    Response = responseJson
                                }
                            }
                        }
                    });

                    continue;
                }

                var text = string.Join("\n", candidate.Content.Parts.Where(p => p.Text != null).Select(p => p.Text));
                return new ChatResponse
                {
                    Reply = string.IsNullOrWhiteSpace(text) ? "Done." : text.Trim(),
                    Links = links
                };
            }

            return new ChatResponse
            {
                Reply = "That request took too many steps, so I stopped to be safe. Could you try something simpler?",
                Links = links
            };
        }

        private static List<GeminiContent> BuildContents(ChatRequest request)
        {
            var contents = new List<GeminiContent>();
            foreach (var turn in request.History.TakeLast(20))
            {
                var role = turn.Role == "model" ? "model" : "user";
                contents.Add(new GeminiContent { Role = role, Parts = new List<GeminiPart> { new GeminiPart { Text = turn.Text } } });
            }
            contents.Add(new GeminiContent { Role = "user", Parts = new List<GeminiPart> { new GeminiPart { Text = request.Message } } });
            return contents;
        }

        private static GeminiContent BuildSystemInstruction(bool canManage)
        {
            var searchCapabilities =
                "search_league_data supports more than name matching: a location filter (city/country/region), " +
                "a date range or upcomingOnly for tournaments and matches, a board game category and player-count range, " +
                "player rating range, and status flags (tournament isOpen, match isCompleted, team isActive). Combine " +
                "whichever apply instead of only searching by name - e.g. for \"what's happening in Zagreb next month\" " +
                "use location plus a date range; for \"who's the next match\" use entityType=Match with upcomingOnly=true.";

            var text = canManage
                ? "You are the assistant embedded in BoardGameLeague, a board game tournament tracker. " +
                  "You can search existing players, teams, venues, board games, tournaments and matches, create new ones, " +
                  "and update existing ones (change a value, move a tournament to a different venue, rename something, " +
                  "correct a score, etc.) using the provided tools. Always use the tools rather than inventing data or ids. " +
                  searchCapabilities +
                  " Update tools identify the existing record by its current name (or, for matches, by team names and " +
                  "optionally the tournament) and only change the fields you actually pass - anything omitted is left as " +
                  "is. Use search_league_data first if you are not sure of the exact current name. After creating or " +
                  "updating something, briefly confirm what changed. If a tool call fails, explain why in plain language " +
                  "and suggest what to change. Keep answers short and conversational."
                : "You are the assistant embedded in BoardGameLeague, a board game tournament tracker. " +
                  "You can search existing players, teams, venues, board games, tournaments and matches using the provided " +
                  "tool, and point users to the right page. " + searchCapabilities +
                  " You cannot create, update or delete anything - this account's role does not have permission. If asked " +
                  "to create or change something, politely explain that and suggest asking an Admin or Manager. Keep " +
                  "answers short and conversational.";

            return new GeminiContent { Parts = new List<GeminiPart> { new GeminiPart { Text = text } } };
        }

        private static GeminiTool BuildToolset(bool canManage)
        {
            var declarations = new List<GeminiFunctionDeclaration>
            {
                new GeminiFunctionDeclaration
                {
                    Name = "search_league_data",
                    Description = "Search existing players, teams, venues, board games, tournaments and/or matches. " +
                                   "Combine a free-text name search with any of the optional filters below - only the " +
                                   "ones relevant to the chosen entityType are applied, the rest are ignored. Use this " +
                                   "for questions like \"what tournaments are in Zagreb\", \"matches next week\", " +
                                   "\"open tournaments\", \"strategy games for 4 players\", or \"top-rated players\".",
                    Parameters = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            query = new { type = "STRING", description = "Optional free-text match against the entity's name (or both team names for matches). Leave out to rely purely on the filters below." },
                            entityType = new
                            {
                                type = "STRING",
                                @enum = new[] { "Player", "Team", "Venue", "BoardGame", "Tournament", "Match", "Any" },
                                description = "Restrict to one entity type, or Any to search everything. Defaults to Any."
                            },
                            location = new
                            {
                                type = "STRING",
                                description = "City, country, or region. Matches a Venue's city/country (for Venue, Tournament and Match searches) or a Team's region (for Team searches)."
                            },
                            dateFrom = new { type = "STRING", description = "ISO date yyyy-MM-dd. For tournaments/matches, only include ones ending/occurring on or after this date." },
                            dateTo = new { type = "STRING", description = "ISO date yyyy-MM-dd. For tournaments/matches, only include ones starting/occurring on or before this date." },
                            upcomingOnly = new { type = "BOOLEAN", description = "For tournaments/matches, only include ones that haven't started yet (relative to now)." },
                            category = new
                            {
                                type = "STRING",
                                @enum = new[] { "Strategy", "Family", "Party", "Cooperative", "Thematic", "Abstract" },
                                description = "Board game category filter."
                            },
                            minPlayers = new { type = "INTEGER", description = "Board games supporting at least this many players." },
                            maxPlayers = new { type = "INTEGER", description = "Board games supporting at most this many players." },
                            minRating = new { type = "INTEGER", description = "Minimum player rating." },
                            maxRating = new { type = "INTEGER", description = "Maximum player rating." },
                            isCompleted = new { type = "BOOLEAN", description = "Filter matches by whether they're already completed (true) or upcoming/pending (false)." },
                            isOpen = new { type = "BOOLEAN", description = "Filter tournaments by whether registration is open." },
                            isActive = new { type = "BOOLEAN", description = "Filter teams by active status." }
                        }
                    }
                }
            };

            if (canManage)
            {
                declarations.Add(new GeminiFunctionDeclaration
                {
                    Name = "create_player",
                    Description = "Create a new player.",
                    Parameters = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            name = new { type = "STRING" },
                            rating = new { type = "INTEGER", description = "Skill rating 0-3000. Defaults to 1200." },
                            country = new { type = "STRING", description = "Country name or code. Defaults to N/A." },
                            role = new { type = "STRING", description = "e.g. Player, Captain, Rook. Defaults to Player." },
                            joinedDate = new { type = "STRING", description = "ISO date yyyy-MM-dd. Defaults to today." }
                        },
                        required = new[] { "name" }
                    }
                });

                declarations.Add(new GeminiFunctionDeclaration
                {
                    Name = "create_team",
                    Description = "Create a new team.",
                    Parameters = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            name = new { type = "STRING" },
                            region = new { type = "STRING" },
                            foundedDate = new { type = "STRING", description = "ISO date yyyy-MM-dd. Defaults to today." },
                            isActive = new { type = "BOOLEAN", description = "Defaults to true." }
                        },
                        required = new[] { "name", "region" }
                    }
                });

                declarations.Add(new GeminiFunctionDeclaration
                {
                    Name = "create_venue",
                    Description = "Create a new venue.",
                    Parameters = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            name = new { type = "STRING" },
                            city = new { type = "STRING" },
                            country = new { type = "STRING" },
                            capacity = new { type = "INTEGER" },
                            indoor = new { type = "BOOLEAN", description = "Defaults to true." }
                        },
                        required = new[] { "name", "city", "country", "capacity" }
                    }
                });

                declarations.Add(new GeminiFunctionDeclaration
                {
                    Name = "create_board_game",
                    Description = "Create a new board game.",
                    Parameters = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            name = new { type = "STRING" },
                            category = new
                            {
                                type = "STRING",
                                @enum = new[] { "Strategy", "Family", "Party", "Cooperative", "Thematic", "Abstract" }
                            },
                            minPlayers = new { type = "INTEGER" },
                            maxPlayers = new { type = "INTEGER" },
                            averagePlayTimeMinutes = new { type = "INTEGER", description = "Defaults to 60." },
                            complexity = new { type = "NUMBER", description = "0.1 to 5.0. Defaults to 2.0." }
                        },
                        required = new[] { "name", "category", "minPlayers", "maxPlayers" }
                    }
                });

                declarations.Add(new GeminiFunctionDeclaration
                {
                    Name = "create_tournament",
                    Description = "Create a new tournament at an existing venue. The venue must already exist - " +
                                   "use search_league_data to confirm its exact name first if unsure.",
                    Parameters = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            name = new { type = "STRING" },
                            description = new { type = "STRING" },
                            venueName = new { type = "STRING", description = "Name of an existing venue." },
                            startDate = new { type = "STRING", description = "ISO date yyyy-MM-dd. Defaults to a week from today." },
                            endDate = new { type = "STRING", description = "ISO date yyyy-MM-dd. Defaults to two days after startDate." },
                            isOpen = new { type = "BOOLEAN", description = "Whether registration is open. Defaults to true." }
                        },
                        required = new[] { "name", "venueName" }
                    }
                });

                declarations.Add(new GeminiFunctionDeclaration
                {
                    Name = "create_match",
                    Description = "Schedule a new match between two existing teams, for an existing tournament and board game. " +
                                   "All four must already exist - use search_league_data to confirm exact names first if unsure.",
                    Parameters = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            tournamentName = new { type = "STRING" },
                            teamAName = new { type = "STRING" },
                            teamBName = new { type = "STRING" },
                            gameName = new { type = "STRING" },
                            startTime = new { type = "STRING", description = "ISO date/time. Defaults to tomorrow 10:00." },
                            scoreA = new { type = "INTEGER", description = "Defaults to 0." },
                            scoreB = new { type = "INTEGER", description = "Defaults to 0." },
                            isCompleted = new { type = "BOOLEAN", description = "Defaults to false." }
                        },
                        required = new[] { "tournamentName", "teamAName", "teamBName", "gameName" }
                    }
                });

                declarations.Add(new GeminiFunctionDeclaration
                {
                    Name = "update_player",
                    Description = "Update fields on an existing player, identified by their current name. Only the " +
                                   "fields you pass are changed; anything omitted stays as is.",
                    Parameters = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            name = new { type = "STRING", description = "Current name of the player to update." },
                            newName = new { type = "STRING" },
                            rating = new { type = "INTEGER" },
                            country = new { type = "STRING" },
                            role = new { type = "STRING" },
                            joinedDate = new { type = "STRING", description = "ISO date yyyy-MM-dd." }
                        },
                        required = new[] { "name" }
                    }
                });

                declarations.Add(new GeminiFunctionDeclaration
                {
                    Name = "update_team",
                    Description = "Update fields on an existing team, identified by its current name. Only the fields " +
                                   "you pass are changed; anything omitted stays as is.",
                    Parameters = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            name = new { type = "STRING", description = "Current name of the team to update." },
                            newName = new { type = "STRING" },
                            region = new { type = "STRING" },
                            foundedDate = new { type = "STRING", description = "ISO date yyyy-MM-dd." },
                            isActive = new { type = "BOOLEAN" }
                        },
                        required = new[] { "name" }
                    }
                });

                declarations.Add(new GeminiFunctionDeclaration
                {
                    Name = "update_venue",
                    Description = "Update fields on an existing venue, identified by its current name. Only the fields " +
                                   "you pass are changed; anything omitted stays as is.",
                    Parameters = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            name = new { type = "STRING", description = "Current name of the venue to update." },
                            newName = new { type = "STRING" },
                            city = new { type = "STRING" },
                            country = new { type = "STRING" },
                            capacity = new { type = "INTEGER" },
                            indoor = new { type = "BOOLEAN" }
                        },
                        required = new[] { "name" }
                    }
                });

                declarations.Add(new GeminiFunctionDeclaration
                {
                    Name = "update_board_game",
                    Description = "Update fields on an existing board game, identified by its current name. Only the " +
                                   "fields you pass are changed; anything omitted stays as is.",
                    Parameters = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            name = new { type = "STRING", description = "Current name of the board game to update." },
                            newName = new { type = "STRING" },
                            category = new
                            {
                                type = "STRING",
                                @enum = new[] { "Strategy", "Family", "Party", "Cooperative", "Thematic", "Abstract" }
                            },
                            minPlayers = new { type = "INTEGER" },
                            maxPlayers = new { type = "INTEGER" },
                            averagePlayTimeMinutes = new { type = "INTEGER" },
                            complexity = new { type = "NUMBER", description = "0.1 to 5.0." }
                        },
                        required = new[] { "name" }
                    }
                });

                declarations.Add(new GeminiFunctionDeclaration
                {
                    Name = "update_tournament",
                    Description = "Update fields on an existing tournament, identified by its current name. Only the " +
                                   "fields you pass are changed; anything omitted stays as is. To move it to a different " +
                                   "venue, pass venueName - that venue must already exist.",
                    Parameters = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            name = new { type = "STRING", description = "Current name of the tournament to update." },
                            newName = new { type = "STRING" },
                            description = new { type = "STRING" },
                            venueName = new { type = "STRING", description = "Name of an existing venue to move the tournament to." },
                            startDate = new { type = "STRING", description = "ISO date yyyy-MM-dd." },
                            endDate = new { type = "STRING", description = "ISO date yyyy-MM-dd." },
                            isOpen = new { type = "BOOLEAN" }
                        },
                        required = new[] { "name" }
                    }
                });

                declarations.Add(new GeminiFunctionDeclaration
                {
                    Name = "update_match",
                    Description = "Update fields on an existing match. Identify it by teamAName and teamBName (order " +
                                   "doesn't matter) and, if there could be more than one match between those teams, " +
                                   "narrow it down with tournamentName. Only the fields you pass are changed. To move a " +
                                   "team, tournament, or game, pass newTeamAName/newTeamBName/newTournamentName/gameName " +
                                   "with the name of an existing one.",
                    Parameters = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            teamAName = new { type = "STRING", description = "One of the two teams in the match to update." },
                            teamBName = new { type = "STRING", description = "The other team in the match to update." },
                            tournamentName = new { type = "STRING", description = "Narrows down which match, if the same two teams played more than once." },
                            newTeamAName = new { type = "STRING", description = "Name of an existing team to change Team A to." },
                            newTeamBName = new { type = "STRING", description = "Name of an existing team to change Team B to." },
                            newTournamentName = new { type = "STRING", description = "Name of an existing tournament to move the match to." },
                            gameName = new { type = "STRING", description = "Name of an existing board game to change the match to." },
                            startTime = new { type = "STRING", description = "ISO date/time." },
                            scoreA = new { type = "INTEGER" },
                            scoreB = new { type = "INTEGER" },
                            isCompleted = new { type = "BOOLEAN" }
                        },
                        required = new[] { "teamAName", "teamBName" }
                    }
                });
            }

            return new GeminiTool { FunctionDeclarations = declarations };
        }

        private async Task<(object Response, List<ChatLinkDto> Links)> ExecuteFunctionAsync(GeminiFunctionCall call, bool canManage, CancellationToken ct)
        {
            try
            {
                switch (call.Name)
                {
                    case "search_league_data":
                        return await SearchAsync(call.Args, ct);
                    case "create_player":
                        return canManage ? await CreatePlayerAsync(call.Args, ct) : Denied();
                    case "create_team":
                        return canManage ? await CreateTeamAsync(call.Args, ct) : Denied();
                    case "create_venue":
                        return canManage ? await CreateVenueAsync(call.Args, ct) : Denied();
                    case "create_board_game":
                        return canManage ? await CreateBoardGameAsync(call.Args, ct) : Denied();
                    case "create_tournament":
                        return canManage ? await CreateTournamentAsync(call.Args, ct) : Denied();
                    case "create_match":
                        return canManage ? await CreateMatchAsync(call.Args, ct) : Denied();
                    case "update_player":
                        return canManage ? await UpdatePlayerAsync(call.Args, ct) : Denied();
                    case "update_team":
                        return canManage ? await UpdateTeamAsync(call.Args, ct) : Denied();
                    case "update_venue":
                        return canManage ? await UpdateVenueAsync(call.Args, ct) : Denied();
                    case "update_board_game":
                        return canManage ? await UpdateBoardGameAsync(call.Args, ct) : Denied();
                    case "update_tournament":
                        return canManage ? await UpdateTournamentAsync(call.Args, ct) : Denied();
                    case "update_match":
                        return canManage ? await UpdateMatchAsync(call.Args, ct) : Denied();
                    default:
                        return (new { error = $"Unknown function '{call.Name}'." }, new List<ChatLinkDto>());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat tool execution failed for {Function}", call.Name);
                return (new { error = "Something went wrong performing that action." }, new List<ChatLinkDto>());
            }
        }

        private static (object, List<ChatLinkDto>) Denied() =>
            (new { error = "This account's role does not have permission to create or modify data." }, new List<ChatLinkDto>());

        private async Task<(object, List<ChatLinkDto>)> SearchAsync(JsonElement args, CancellationToken ct)
        {
            var query = GetString(args, "query").Trim().ToLower();
            var entityType = GetString(args, "entityType", "Any");
            var location = GetString(args, "location").Trim().ToLower();
            var category = GetString(args, "category");
            var minPlayers = TryGetInt(args, "minPlayers");
            var maxPlayers = TryGetInt(args, "maxPlayers");
            var minRating = TryGetInt(args, "minRating");
            var maxRating = TryGetInt(args, "maxRating");
            var isCompleted = TryGetBool(args, "isCompleted");
            var isOpen = TryGetBool(args, "isOpen");
            var isActive = TryGetBool(args, "isActive");
            var dateFrom = TryGetDate(args, "dateFrom");
            var dateTo = TryGetDate(args, "dateTo");
            var upcomingOnly = TryGetBool(args, "upcomingOnly") ?? false;

            var links = new List<ChatLinkDto>();
            var results = new List<object>();

            async Task AddAsync<T>(IQueryable<T> source, Func<T, (string Name, string Url, string Info)> map)
            {
                var items = await source.Take(MaxResultsPerCategory).ToListAsync(ct);
                foreach (var item in items)
                {
                    var (name, url, info) = map(item);
                    results.Add(new { name, url, info });
                    links.Add(new ChatLinkDto { Label = name, Url = url });
                }
            }

            var wantsAny = entityType == "Any" || string.IsNullOrEmpty(entityType);

            if (wantsAny || entityType == "Player")
            {
                var q = _context.Players.AsQueryable();
                if (!string.IsNullOrEmpty(query)) q = q.Where(p => p.Name.ToLower().Contains(query));
                if (!string.IsNullOrEmpty(location)) q = q.Where(p => p.Country.ToLower().Contains(location));
                if (minRating.HasValue) q = q.Where(p => p.Rating >= minRating.Value);
                if (maxRating.HasValue) q = q.Where(p => p.Rating <= maxRating.Value);
                await AddAsync(q.OrderByDescending(p => p.Rating),
                    p => (p.Name, $"/Players/Details/{p.Id}", $"Rating {p.Rating}, {p.Country}, {p.Role}"));
            }
            if (wantsAny || entityType == "Team")
            {
                var q = _context.Teams.AsQueryable();
                if (!string.IsNullOrEmpty(query)) q = q.Where(t => t.Name.ToLower().Contains(query));
                if (!string.IsNullOrEmpty(location)) q = q.Where(t => t.Region.ToLower().Contains(location));
                if (isActive.HasValue) q = q.Where(t => t.IsActive == isActive.Value);
                await AddAsync(q,
                    t => (t.Name, $"/Teams/Details/{t.Id}", $"{t.Region}, {(t.IsActive ? "active" : "inactive")}, {t.TotalWins}W-{t.TotalLosses}L"));
            }
            if (wantsAny || entityType == "Venue")
            {
                var q = _context.Venues.AsQueryable();
                if (!string.IsNullOrEmpty(query)) q = q.Where(v => v.Name.ToLower().Contains(query));
                if (!string.IsNullOrEmpty(location)) q = q.Where(v => v.City.ToLower().Contains(location) || v.Country.ToLower().Contains(location));
                await AddAsync(q,
                    v => (v.Name, $"/Venues/Details/{v.Id}", $"{v.City}, {v.Country}, capacity {v.Capacity}, {(v.Indoor ? "indoor" : "outdoor")}"));
            }
            if (wantsAny || entityType == "BoardGame")
            {
                var q = _context.BoardGames.AsQueryable();
                if (!string.IsNullOrEmpty(query)) q = q.Where(b => b.Name.ToLower().Contains(query));
                if (!string.IsNullOrEmpty(category) && Enum.TryParse<GameCategory>(category, true, out var cat)) q = q.Where(b => b.Category == cat);
                if (minPlayers.HasValue) q = q.Where(b => b.MaxPlayers >= minPlayers.Value);
                if (maxPlayers.HasValue) q = q.Where(b => b.MinPlayers <= maxPlayers.Value);
                await AddAsync(q,
                    b => (b.Name, $"/BoardGames/Details/{b.Id}", $"{b.Category}, {b.MinPlayers}-{b.MaxPlayers} players, complexity {b.Complexity}"));
            }
            if (wantsAny || entityType == "Tournament")
            {
                var q = _context.Tournaments.Include(t => t.Venue).AsQueryable();
                if (!string.IsNullOrEmpty(query)) q = q.Where(t => t.Name.ToLower().Contains(query));
                if (!string.IsNullOrEmpty(location)) q = q.Where(t => t.Venue.City.ToLower().Contains(location) || t.Venue.Country.ToLower().Contains(location));
                if (dateFrom.HasValue) q = q.Where(t => t.EndDate >= dateFrom.Value);
                if (dateTo.HasValue) q = q.Where(t => t.StartDate <= dateTo.Value);
                if (upcomingOnly) q = q.Where(t => t.StartDate >= DateTime.Now);
                if (isOpen.HasValue) q = q.Where(t => t.IsOpen == isOpen.Value);
                await AddAsync(q.OrderBy(t => t.StartDate),
                    t => (t.Name, $"/Tournaments/Details/{t.Id}", $"{t.StartDate:yyyy-MM-dd} to {t.EndDate:yyyy-MM-dd} at {t.Venue.Name}, {t.Venue.City} - {(t.IsOpen ? "open" : "closed")}"));
            }
            if (wantsAny || entityType == "Match")
            {
                var q = _context.Matches
                    .Include(m => m.TeamA).Include(m => m.TeamB)
                    .Include(m => m.Tournament).ThenInclude(t => t.Venue)
                    .AsQueryable();
                if (!string.IsNullOrEmpty(query)) q = q.Where(m => m.TeamA.Name.ToLower().Contains(query) || m.TeamB.Name.ToLower().Contains(query));
                if (!string.IsNullOrEmpty(location)) q = q.Where(m => m.Tournament.Venue.City.ToLower().Contains(location) || m.Tournament.Venue.Country.ToLower().Contains(location));
                if (dateFrom.HasValue) q = q.Where(m => m.StartTime >= dateFrom.Value);
                if (dateTo.HasValue) q = q.Where(m => m.StartTime <= dateTo.Value);
                if (upcomingOnly) q = q.Where(m => m.StartTime >= DateTime.Now);
                if (isCompleted.HasValue) q = q.Where(m => m.IsCompleted == isCompleted.Value);
                await AddAsync(q.OrderBy(m => m.StartTime),
                    m => ($"{m.TeamA.Name} - {m.TeamB.Name}", $"/Matches/Details/{m.Id}",
                        $"{m.StartTime:yyyy-MM-dd HH:mm} at {m.Tournament.Venue.City} ({m.Tournament.Name}) - {(m.IsCompleted ? $"{m.ScoreA}-{m.ScoreB}" : "upcoming")}"));
            }

            if (results.Count == 0)
            {
                return (new { message = "No matches found for these filters." }, links);
            }

            return (new { results }, links);
        }

        private async Task<(object, List<ChatLinkDto>)> CreatePlayerAsync(JsonElement args, CancellationToken ct)
        {
            var player = new Player
            {
                Name = GetString(args, "name"),
                Rating = GetInt(args, "rating", 1200),
                Country = GetString(args, "country", "N/A"),
                Role = GetString(args, "role", "Player"),
                JoinedDate = ParseDateOrDefault(args, "joinedDate", DateTime.Today)
            };

            var errors = ValidateEntity(player);
            if (errors.Count > 0)
            {
                return (new { error = "Validation failed: " + string.Join("; ", errors) }, new List<ChatLinkDto>());
            }

            _context.Players.Add(player);
            await _context.SaveChangesAsync(ct);

            var link = new ChatLinkDto { Label = player.Name, Url = $"/Players/Details/{player.Id}" };
            return (new { success = true, id = player.Id, name = player.Name }, new List<ChatLinkDto> { link });
        }

        private async Task<(object, List<ChatLinkDto>)> CreateTeamAsync(JsonElement args, CancellationToken ct)
        {
            var team = new Team
            {
                Name = GetString(args, "name"),
                Region = GetString(args, "region"),
                FoundedDate = ParseDateOrDefault(args, "foundedDate", DateTime.Today),
                IsActive = GetBool(args, "isActive", true)
            };

            var errors = ValidateEntity(team);
            if (errors.Count > 0)
            {
                return (new { error = "Validation failed: " + string.Join("; ", errors) }, new List<ChatLinkDto>());
            }

            _context.Teams.Add(team);
            await _context.SaveChangesAsync(ct);

            var link = new ChatLinkDto { Label = team.Name, Url = $"/Teams/Details/{team.Id}" };
            return (new { success = true, id = team.Id, name = team.Name }, new List<ChatLinkDto> { link });
        }

        private async Task<(object, List<ChatLinkDto>)> CreateVenueAsync(JsonElement args, CancellationToken ct)
        {
            var venue = new Venue
            {
                Name = GetString(args, "name"),
                City = GetString(args, "city"),
                Country = GetString(args, "country"),
                Capacity = GetInt(args, "capacity", 100),
                Indoor = GetBool(args, "indoor", true)
            };

            var errors = ValidateEntity(venue);
            if (errors.Count > 0)
            {
                return (new { error = "Validation failed: " + string.Join("; ", errors) }, new List<ChatLinkDto>());
            }

            _context.Venues.Add(venue);
            await _context.SaveChangesAsync(ct);

            var link = new ChatLinkDto { Label = venue.Name, Url = $"/Venues/Details/{venue.Id}" };
            return (new { success = true, id = venue.Id, name = venue.Name }, new List<ChatLinkDto> { link });
        }

        private async Task<(object, List<ChatLinkDto>)> CreateBoardGameAsync(JsonElement args, CancellationToken ct)
        {
            var categoryStr = GetString(args, "category", "Family");
            if (!Enum.TryParse<GameCategory>(categoryStr, true, out var category))
            {
                category = GameCategory.Family;
            }

            var game = new BoardGame
            {
                Name = GetString(args, "name"),
                Category = category,
                MinPlayers = GetInt(args, "minPlayers", 2),
                MaxPlayers = GetInt(args, "maxPlayers", 4),
                AveragePlayTime = TimeSpan.FromMinutes(GetInt(args, "averagePlayTimeMinutes", 60)),
                Complexity = GetDecimal(args, "complexity", 2.0m)
            };

            var errors = ValidateEntity(game);
            if (errors.Count > 0)
            {
                return (new { error = "Validation failed: " + string.Join("; ", errors) }, new List<ChatLinkDto>());
            }

            _context.BoardGames.Add(game);
            await _context.SaveChangesAsync(ct);

            var link = new ChatLinkDto { Label = game.Name, Url = $"/BoardGames/Details/{game.Id}" };
            return (new { success = true, id = game.Id, name = game.Name }, new List<ChatLinkDto> { link });
        }

        private async Task<(object, List<ChatLinkDto>)> CreateTournamentAsync(JsonElement args, CancellationToken ct)
        {
            var venueName = GetString(args, "venueName");
            var venue = await FindByNameAsync(_context.Venues, venueName, ct);
            if (venue == null)
            {
                return (new { error = $"No venue matching '{venueName}' was found. Use search_league_data to find the correct venue name first." },
                    new List<ChatLinkDto>());
            }

            var startDate = ParseDateOrDefault(args, "startDate", DateTime.Today.AddDays(7));
            var description = GetString(args, "description");
            var name = GetString(args, "name");

            var tournament = new Tournament
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(description) ? $"{name} tournament." : description,
                StartDate = startDate,
                EndDate = ParseDateOrDefault(args, "endDate", startDate.AddDays(2)),
                VenueId = venue.Id,
                IsOpen = GetBool(args, "isOpen", true)
            };

            var errors = ValidateEntity(tournament);
            if (errors.Count > 0)
            {
                return (new { error = "Validation failed: " + string.Join("; ", errors) }, new List<ChatLinkDto>());
            }

            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync(ct);

            var link = new ChatLinkDto { Label = tournament.Name, Url = $"/Tournaments/Details/{tournament.Id}" };
            return (new { success = true, id = tournament.Id, name = tournament.Name }, new List<ChatLinkDto> { link });
        }

        private async Task<(object, List<ChatLinkDto>)> CreateMatchAsync(JsonElement args, CancellationToken ct)
        {
            var tournament = await FindByNameAsync(_context.Tournaments, GetString(args, "tournamentName"), ct);
            var teamA = await FindByNameAsync(_context.Teams, GetString(args, "teamAName"), ct);
            var teamB = await FindByNameAsync(_context.Teams, GetString(args, "teamBName"), ct);
            var game = await FindByNameAsync(_context.BoardGames, GetString(args, "gameName"), ct);

            var missing = new List<string>();
            if (tournament == null) missing.Add("tournament");
            if (teamA == null) missing.Add("team A");
            if (teamB == null) missing.Add("team B");
            if (game == null) missing.Add("board game");
            if (missing.Count > 0)
            {
                return (new { error = $"Could not find: {string.Join(", ", missing)}. Use search_league_data to confirm the exact names first." },
                    new List<ChatLinkDto>());
            }

            if (teamA!.Id == teamB!.Id)
            {
                return (new { error = "Team A and Team B must be different teams." }, new List<ChatLinkDto>());
            }

            var match = new Match
            {
                TournamentId = tournament!.Id,
                TeamAId = teamA.Id,
                TeamBId = teamB.Id,
                GameId = game!.Id,
                StartTime = ParseDateOrDefault(args, "startTime", DateTime.Today.AddDays(1).AddHours(10)),
                ScoreA = GetInt(args, "scoreA", 0),
                ScoreB = GetInt(args, "scoreB", 0),
                IsCompleted = GetBool(args, "isCompleted", false)
            };

            var errors = ValidateEntity(match);
            if (errors.Count > 0)
            {
                return (new { error = "Validation failed: " + string.Join("; ", errors) }, new List<ChatLinkDto>());
            }

            _context.Matches.Add(match);
            await _context.SaveChangesAsync(ct);

            var link = new ChatLinkDto { Label = $"{teamA.Name} - {teamB.Name}", Url = $"/Matches/Details/{match.Id}" };
            return (new { success = true, id = match.Id, name = link.Label }, new List<ChatLinkDto> { link });
        }

        // Each Update*Async method below follows the same shape: resolve the target entity by
        // its current name (returning a clear error if it can't be found), apply only the
        // fields actually present in `args` (args.TryGetProperty gates each one so omitted
        // fields are left untouched), validate, then save.

        private async Task<(object, List<ChatLinkDto>)> UpdatePlayerAsync(JsonElement args, CancellationToken ct)
        {
            var name = GetString(args, "name");
            var player = await FindByNameAsync(_context.Players, name, ct);
            if (player == null)
            {
                return (new { error = $"No player matching '{name}' was found. Use search_league_data to confirm the exact name first." }, new List<ChatLinkDto>());
            }

            if (args.TryGetProperty("newName", out _)) player.Name = GetString(args, "newName", player.Name);
            if (args.TryGetProperty("rating", out _)) player.Rating = GetInt(args, "rating", player.Rating);
            if (args.TryGetProperty("country", out _)) player.Country = GetString(args, "country", player.Country);
            if (args.TryGetProperty("role", out _)) player.Role = GetString(args, "role", player.Role);
            if (args.TryGetProperty("joinedDate", out _)) player.JoinedDate = ParseDateOrDefault(args, "joinedDate", player.JoinedDate);

            var errors = ValidateEntity(player);
            if (errors.Count > 0)
            {
                return (new { error = "Validation failed: " + string.Join("; ", errors) }, new List<ChatLinkDto>());
            }

            await _context.SaveChangesAsync(ct);

            var link = new ChatLinkDto { Label = player.Name, Url = $"/Players/Details/{player.Id}" };
            return (new { success = true, id = player.Id, name = player.Name }, new List<ChatLinkDto> { link });
        }

        private async Task<(object, List<ChatLinkDto>)> UpdateTeamAsync(JsonElement args, CancellationToken ct)
        {
            var name = GetString(args, "name");
            var team = await FindByNameAsync(_context.Teams, name, ct);
            if (team == null)
            {
                return (new { error = $"No team matching '{name}' was found. Use search_league_data to confirm the exact name first." }, new List<ChatLinkDto>());
            }

            if (args.TryGetProperty("newName", out _)) team.Name = GetString(args, "newName", team.Name);
            if (args.TryGetProperty("region", out _)) team.Region = GetString(args, "region", team.Region);
            if (args.TryGetProperty("foundedDate", out _)) team.FoundedDate = ParseDateOrDefault(args, "foundedDate", team.FoundedDate);
            if (args.TryGetProperty("isActive", out _)) team.IsActive = GetBool(args, "isActive", team.IsActive);

            var errors = ValidateEntity(team);
            if (errors.Count > 0)
            {
                return (new { error = "Validation failed: " + string.Join("; ", errors) }, new List<ChatLinkDto>());
            }

            await _context.SaveChangesAsync(ct);

            var link = new ChatLinkDto { Label = team.Name, Url = $"/Teams/Details/{team.Id}" };
            return (new { success = true, id = team.Id, name = team.Name }, new List<ChatLinkDto> { link });
        }

        private async Task<(object, List<ChatLinkDto>)> UpdateVenueAsync(JsonElement args, CancellationToken ct)
        {
            var name = GetString(args, "name");
            var venue = await FindByNameAsync(_context.Venues, name, ct);
            if (venue == null)
            {
                return (new { error = $"No venue matching '{name}' was found. Use search_league_data to confirm the exact name first." }, new List<ChatLinkDto>());
            }

            if (args.TryGetProperty("newName", out _)) venue.Name = GetString(args, "newName", venue.Name);
            if (args.TryGetProperty("city", out _)) venue.City = GetString(args, "city", venue.City);
            if (args.TryGetProperty("country", out _)) venue.Country = GetString(args, "country", venue.Country);
            if (args.TryGetProperty("capacity", out _)) venue.Capacity = GetInt(args, "capacity", venue.Capacity);
            if (args.TryGetProperty("indoor", out _)) venue.Indoor = GetBool(args, "indoor", venue.Indoor);

            var errors = ValidateEntity(venue);
            if (errors.Count > 0)
            {
                return (new { error = "Validation failed: " + string.Join("; ", errors) }, new List<ChatLinkDto>());
            }

            await _context.SaveChangesAsync(ct);

            var link = new ChatLinkDto { Label = venue.Name, Url = $"/Venues/Details/{venue.Id}" };
            return (new { success = true, id = venue.Id, name = venue.Name }, new List<ChatLinkDto> { link });
        }

        private async Task<(object, List<ChatLinkDto>)> UpdateBoardGameAsync(JsonElement args, CancellationToken ct)
        {
            var name = GetString(args, "name");
            var game = await FindByNameAsync(_context.BoardGames, name, ct);
            if (game == null)
            {
                return (new { error = $"No board game matching '{name}' was found. Use search_league_data to confirm the exact name first." }, new List<ChatLinkDto>());
            }

            if (args.TryGetProperty("newName", out _)) game.Name = GetString(args, "newName", game.Name);
            if (args.TryGetProperty("category", out _))
            {
                var categoryStr = GetString(args, "category");
                if (Enum.TryParse<GameCategory>(categoryStr, true, out var cat))
                {
                    game.Category = cat;
                }
            }
            if (args.TryGetProperty("minPlayers", out _)) game.MinPlayers = GetInt(args, "minPlayers", game.MinPlayers);
            if (args.TryGetProperty("maxPlayers", out _)) game.MaxPlayers = GetInt(args, "maxPlayers", game.MaxPlayers);
            if (args.TryGetProperty("averagePlayTimeMinutes", out _)) game.AveragePlayTime = TimeSpan.FromMinutes(GetInt(args, "averagePlayTimeMinutes", (int)game.AveragePlayTime.TotalMinutes));
            if (args.TryGetProperty("complexity", out _)) game.Complexity = GetDecimal(args, "complexity", game.Complexity);

            var errors = ValidateEntity(game);
            if (errors.Count > 0)
            {
                return (new { error = "Validation failed: " + string.Join("; ", errors) }, new List<ChatLinkDto>());
            }

            await _context.SaveChangesAsync(ct);

            var link = new ChatLinkDto { Label = game.Name, Url = $"/BoardGames/Details/{game.Id}" };
            return (new { success = true, id = game.Id, name = game.Name }, new List<ChatLinkDto> { link });
        }

        private async Task<(object, List<ChatLinkDto>)> UpdateTournamentAsync(JsonElement args, CancellationToken ct)
        {
            var name = GetString(args, "name");
            var tournament = await FindByNameAsync(_context.Tournaments, name, ct);
            if (tournament == null)
            {
                return (new { error = $"No tournament matching '{name}' was found. Use search_league_data to confirm the exact name first." }, new List<ChatLinkDto>());
            }

            if (args.TryGetProperty("newName", out _)) tournament.Name = GetString(args, "newName", tournament.Name);
            if (args.TryGetProperty("description", out _)) tournament.Description = GetString(args, "description", tournament.Description);
            if (args.TryGetProperty("startDate", out _)) tournament.StartDate = ParseDateOrDefault(args, "startDate", tournament.StartDate);
            if (args.TryGetProperty("endDate", out _)) tournament.EndDate = ParseDateOrDefault(args, "endDate", tournament.EndDate);
            if (args.TryGetProperty("isOpen", out _)) tournament.IsOpen = GetBool(args, "isOpen", tournament.IsOpen);
            if (args.TryGetProperty("venueName", out _))
            {
                var venueName = GetString(args, "venueName");
                var venue = await FindByNameAsync(_context.Venues, venueName, ct);
                if (venue == null)
                {
                    return (new { error = $"No venue matching '{venueName}' was found. Use search_league_data to confirm the exact venue name first." },
                        new List<ChatLinkDto>());
                }
                tournament.VenueId = venue.Id;
            }

            var errors = ValidateEntity(tournament);
            if (errors.Count > 0)
            {
                return (new { error = "Validation failed: " + string.Join("; ", errors) }, new List<ChatLinkDto>());
            }

            await _context.SaveChangesAsync(ct);

            var link = new ChatLinkDto { Label = tournament.Name, Url = $"/Tournaments/Details/{tournament.Id}" };
            return (new { success = true, id = tournament.Id, name = tournament.Name }, new List<ChatLinkDto> { link });
        }

        private async Task<(object, List<ChatLinkDto>)> UpdateMatchAsync(JsonElement args, CancellationToken ct)
        {
            var teamAName = GetString(args, "teamAName");
            var teamBName = GetString(args, "teamBName");
            var tournamentName = GetString(args, "tournamentName");

            var teamA = await FindByNameAsync(_context.Teams, teamAName, ct);
            var teamB = await FindByNameAsync(_context.Teams, teamBName, ct);
            if (teamA == null || teamB == null)
            {
                return (new { error = $"Could not find one or both teams ('{teamAName}', '{teamBName}'). Use search_league_data to confirm the exact names first." },
                    new List<ChatLinkDto>());
            }

            var candidatesQuery = _context.Matches
                .Include(m => m.Tournament)
                .Where(m => (m.TeamAId == teamA.Id && m.TeamBId == teamB.Id) || (m.TeamAId == teamB.Id && m.TeamBId == teamA.Id));

            if (!string.IsNullOrWhiteSpace(tournamentName))
            {
                var lowered = tournamentName.ToLower();
                candidatesQuery = candidatesQuery.Where(m => m.Tournament.Name.ToLower().Contains(lowered));
            }

            var candidates = await candidatesQuery.ToListAsync(ct);

            if (candidates.Count == 0)
            {
                return (new { error = $"No match found between '{teamA.Name}' and '{teamB.Name}'" +
                    (string.IsNullOrWhiteSpace(tournamentName) ? "" : $" in a tournament matching '{tournamentName}'") +
                    ". Use search_league_data to confirm." }, new List<ChatLinkDto>());
            }

            if (candidates.Count > 1)
            {
                return (new { error = $"Found {candidates.Count} matches between '{teamA.Name}' and '{teamB.Name}'. Narrow it down with tournamentName." },
                    new List<ChatLinkDto>());
            }

            var match = candidates[0];

            if (args.TryGetProperty("newTeamAName", out _))
            {
                var newTeamAName = GetString(args, "newTeamAName");
                var newTeamA = await FindByNameAsync(_context.Teams, newTeamAName, ct);
                if (newTeamA == null)
                {
                    return (new { error = $"No team matching '{newTeamAName}' was found." }, new List<ChatLinkDto>());
                }
                match.TeamAId = newTeamA.Id;
            }
            if (args.TryGetProperty("newTeamBName", out _))
            {
                var newTeamBName = GetString(args, "newTeamBName");
                var newTeamB = await FindByNameAsync(_context.Teams, newTeamBName, ct);
                if (newTeamB == null)
                {
                    return (new { error = $"No team matching '{newTeamBName}' was found." }, new List<ChatLinkDto>());
                }
                match.TeamBId = newTeamB.Id;
            }
            if (args.TryGetProperty("newTournamentName", out _))
            {
                var newTournamentName = GetString(args, "newTournamentName");
                var newTournament = await FindByNameAsync(_context.Tournaments, newTournamentName, ct);
                if (newTournament == null)
                {
                    return (new { error = $"No tournament matching '{newTournamentName}' was found." }, new List<ChatLinkDto>());
                }
                match.TournamentId = newTournament.Id;
            }
            if (args.TryGetProperty("gameName", out _))
            {
                var gameName = GetString(args, "gameName");
                var game = await FindByNameAsync(_context.BoardGames, gameName, ct);
                if (game == null)
                {
                    return (new { error = $"No board game matching '{gameName}' was found." }, new List<ChatLinkDto>());
                }
                match.GameId = game.Id;
            }
            if (args.TryGetProperty("startTime", out _)) match.StartTime = ParseDateOrDefault(args, "startTime", match.StartTime);
            if (args.TryGetProperty("scoreA", out _)) match.ScoreA = GetInt(args, "scoreA", match.ScoreA);
            if (args.TryGetProperty("scoreB", out _)) match.ScoreB = GetInt(args, "scoreB", match.ScoreB);
            if (args.TryGetProperty("isCompleted", out _)) match.IsCompleted = GetBool(args, "isCompleted", match.IsCompleted);

            if (match.TeamAId == match.TeamBId)
            {
                return (new { error = "Team A and Team B must be different teams." }, new List<ChatLinkDto>());
            }

            var errors = ValidateEntity(match);
            if (errors.Count > 0)
            {
                return (new { error = "Validation failed: " + string.Join("; ", errors) }, new List<ChatLinkDto>());
            }

            await _context.SaveChangesAsync(ct);

            var updatedTeamA = await _context.Teams.FindAsync(new object?[] { match.TeamAId }, ct);
            var updatedTeamB = await _context.Teams.FindAsync(new object?[] { match.TeamBId }, ct);
            var label = $"{updatedTeamA?.Name} - {updatedTeamB?.Name}";
            var link = new ChatLinkDto { Label = label, Url = $"/Matches/Details/{match.Id}" };
            return (new { success = true, id = match.Id, name = label }, new List<ChatLinkDto> { link });
        }

        private static async Task<TEntity?> FindByNameAsync<TEntity>(IQueryable<TEntity> source, string name, CancellationToken ct)
            where TEntity : class
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var lowered = name.ToLower();
            var property = typeof(TEntity).GetProperty("Name");
            if (property == null)
            {
                return null;
            }

            // EF can't translate a reflection-based property access, so pull candidates
            // client-side; the seeded/created data volumes here are small enough for that
            // to be fine, and it keeps this generic across five different entity types.
            var all = await source.ToListAsync(ct);
            return all.FirstOrDefault(e => ((string?)property.GetValue(e))?.ToLower().Contains(lowered) == true);
        }

        private static List<string> ValidateEntity(object entity)
        {
            var context = new ValidationContext(entity);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(entity, context, results, validateAllProperties: true);
            return results.Select(r => r.ErrorMessage ?? "Invalid value.").ToList();
        }

        private static string GetString(JsonElement args, string prop, string fallback = "")
        {
            return args.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.String
                ? (el.GetString() ?? fallback)
                : fallback;
        }

        private static int GetInt(JsonElement args, string prop, int fallback)
        {
            if (args.TryGetProperty(prop, out var el))
            {
                if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var i)) return i;
                if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var i2)) return i2;
            }
            return fallback;
        }

        private static decimal GetDecimal(JsonElement args, string prop, decimal fallback)
        {
            if (args.TryGetProperty(prop, out var el))
            {
                if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var d)) return d;
                if (el.ValueKind == JsonValueKind.String && decimal.TryParse(el.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var d2)) return d2;
            }
            return fallback;
        }

        private static bool GetBool(JsonElement args, string prop, bool fallback)
        {
            if (args.TryGetProperty(prop, out var el))
            {
                if (el.ValueKind == JsonValueKind.True) return true;
                if (el.ValueKind == JsonValueKind.False) return false;
            }
            return fallback;
        }

        private static DateTime ParseDateOrDefault(JsonElement args, string prop, DateTime fallback)
        {
            return TryGetDate(args, prop) ?? fallback;
        }

        private static int? TryGetInt(JsonElement args, string prop)
        {
            if (args.TryGetProperty(prop, out var el))
            {
                if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var i)) return i;
                if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var i2)) return i2;
            }
            return null;
        }

        private static bool? TryGetBool(JsonElement args, string prop)
        {
            if (args.TryGetProperty(prop, out var el))
            {
                if (el.ValueKind == JsonValueKind.True) return true;
                if (el.ValueKind == JsonValueKind.False) return false;
            }
            return null;
        }

        private static DateTime? TryGetDate(JsonElement args, string prop)
        {
            if (args.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.String)
            {
                var s = el.GetString();
                if (!string.IsNullOrWhiteSpace(s) && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    return dt;
                }
            }
            return null;
        }
    }
}
