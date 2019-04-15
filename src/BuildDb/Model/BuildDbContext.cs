using System;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Build.Logging.BuildDb.Model
{
    internal class BuildDbContext : DbContext
    {
        // Increment this in any public release when the schema is updated.
        public static readonly int CurrentSchemaVersion = 1;

        public DbSet<SchemaVersion> SchemaVersion { get; set; }
        public DbSet<Build> Builds { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<PropertyDefinition> PropertyDefinitions { get; set; }
        public DbSet<BuildProperty> BuildProperties { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectProperty> ProjectProperties { get; set; }
        public DbSet<ItemGroup> ItemGroups { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemMetadata> ItemMetadata { get; set; }

        public BuildDbContext() : base(CreateInMemoryOptions())
        {
        }

        public BuildDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SchemaVersion>(schemaVersion =>
            {
                schemaVersion.HasKey(s => s.Version);
                schemaVersion.HasData(new SchemaVersion() { Version = CurrentSchemaVersion });
                schemaVersion.ToTable("SchemaVersion");
            });

            modelBuilder.Entity<Build>(build =>
            {

            });

            modelBuilder.Entity<PropertyDefinition>(propDef =>
            {
                propDef.HasIndex(p => new { p.Name, p.IsMetadata });
            });

            modelBuilder.Entity<Property>(prop =>
            {
                prop.HasOne(p => p.Definition)
                    .WithMany(d => d.Properties)
                    .HasForeignKey(p => p.DefinitionId);
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
                prop.HasKey(p => new { p.ProjectId, p.PropertyId, p.Global });

                prop.HasOne(p => p.Project)
                    .WithMany(p => p.Properties)
                    .HasForeignKey(p => p.ProjectId);

                prop.HasOne(p => p.Property)
                    .WithMany()
                    .HasForeignKey(p => p.PropertyId);
            });

            modelBuilder.Entity<ItemGroup>(itemGroup =>
            {

            });

            modelBuilder.Entity<Item>(item =>
            {
                item.HasOne(i => i.ItemGroup)
                    .WithMany(g => g.Items)
                    .HasForeignKey(i => i.ItemGroupId);
            });

            modelBuilder.Entity<ItemMetadata>(itemMetadata =>
            {
                itemMetadata.HasKey(m => new { m.ItemId, m.PropertyId });

                itemMetadata.HasOne(m => m.Item)
                    .WithMany(i => i.Metadata)
                    .HasForeignKey(m => m.ItemId);

                itemMetadata.HasOne(m => m.Property)
                    .WithMany()
                    .HasForeignKey(m => m.PropertyId);
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
