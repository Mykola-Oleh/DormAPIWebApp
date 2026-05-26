using Microsoft.EntityFrameworkCore;

namespace DormAPIWebApp.Models
{
    public class DormContext : DbContext
    {
        public DormContext(DbContextOptions<DormContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Dorm> Dorms { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<CheckIn> CheckIns { get; set; }
        public DbSet<Payment> Payments { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(10,2)");
        }
    }

}
