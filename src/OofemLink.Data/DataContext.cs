using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using OofemLink.Data.MeshEntities;
using OofemLink.Data.ModelEntities;

namespace OofemLink.Data
{
	public class DataContext : DbContext
	{
		public static DbContextOptions<DataContext> CreateNewInMemoryContextOptions()
		{
			// Create a fresh service provider, and therefore a fresh 
			// InMemory database instance.
			var serviceProvider = new ServiceCollection()
				.AddEntityFrameworkInMemoryDatabase()
				.BuildServiceProvider();

			// Create a new options instance telling the context to use an
			// InMemory database and the new service provider.
			var builder = new DbContextOptionsBuilder<DataContext>();
			builder.UseInMemoryDatabase()
				   .UseInternalServiceProvider(serviceProvider);

			return builder.Options;
		}

		public DataContext()
		{ }

		public DataContext(DbContextOptions<DataContext> options)
			: base(options)
		{ }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			//optionsBuilder.UseInMemoryDatabase();
			optionsBuilder.UseSqlite("Filename=./oofem.db", b => b.MigrationsAssembly("OofemLink.WebApi"));
			//optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=oofem_db;Trusted_Connection=True;", b => b.MigrationsAssembly("OofemLink.WebApi"));
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Vertex>().HasKey(v => new { v.Id, v.ModelId });

			modelBuilder.Entity<Curve>().HasKey(c => new { c.Id, c.ModelId });
			modelBuilder.Entity<Surface>().HasKey(s => new { s.Id, s.ModelId });

			modelBuilder.Entity<Macro>().HasKey(m => new { m.Id, m.ModelId });

			modelBuilder.Entity<Macro>().HasMany(m => m.Vertices).WithOne(v => v.Macro).HasForeignKey(v => new { v.MacroId, v.ModelId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<Macro>().HasMany(m => m.Curves).WithOne(v => v.Macro).HasForeignKey(c => new { c.MacroId, c.ModelId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<Macro>().HasMany(m => m.Surfaces).WithOne(v => v.Macro).HasForeignKey(s => new { s.MacroId, s.ModelId }).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Node>().HasKey(b => new { b.Id, b.MeshId });
			modelBuilder.Entity<Beam>().HasKey(b => new { b.Id, b.MeshId });
		}

		public DbSet<Project> Projects { get; set; }
		public DbSet<Simulation> Simulations { get; set; }

		public DbSet<Model> Models { get; set; }

		public DbSet<Macro> Macros { get; set; }
		public DbSet<Vertex> Vertices { get; set; }
		public DbSet<Curve> Curves { get; set; }
		public DbSet<Surface> Surfaces { get; set; }

		public DbSet<Mesh> Meshes { get; set; }
		public DbSet<Node> Nodes { get; set; }
		public DbSet<Beam> Beams { get; set; }
	}
}
