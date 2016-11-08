using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using OofemLink.Data.Entities;

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
			//modelBuilder.Entity<VertexNodeMap>()
			//	.HasOne(m => m.Vertex)
			//	.WithMany(v => v.VertexNodeMap)
			//	.HasForeignKey(m => new { m.ModelId, m.VertexId });
			//modelBuilder.Entity<VertexNodeMap>()
			//	.HasOne(m => m.Node)
			//	.WithMany(n => n.VertexNodeMap)
			//	.HasForeignKey(m => new { m.MeshId, m.NodeId });
			//modelBuilder.Entity<VertexNodeMap>()
			//	.HasKey(m => new { m.ModelId, m.VertexId, m.MeshId, m.NodeId });

			modelBuilder.Entity<Vertex>().HasKey(e => new { e.ModelId, e.Id });
			modelBuilder.Entity<Curve>().HasKey(e => new { e.ModelId, e.Id });
			modelBuilder.Entity<Surface>().HasKey(e => new { e.ModelId, e.Id });
			modelBuilder.Entity<Volume>().HasKey(e => new { e.ModelId, e.Id });

			modelBuilder.Entity<VertexCurveMapping>().HasKey(m => new { m.ModelId, m.VertexId, m.CurveId });
			modelBuilder.Entity<VertexCurveMapping>().HasOne(m => m.Vertex).WithMany(v => v.Curves).HasForeignKey(m => new { m.ModelId, m.VertexId });
			modelBuilder.Entity<VertexCurveMapping>().HasOne(m => m.Curve).WithMany(c => c.Vertices).HasForeignKey(m => new { m.ModelId, m.CurveId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<VertexCurveMapping>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<CurveSurfaceMapping>().HasKey(m => new { m.ModelId, m.CurveId, m.SurfaceId });
			modelBuilder.Entity<CurveSurfaceMapping>().HasOne(m => m.Curve).WithMany(c => c.Surfaces).HasForeignKey(m => new { m.ModelId, m.CurveId });
			modelBuilder.Entity<CurveSurfaceMapping>().HasOne(m => m.Surface).WithMany(s => s.Curves).HasForeignKey(m => new { m.ModelId, m.SurfaceId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<CurveSurfaceMapping>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<SurfaceVolumeMapping>().HasKey(m => new { m.ModelId, m.SurfaceId, m.VolumeId });
			modelBuilder.Entity<SurfaceVolumeMapping>().HasOne(m => m.Surface).WithMany(s => s.Volumes).HasForeignKey(m => new { m.ModelId, m.SurfaceId });
			modelBuilder.Entity<SurfaceVolumeMapping>().HasOne(m => m.Volume).WithMany(v => v.Surfaces).HasForeignKey(m => new { m.ModelId, m.VolumeId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<SurfaceVolumeMapping>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Macro>().HasKey(e => new { e.ModelId, e.Id });

			modelBuilder.Entity<MacroBoundaryCurveMapping>().HasKey(m => new { m.ModelId, m.MacroId, m.BoundaryCurveId });
			modelBuilder.Entity<MacroBoundaryCurveMapping>().HasOne(m => m.BoundaryCurve).WithMany().HasForeignKey(m => new { m.ModelId, m.BoundaryCurveId });
			modelBuilder.Entity<MacroBoundaryCurveMapping>().HasOne(m => m.Macro).WithMany(m => m.BoundaryCurves).HasForeignKey(m => new { m.ModelId, m.MacroId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroBoundaryCurveMapping>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<MacroInternalCurveMapping>().HasKey(m => new { m.ModelId, m.MacroId, m.InternalCurveId });
			modelBuilder.Entity<MacroInternalCurveMapping>().HasOne(m => m.InternalCurve).WithMany().HasForeignKey(m => new { m.ModelId, m.InternalCurveId });
			modelBuilder.Entity<MacroInternalCurveMapping>().HasOne(m => m.Macro).WithMany(m => m.InternalCurves).HasForeignKey(m => new { m.ModelId, m.MacroId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroInternalCurveMapping>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<MacroInternalVertexMapping>().HasKey(m => new { m.ModelId, m.MacroId, m.InternalVertexId });
			modelBuilder.Entity<MacroInternalVertexMapping>().HasOne(m => m.InternalVertex).WithMany().HasForeignKey(m => new { m.ModelId, m.InternalVertexId });
			modelBuilder.Entity<MacroInternalVertexMapping>().HasOne(m => m.Macro).WithMany(m => m.InternalVertices).HasForeignKey(m => new { m.ModelId, m.MacroId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroInternalVertexMapping>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<MacroOpeningCurveMapping>().HasKey(m => new { m.ModelId, m.MacroId, m.OpeningCurveId });
			modelBuilder.Entity<MacroOpeningCurveMapping>().HasOne(m => m.OpeningCurve).WithMany().HasForeignKey(m => new { m.ModelId, m.OpeningCurveId });
			modelBuilder.Entity<MacroOpeningCurveMapping>().HasOne(m => m.Macro).WithMany(m => m.OpeningCurves).HasForeignKey(m => new { m.ModelId, m.MacroId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroOpeningCurveMapping>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<MacroBoundarySurfaceMapping>().HasKey(m => new { m.ModelId, m.MacroId, m.BoundarySurfaceId });
			modelBuilder.Entity<MacroBoundarySurfaceMapping>().HasOne(m => m.BoundarySurface).WithMany().HasForeignKey(m => new { m.ModelId, m.BoundarySurfaceId });
			modelBuilder.Entity<MacroBoundarySurfaceMapping>().HasOne(m => m.Macro).WithMany(m => m.BoundarySurfaces).HasForeignKey(m => new { m.ModelId, m.MacroId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroBoundarySurfaceMapping>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);

			// ATTRIBUTES
			modelBuilder.Entity<MacroAttribute>().HasKey(a => new { a.ModelId, a.Id });

			modelBuilder.Entity<AttributeMapping>().HasKey(m => new { m.ModelId, m.MacroId, m.AttributeId });
			modelBuilder.Entity<AttributeMapping>().HasOne(m => m.Model).WithMany().HasForeignKey(m => new { m.ModelId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<AttributeMapping>().HasOne(m => m.TimeFunction).WithMany(t => t.AttributeMappings).HasForeignKey(m => m.TimeFunctionId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<AttributeMapping>().HasOne(m => m.Attribute).WithMany(a => a.AttributeMappings).HasForeignKey(m => new { m.ModelId, m.AttributeId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<AttributeMapping>().HasOne(m => m.Macro).WithMany(m => m.Attributes).HasForeignKey(m => new { m.ModelId, m.MacroId });

			// MESH
			modelBuilder.Entity<Node>().HasKey(b => new { b.MeshId, b.Id });
			modelBuilder.Entity<Element>().HasKey(b => new { b.MeshId, b.Id });

			// ElementNodes
			modelBuilder.Entity<ElementNode>()
				.HasOne(en => en.Element)
				.WithMany(e => e.ElementNodes)
				.HasForeignKey(en => new { en.MeshId, en.ElementId });
			modelBuilder.Entity<ElementNode>()
				.HasOne(en => en.Node)
				.WithMany(n => n.ElementNodes)
				.HasForeignKey(en => new { en.MeshId, en.NodeId });
			modelBuilder.Entity<ElementNode>()
				.HasKey(m => new { m.MeshId, m.ElementId, m.NodeId });
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
		public DbSet<MacroAttribute> MacroAttributes { get; set; }
		public DbSet<TimeFunction> TimeFunctions { get; set; }

		public DbSet<Vertex> Vertices { get; set; }
		public DbSet<Curve> Curves { get; set; }
		public DbSet<Surface> Surfaces { get; set; }
		public DbSet<Volume> Volumes { get; set; }

		public DbSet<Mesh> Meshes { get; set; }
		public DbSet<Node> Nodes { get; set; }
		public DbSet<Element> Elements { get; set; }
	}
}
