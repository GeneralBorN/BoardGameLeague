using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BoardGameLeague.Models
{
    public enum GameCategory
    {
        Strategy,
        Family,
        Party,
        Cooperative,
        Thematic,
        Abstract
    }

    public class Player
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public int Rating { get; set; }
        public DateTime JoinedDate { get; set; }
        public string Country { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
    }

    public class BoardGame
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public GameCategory Category { get; set; }
        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public TimeSpan AveragePlayTime { get; set; }
        public decimal Complexity { get; set; }
        public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
    }

    public class Team
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public DateTime FoundedDate { get; set; }
        public bool IsActive { get; set; }
        public int TotalWins { get; set; }
        public int TotalLosses { get; set; }
        public virtual ICollection<Player> Players { get; set; } = new List<Player>();
        public virtual ICollection<Tournament> Tournaments { get; set; } = new List<Tournament>();

        [NotMapped]
        public double WinRate => (TotalWins + TotalLosses) == 0 ? 0 : (double)TotalWins / (TotalWins + TotalLosses);
    }

    public class Venue
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public bool Indoor { get; set; }
        public virtual ICollection<Tournament> Tournaments { get; set; } = new List<Tournament>();
    }

    public class Tournament
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [ForeignKey("Venue")]
        public Guid VenueId { get; set; }
        public virtual Venue Venue { get; set; } = null!;

        public bool IsOpen { get; set; }
        public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
        public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
    }

    public class Match
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey("Tournament")]
        public Guid TournamentId { get; set; }
        public virtual Tournament Tournament { get; set; } = null!;

        [ForeignKey("TeamA")]
        public Guid TeamAId { get; set; }
        public virtual Team TeamA { get; set; } = null!;

        [ForeignKey("TeamB")]
        public Guid TeamBId { get; set; }
        public virtual Team TeamB { get; set; } = null!;

        [ForeignKey("Game")]
        public Guid GameId { get; set; }
        public virtual BoardGame Game { get; set; } = null!;

        public DateTime StartTime { get; set; }
        public int ScoreA { get; set; }
        public int ScoreB { get; set; }
        public bool IsCompleted { get; set; }

        [NotMapped]
        public Team Winner => ScoreA > ScoreB ? TeamA : TeamB;
    }

    public class LeagueDashboardViewModel
    {
        public List<Team> AllTeams { get; set; } = new List<Team>();
        public List<Tournament> AllTournaments { get; set; } = new List<Tournament>();
        public List<Match> AllMatches { get; set; } = new List<Match>();
        public List<BoardGame> AllBoardGames { get; set; } = new List<BoardGame>();

        public List<Team> TopTeamsByAveragePlayerRating { get; set; } = new List<Team>();
        public List<Team> HighWinRateTeams { get; set; } = new List<Team>();
        public List<Tournament> UpcomingTournaments { get; set; } = new List<Tournament>();
        public List<string> PopularGameNames { get; set; } = new List<string>();
        public Dictionary<string, int> MatchesByVenue { get; set; } = new Dictionary<string, int>();
    }

    public static class LeagueDataFactory
    {
        public static async Task<LeagueDashboardViewModel> CreateSampleLeagueAsync()
        {
            await Task.Delay(1); // demonstrate async concept

            var players = new List<Player>
            {
                new Player { Name = "Ana", Rating = 1800, JoinedDate = DateTime.Today.AddYears(-2), Country = "HR", Role = "Captain" },
                new Player { Name = "Ivan", Rating = 1750, JoinedDate = DateTime.Today.AddYears(-1), Country = "HR", Role = "Player" },
                new Player { Name = "Luka", Rating = 1675, JoinedDate = DateTime.Today.AddYears(-1), Country = "HR", Role = "Player" },
                new Player { Name = "Petra", Rating = 1720, JoinedDate = DateTime.Today.AddMonths(-9), Country = "SI", Role = "Player" },
                new Player { Name = "Nina", Rating = 1580, JoinedDate = DateTime.Today.AddMonths(-6), Country = "SI", Role = "Player" },
                new Player { Name = "Marko", Rating = 1620, JoinedDate = DateTime.Today.AddMonths(-11), Country = "RS", Role = "Co-captain" },
                new Player { Name = "Ema", Rating = 1695, JoinedDate = DateTime.Today.AddYears(-3), Country = "HR", Role = "Player" },
                new Player { Name = "Sara", Rating = 1510, JoinedDate = DateTime.Today.AddMonths(-4), Country = "BA", Role = "Player" },
                new Player { Name = "Ivan2", Rating = 1605, JoinedDate = DateTime.Today.AddMonths(-2), Country = "BA", Role = "Player" }
            };

            var boardGames = new List<BoardGame>
            {
                new BoardGame { Name = "Catan", Category = GameCategory.Family, MinPlayers = 3, MaxPlayers = 4, AveragePlayTime = TimeSpan.FromMinutes(90), Complexity = 2.3m },
                new BoardGame { Name = "Azul", Category = GameCategory.Abstract, MinPlayers = 2, MaxPlayers = 4, AveragePlayTime = TimeSpan.FromMinutes(45), Complexity = 1.8m },
                new BoardGame { Name = "Pandemic", Category = GameCategory.Cooperative, MinPlayers = 2, MaxPlayers = 4, AveragePlayTime = TimeSpan.FromMinutes(60), Complexity = 2.5m },
                new BoardGame { Name = "Terraforming Mars", Category = GameCategory.Strategy, MinPlayers = 1, MaxPlayers = 5, AveragePlayTime = TimeSpan.FromMinutes(120), Complexity = 3.4m }
            };

            var teams = new List<Team>
            {
                new Team { Name = "Dice Crushers", Region = "Zagreb", FoundedDate = DateTime.Today.AddYears(-3), IsActive = true, TotalWins = 12, TotalLosses = 4 },
                new Team { Name = "Meeple Masters", Region = "Split", FoundedDate = DateTime.Today.AddYears(-2), IsActive = true, TotalWins = 8, TotalLosses = 8 },
                new Team { Name = "Card Sharks", Region = "Rijeka", FoundedDate = DateTime.Today.AddYears(-1), IsActive = true, TotalWins = 10, TotalLosses = 6 },
                new Team { Name = "Strategy Squad", Region = "Osijek", FoundedDate = DateTime.Today.AddYears(-4), IsActive = false, TotalWins = 6, TotalLosses = 10 },
                new Team { Name = "Board Bandits", Region = "Zadar", FoundedDate = DateTime.Today.AddYears(-2), IsActive = true, TotalWins = 11, TotalLosses = 5 },
                new Team { Name = "RPG Rebels", Region = "Split", FoundedDate = DateTime.Today.AddYears(-3), IsActive = true, TotalWins = 9, TotalLosses = 7 }
            };

            void AddPlayersToTeam(Team team, params Player[] roster)
            {
                foreach (var p in roster)
                {
                    team.Players.Add(p);
                    p.Teams.Add(team);
                }
            }

            AddPlayersToTeam(teams[0], players[0], players[1], players[2]);
            AddPlayersToTeam(teams[1], players[3], players[4], players[5]);
            AddPlayersToTeam(teams[2], players[6], players[7], players[8]);
            AddPlayersToTeam(teams[3], players[1], players[4], players[7]);
            AddPlayersToTeam(teams[4], players[0], players[5], players[6]);
            AddPlayersToTeam(teams[5], players[2], players[3], players[8]);

            var venues = new List<Venue>
            {
                new Venue { Name = "Zagreb Convention Centre", City = "Zagreb", Country = "HR", Capacity = 1500, Indoor = true },
                new Venue { Name = "Split Game Hall", City = "Split", Country = "HR", Capacity = 900, Indoor = true },
                new Venue { Name = "Rijeka Arena", City = "Rijeka", Country = "HR", Capacity = 1200, Indoor = false }
            };

            var tournaments = new List<Tournament>
            {
                new Tournament { Name = "Spring Board Game League", Description = "Season opener", StartDate = DateTime.Today.AddDays(10), EndDate = DateTime.Today.AddDays(13), Venue = venues[0], IsOpen = true },
                new Tournament { Name = "Summer Board Game League", Description = "Half-year championship", StartDate = DateTime.Today.AddDays(80), EndDate = DateTime.Today.AddDays(83), Venue = venues[1], IsOpen = true },
                new Tournament { Name = "Autumn Board Game League", Description = "Season finale", StartDate = DateTime.Today.AddDays(170), EndDate = DateTime.Today.AddDays(173), Venue = venues[2], IsOpen = false }
            };

            venues[0].Tournaments.Add(tournaments[0]);
            venues[1].Tournaments.Add(tournaments[1]);
            venues[2].Tournaments.Add(tournaments[2]);

            // assign teams to tournaments (many-to-many relationship)
            foreach (var team in new[] { teams[0], teams[1], teams[2] })
            {
                tournaments[0].Teams.Add(team);
            }
            foreach (var team in new[] { teams[2], teams[3], teams[4] })
            {
                tournaments[1].Teams.Add(team);
            }
            foreach (var team in new[] { teams[0], teams[4], teams[5] })
            {
                tournaments[2].Teams.Add(team);
            }

            foreach (var tournament in new[] { tournaments[0], tournaments[2] })
            {
                teams[0].Tournaments.Add(tournament);
            }
            teams[1].Tournaments.Add(tournaments[0]);
            foreach (var tournament in new[] { tournaments[0], tournaments[1] })
            {
                teams[2].Tournaments.Add(tournament);
            }
            teams[3].Tournaments.Add(tournaments[1]);
            foreach (var tournament in new[] { tournaments[1], tournaments[2] })
            {
                teams[4].Tournaments.Add(tournament);
            }
            teams[5].Tournaments.Add(tournaments[2]);

            var matches = new List<Match>
            {
                new Match { Tournament = tournaments[0], TeamA = teams[0], TeamB = teams[1], Game = boardGames[0], StartTime = DateTime.Today.AddDays(10).AddHours(11), ScoreA=3, ScoreB=1, IsCompleted=false },
                new Match { Tournament = tournaments[0], TeamA = teams[1], TeamB = teams[2], Game = boardGames[1], StartTime = DateTime.Today.AddDays(10).AddHours(14), ScoreA=2, ScoreB=3, IsCompleted=false },
                new Match { Tournament = tournaments[0], TeamA = teams[0], TeamB = teams[2], Game = boardGames[2], StartTime = DateTime.Today.AddDays(11).AddHours(10), ScoreA=4, ScoreB=2, IsCompleted=false },
                new Match { Tournament = tournaments[1], TeamA = teams[2], TeamB = teams[3], Game = boardGames[3], StartTime = DateTime.Today.AddDays(80).AddHours(10), ScoreA=2, ScoreB=3, IsCompleted=false },
                new Match { Tournament = tournaments[1], TeamA = teams[3], TeamB = teams[4], Game = boardGames[0], StartTime = DateTime.Today.AddDays(81).AddHours(12), ScoreA=1, ScoreB=4, IsCompleted=false },
                new Match { Tournament = tournaments[1], TeamA = teams[2], TeamB = teams[4], Game = boardGames[1], StartTime = DateTime.Today.AddDays(82).AddHours(15), ScoreA=3, ScoreB=3, IsCompleted=false },
                new Match { Tournament = tournaments[2], TeamA = teams[0], TeamB = teams[4], Game = boardGames[2], StartTime = DateTime.Today.AddDays(170).AddHours(9), ScoreA=0, ScoreB=0, IsCompleted=false },
                new Match { Tournament = tournaments[2], TeamA = teams[4], TeamB = teams[5], Game = boardGames[3], StartTime = DateTime.Today.AddDays(171).AddHours(14), ScoreA=0, ScoreB=0, IsCompleted=false },
                new Match { Tournament = tournaments[2], TeamA = teams[0], TeamB = teams[5], Game = boardGames[0], StartTime = DateTime.Today.AddDays(172).AddHours(11), ScoreA=0, ScoreB=0, IsCompleted=false }
            };

            foreach (var m in matches)
            {
                m.Tournament.Matches.Add(m);
            }

            // LINQ examples
            var topTeams = teams
                .OrderByDescending(t => t.Players.Count > 0 ? t.Players.Average(p => p.Rating) : 0)
                .ThenByDescending(t => t.WinRate)
                .Take(3)
                .ToList();

            var highWinRate = teams
                .Where(t => t.WinRate >= 0.60)
                .OrderByDescending(t => t.WinRate)
                .ToList();

            var upcoming = tournaments
                .Where(t => t.StartDate > DateTime.Today)
                .OrderBy(t => t.StartDate)
                .ToList();

            var popularGames = matches
                .GroupBy(m => m.Game.Name)
                .Select(g => new { GameName = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Select(x => x.GameName)
                .ToList();

            var matchesByVenue = matches
                .GroupBy(m => m.Tournament.Venue.Name)
                .ToDictionary(g => g.Key, g => g.Count());

            return new LeagueDashboardViewModel
            {
                AllTeams = teams,
                AllTournaments = tournaments,
                AllMatches = matches,
                AllBoardGames = boardGames,
                TopTeamsByAveragePlayerRating = topTeams,
                HighWinRateTeams = highWinRate,
                UpcomingTournaments = upcoming,
                PopularGameNames = popularGames,
                MatchesByVenue = matchesByVenue
            };
        }
    }
}
