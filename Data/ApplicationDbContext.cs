using Microsoft.EntityFrameworkCore;
using PostmateAPI.Models;

namespace PostmateAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Post> Posts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Post>(entity =>
            {
                entity.ToTable("posts");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Topic).HasColumnName("topic");
                entity.Property(e => e.Draft).HasColumnName("draft");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.ScheduledAt).HasColumnName("scheduled_at");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });
        }
    }
}
