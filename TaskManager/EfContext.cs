using Microsoft.EntityFrameworkCore;
using TaskManager.Models;

namespace TaskManager
{
    public class EfContext : DbContext
    {
        public EfContext(DbContextOptions<EfContext> options) : base(options) { }

        public DbSet<PersonalTask> PersonalTasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PersonalTask>().ToTable("PersonalTasks").HasKey(t => t.Id);
            modelBuilder.Entity<PersonalTask>().Property(t => t.Title).HasColumnType("VARCHAR(60)").IsRequired();
            modelBuilder.Entity<PersonalTask>().Property(t => t.Description).HasColumnType("VARCHAR(200)");
            modelBuilder.Entity<PersonalTask>().Property(t => t.CreatedAt).IsRequired();
        }
    }
}
