using DAL.Configurations;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Context
{
    public class MyContext : DbContext
    {
        public MyContext(DbContextOptions<MyContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserCredential> UserCredentials { get; set; }
        public DbSet<Script> Scripts { get; set; }
        public DbSet<Release> Releases { get; set; }
        public DbSet<Conflict> Conflicts { get; set; }
        public DbSet<Commit> Commits { get; set; }
        public DbSet<Batch> Batches { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new UserMap());
            modelBuilder.ApplyConfiguration(new UserCredentialMap());
            modelBuilder.ApplyConfiguration(new ScriptMap());
            modelBuilder.ApplyConfiguration(new ReleaseMap());
            modelBuilder.ApplyConfiguration(new ConflictMap());
            modelBuilder.ApplyConfiguration(new CommitMap());
            modelBuilder.ApplyConfiguration(new BatchMap());
        }
    }
}
