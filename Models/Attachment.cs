using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoardGameLeague.Models
{
    public class Attachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid TournamentId { get; set; }

        [ForeignKey(nameof(TournamentId))]
        public virtual Tournament? Tournament { get; set; }

        [Required]
        [StringLength(260)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(1024)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
