using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BoardGameLeague.Models
{
    public class PlayerDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Rating { get; set; }
        public DateTime JoinedDate { get; set; }
        public string Country { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class PlayerCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 3000)]
        public int Rating { get; set; }

        [Required]
        public DateTime JoinedDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Country { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty;
    }

    public class PlayerUpdateDto : PlayerCreateDto
    {
        [Required]
        public Guid Id { get; set; }
    }

    public class BoardGameDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public GameCategory Category { get; set; }
        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public int AveragePlayTimeMinutes { get; set; }
        public decimal Complexity { get; set; }
    }

    public class BoardGameCreateDto
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public GameCategory Category { get; set; }

        [Range(1, 50)]
        public int MinPlayers { get; set; }

        [Range(1, 50)]
        public int MaxPlayers { get; set; }

        [Range(15, 720)]
        public int AveragePlayTimeMinutes { get; set; }

        [Range(0.1, 5.0)]
        public decimal Complexity { get; set; }
    }

    public class BoardGameUpdateDto : BoardGameCreateDto
    {
        [Required]
        public Guid Id { get; set; }
    }

    public class TeamDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public DateTime FoundedDate { get; set; }
        public bool IsActive { get; set; }
        public int TotalWins { get; set; }
        public int TotalLosses { get; set; }
        public List<PlayerDto> Players { get; set; } = new List<PlayerDto>();
    }

    public class TeamCreateDto
    {
        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        public string Region { get; set; } = string.Empty;

        [Required]
        public DateTime FoundedDate { get; set; }

        public bool IsActive { get; set; }

        [Range(0, 1000)]
        public int TotalWins { get; set; }

        [Range(0, 1000)]
        public int TotalLosses { get; set; }

        public List<Guid> PlayerIds { get; set; } = new List<Guid>();
    }

    public class TeamUpdateDto : TeamCreateDto
    {
        [Required]
        public Guid Id { get; set; }
    }

    public class TeamQueryParameters : QueryParameters
    {
        public string? Name { get; set; }
        public string? Region { get; set; }
        public bool? IsActive { get; set; }
        public int? MinWins { get; set; }
        public int? MaxWins { get; set; }
    }

    public class VenueDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public bool Indoor { get; set; }
    }

    public class VenueCreateDto
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        public string Country { get; set; } = string.Empty;

        [Range(1, 50000)]
        public int Capacity { get; set; }

        public bool Indoor { get; set; }
    }

    public class VenueUpdateDto : VenueCreateDto
    {
        [Required]
        public Guid Id { get; set; }
    }

    public class VenueQueryParameters : QueryParameters
    {
        public string? Name { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public int? MinCapacity { get; set; }
        public int? MaxCapacity { get; set; }
        public bool? Indoor { get; set; }
    }

    public class TournamentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsOpen { get; set; }
        public VenueDto? Venue { get; set; }
        public List<TeamDto> Teams { get; set; } = new List<TeamDto>();
    }

    public class TournamentCreateDto
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Required]
        public Guid VenueId { get; set; }

        public bool IsOpen { get; set; }
        public List<Guid> TeamIds { get; set; } = new List<Guid>();
    }

    public class TournamentUpdateDto : TournamentCreateDto
    {
        [Required]
        public Guid Id { get; set; }
    }

    public class TournamentQueryParameters : QueryParameters
    {
        public string? Name { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsOpen { get; set; }
        public Guid? VenueId { get; set; }
    }

    public class MatchDto
    {
        public Guid Id { get; set; }
        public VenueDto? Venue { get; set; }
        public TournamentDto? Tournament { get; set; }
        public TeamDto? TeamA { get; set; }
        public TeamDto? TeamB { get; set; }
        public BoardGameDto? Game { get; set; }
        public DateTime StartTime { get; set; }
        public int ScoreA { get; set; }
        public int ScoreB { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class MatchCreateDto
    {
        [Required]
        public Guid TournamentId { get; set; }

        [Required]
        public Guid TeamAId { get; set; }

        [Required]
        public Guid TeamBId { get; set; }

        [Required]
        public Guid GameId { get; set; }

        public DateTime StartTime { get; set; }
        [Range(0, 100)]
        public int ScoreA { get; set; }
        [Range(0, 100)]
        public int ScoreB { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class MatchUpdateDto : MatchCreateDto
    {
        [Required]
        public Guid Id { get; set; }
    }

    public class MatchQueryParameters : QueryParameters
    {
        public Guid? TournamentId { get; set; }
        public Guid? TeamAId { get; set; }
        public Guid? TeamBId { get; set; }
        public Guid? GameId { get; set; }
        public bool? IsCompleted { get; set; }
        public DateTime? StartTimeFrom { get; set; }
        public DateTime? StartTimeTo { get; set; }
    }

    public class QueryParameters
    {
        private const int MaxPageSize = 50;
        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        public string? SortBy { get; set; }
        public string? SortOrder { get; set; } = "asc"; // "asc" or "desc"
    }

    public class BoardGameQueryParameters : QueryParameters
    {
        public string? Name { get; set; }
        public GameCategory? Category { get; set; }
        public decimal? MinComplexity { get; set; }
        public decimal? MaxComplexity { get; set; }
    }

    public class PlayerQueryParameters : QueryParameters
    {
        public string? Name { get; set; }
        public string? Country { get; set; }
        public string? Role { get; set; }
        public int? MinRating { get; set; }
        public int? MaxRating { get; set; }
    }

    public class AttachmentDto
    {
        public int Id { get; set; }
        public Guid TournamentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
