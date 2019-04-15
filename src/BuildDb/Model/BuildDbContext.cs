using System;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Build.Logging.BuildDb.Model
{
    internal class BuildDbContext : DbContext
    {
        public DbSet<Build> Builds { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<BuildProperty> BuildProperties { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectProperty> ProjectProperties { get; set; }

        public BuildDbContext() : base(CreateInMemoryOptions())
        {
        }

        public BuildDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Build>(build =>
            {

            });

            modelBuilder.Entity<Property>(prop =>
            {
                prop.HasIndex(p => p.Name);
            });

            modelBuilder.Entity<BuildProperty>(prop =>
            {
                prop.HasKey(p => new { p.BuildId, p.PropertyId });

                prop.HasOne(p => p.Build)
                    .WithMany(b => b.Environment)
                    .HasForeignKey(p => p.BuildId);

                prop.HasOne(p => p.Property)
                    .WithMany()
                    .HasForeignKey(p => p.PropertyId);
            });

            modelBuilder.Entity<Project>(project =>
            {
                project.HasIndex(p => new { p.BuildId, p.ProjectContextId });

                project.HasOne(p => p.Build)
                    .WithMany(b => b.Projects)
                    .HasForeignKey(p => p.BuildId);
            });

            modelBuilder.Entity<ProjectProperty>(prop =>
            {
                prop.HasKey(p => new { p.ProjectId, p.PropertyId });

                prop.HasOne(p => p.Project)
                    .WithMany(p => p.Properties)
                    .HasForeignKey(p => p.ProjectId);

                prop.HasOne(p => p.Property)
                    .WithMany()
                    .HasForeignKey(p => p.PropertyId);
            });
        }

        private static DbContextOptions CreateInMemoryOptions()
        {
            return new DbContextOptionsBuilder()
                .UseSqlite("Data Source=:memory:")
                .Options;
        }
    }
}
