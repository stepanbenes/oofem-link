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
			//optionsBuilder.UseSqlite("Filename=./oofem.db", b => b.MigrationsAssembly("OofemLink.WebApi"));
			optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=oofem_db;Trusted_Connection=True;", b => b.MigrationsAssembly("OofemLink.WebApi"));
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			// MODEL
			modelBuilder.Entity<Project>().HasIndex(p => p.Name).IsUnique();

			modelBuilder.Entity<Model>()
				.Property(m => m.Id)
				.ValueGeneratedNever(); // don't let db to generate id

			modelBuilder.Entity<Model>()
				.HasOne(m => m.Simulation)
				.WithMany(s => s.Models)
				.HasForeignKey(m => m.Id)
				.IsRequired();

			// Vertex-Node Map
			modelBuilder.Entity<VertexNodeMap>()
				.HasOne(m => m.Vertex)
				.WithMany(v => v.VertexNodeMap)
				.HasForeignKey(m => new { m.VertexId, m.ModelId });
			modelBuilder.Entity<VertexNodeMap>()
				.HasOne(m => m.Node)
				.WithMany(n => n.VertexNodeMap)
				.HasForeignKey(m => new { m.NodeId, m.MeshId });
			modelBuilder.Entity<VertexNodeMap>()
				.HasKey(m => new { m.VertexId, m.ModelId, m.NodeId, m.MeshId });

			// Curve-Element Map
			modelBuilder.Entity<CurveElementMap>()
				.HasOne(m => m.Curve)
				.WithMany(c => c.CurveElementMap)
				.HasForeignKey(m => new { m.CurveId, m.ModelId });
			modelBuilder.Entity<CurveElementMap>()
				.HasOne(m => m.Element)
				.WithMany(e => e.CurveElementMap)
				.HasForeignKey(m => new { m.ElementId, m.MeshId });
			modelBuilder.Entity<CurveElementMap>()
				.HasKey(m => new { m.CurveId, m.ModelId, m.ElementId, m.MeshId });

			// Surface-Element Map
			modelBuilder.Entity<SurfaceElementMap>()
				.HasOne(m => m.Surface)
				.WithMany(s => s.SurfaceElementMap)
				.HasForeignKey(m => new { m.SurfaceId, m.ModelId });
			modelBuilder.Entity<SurfaceElementMap>()
				.HasOne(m => m.Element)
				.WithMany(e => e.SurfaceElementMap)
				.HasForeignKey(m => new { m.ElementId, m.MeshId });
			modelBuilder.Entity<SurfaceElementMap>()
				.HasKey(m => new { m.SurfaceId, m.ModelId, m.ElementId, m.MeshId });

			// MESH
			modelBuilder.Entity<Vertex>().HasKey(v => new { v.Id, v.ModelId });

			modelBuilder.Entity<Curve>().HasKey(c => new { c.Id, c.ModelId });
			modelBuilder.Entity<Surface>().HasKey(s => new { s.Id, s.ModelId });

			modelBuilder.Entity<Macro>().HasKey(m => new { m.Id, m.ModelId });

			modelBuilder.Entity<Macro>().HasMany(m => m.Vertices).WithOne(v => v.Macro).HasForeignKey(v => new { v.MacroId, v.ModelId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<Macro>().HasMany(m => m.Curves).WithOne(v => v.Macro).HasForeignKey(c => new { c.MacroId, c.ModelId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<Macro>().HasMany(m => m.Surfaces).WithOne(v => v.Macro).HasForeignKey(s => new { s.MacroId, s.ModelId }).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Node>().HasKey(b => new { b.Id, b.MeshId });
			modelBuilder.Entity<Element>().HasKey(b => new { b.Id, b.MeshId });

			// ElementNodes
			modelBuilder.Entity<ElementNode>()
				.HasOne(en => en.Element)
				.WithMany(e => e.ElementNodes)
				.HasForeignKey(en => new { en.ElementId, en.MeshId });
			modelBuilder.Entity<ElementNode>()
				.HasOne(en => en.Node)
				.WithMany(n => n.ElementNodes)
				.HasForeignKey(en => new { en.NodeId, en.MeshId });
			modelBuilder.Entity<ElementNode>()
				.HasKey(m => new { m.ElementId, m.NodeId, m.MeshId });
			modelBuilder.Entity<ElementNode>()
				.ToTable("ElementNodes");

			modelBuilder.Entity<Node>()
				.HasOne(n => n.Mesh)
				.WithMany(m => m.Nodes)
				.OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<Element>()
				.HasOne(e => e.Mesh)
				.WithMany(m => m.Elements)
				.OnDelete(DeleteBehavior.Restrict);

			// Mesh-Model relationship
			modelBuilder.Entity<Mesh>()
				.HasOne(m => m.Model)
				.WithMany(m => m.Meshes)
				.OnDelete(DeleteBehavior.Restrict);
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
		public DbSet<Element> Elements { get; set; }
	}
}
