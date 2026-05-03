using Microsoft.EntityFrameworkCore;

namespace BoardGameLeague.Models
{
    public class BoardGameLeagueDbContext : DbContext
    {
        public BoardGameLeagueDbContext(DbContextOptions<BoardGameLeagueDbContext> options)
            : base(options)
        {
        }

        public DbSet<Player> Players { get; set; } = null!;
        public DbSet<BoardGame> BoardGames { get; set; } = null!;
        public DbSet<Team> Teams { get; set; } = null!;
        public DbSet<Venue> Venues { get; set; } = null!;
        public DbSet<Tournament> Tournaments { get; set; } = null!;
        public DbSet<Match> Matches { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Tournament>()
                .HasOne(t => t.Venue)
                .WithMany(v => v.Tournaments)
                .HasForeignKey(t => t.VenueId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.Tournament)
                .WithMany(t => t.Matches)
                .HasForeignKey(m => m.TournamentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.TeamA)
                .WithMany()
                .HasForeignKey(m => m.TeamAId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.TeamB)
                .WithMany()
                .HasForeignKey(m => m.TeamBId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.Game)
                .WithMany(g => g.Matches)
                .HasForeignKey(m => m.GameId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BoardGame>()
                .Property(b => b.Complexity)
                .HasPrecision(4, 2);

            modelBuilder.Entity<Team>()
                .HasMany(t => t.Tournaments)
                .WithMany(t => t.Teams);
        }
    }
}
