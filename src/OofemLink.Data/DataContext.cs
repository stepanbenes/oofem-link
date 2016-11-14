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
		{
#if DEBUG
			Database.EnsureCreated();
#endif
		}

		public DataContext(DbContextOptions<DataContext> options)
			: base(options)
		{
#if DEBUG
			Database.EnsureCreated();
#endif
		}

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

			// GEOMETRY ENTITIES
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

			modelBuilder.Entity<MacroCurveMapping>().HasKey(m => new { m.ModelId, m.MacroId, m.CurveId });
			modelBuilder.Entity<MacroCurveMapping>().HasOne(m => m.Curve).WithMany().HasForeignKey(m => new { m.ModelId, m.CurveId });
			modelBuilder.Entity<MacroCurveMapping>().HasOne(m => m.Macro).WithMany(m => m.Curves).HasForeignKey(m => new { m.ModelId, m.MacroId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroCurveMapping>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<MacroSurfaceMapping>().HasKey(m => new { m.ModelId, m.MacroId, m.SurfaceId });
			modelBuilder.Entity<MacroSurfaceMapping>().HasOne(m => m.Surface).WithMany().HasForeignKey(m => new { m.ModelId, m.SurfaceId });
			modelBuilder.Entity<MacroSurfaceMapping>().HasOne(m => m.Macro).WithMany(m => m.Surfaces).HasForeignKey(m => new { m.ModelId, m.MacroId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroSurfaceMapping>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<MacroVolumeMapping>().HasKey(m => new { m.ModelId, m.MacroId, m.VolumeId });
			modelBuilder.Entity<MacroVolumeMapping>().HasOne(m => m.Volume).WithMany().HasForeignKey(m => new { m.ModelId, m.VolumeId });
			modelBuilder.Entity<MacroVolumeMapping>().HasOne(m => m.Macro).WithMany(m => m.Volumes).HasForeignKey(m => new { m.ModelId, m.MacroId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroVolumeMapping>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);

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

			// ATTRIBUTES
			modelBuilder.Entity<Entities.Attribute>().HasKey(a => (new { a.ModelId, a.Id }));

			modelBuilder.Entity<AttributeMapping>().HasKey(m => new { m.ModelId, m.MacroId, m.AttributeId });
			modelBuilder.Entity<AttributeMapping>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);
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

			// MESH-MODEL RELATIONSHIP
			modelBuilder.Entity<Mesh>()
				.HasOne(m => m.Model)
				.WithMany(m => m.Meshes)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<VertexNodeMapping>().HasKey(m => new { m.ModelId, m.VertexId, m.MeshId, m.NodeId });
			modelBuilder.Entity<VertexNodeMapping>().HasOne(m => m.Vertex).WithMany(v => v.Nodes).HasForeignKey(m => new { m.ModelId, m.VertexId });
			modelBuilder.Entity<VertexNodeMapping>().HasOne(m => m.Node).WithMany(n => n.Vertices).HasForeignKey(m => new { m.MeshId, m.NodeId });
			modelBuilder.Entity<VertexNodeMapping>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<VertexNodeMapping>().HasOne(m => m.Mesh).WithMany(m => m.VertexNodes).HasForeignKey(m => m.MeshId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Edge>().HasKey(e => new { e.ModelId, e.CurveId, e.MeshId, e.ElementId });
			modelBuilder.Entity<Edge>().HasOne(e => e.Curve).WithMany(c => c.Edges).HasForeignKey(e => new { e.ModelId, e.CurveId });
			modelBuilder.Entity<Edge>().HasOne(e => e.Element).WithMany(c => c.Edges).HasForeignKey(e => new { e.MeshId, e.ElementId });
			modelBuilder.Entity<Edge>().HasOne(e => e.Model).WithMany().HasForeignKey(e => e.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<Edge>().HasOne(e => e.Mesh).WithMany(m => m.Edges).HasForeignKey(e => e.MeshId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Face>().HasKey(f => new { f.ModelId, f.SurfaceId, f.MeshId, f.ElementId });
			modelBuilder.Entity<Face>().HasOne(f => f.Surface).WithMany(f => f.Faces).HasForeignKey(f => new { f.ModelId, f.SurfaceId });
			modelBuilder.Entity<Face>().HasOne(f => f.Element).WithMany(f => f.Faces).HasForeignKey(f => new { f.MeshId, f.ElementId });
			modelBuilder.Entity<Face>().HasOne(f => f.Model).WithMany().HasForeignKey(f => f.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<Face>().HasOne(f => f.Mesh).WithMany(m => m.Faces).HasForeignKey(f => f.MeshId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<VolumeElementMapping>().HasKey(m => new { m.ModelId, m.VolumeId, m.MeshId, m.ElementId });
			modelBuilder.Entity<VolumeElementMapping>().HasOne(m => m.Volume).WithMany(v => v.Elements).HasForeignKey(m => new { m.ModelId, m.VolumeId });
			modelBuilder.Entity<VolumeElementMapping>().HasOne(m => m.Element).WithMany(e => e.Volumes).HasForeignKey(m => new { m.MeshId, m.ElementId });
			modelBuilder.Entity<VolumeElementMapping>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<VolumeElementMapping>().HasOne(m => m.Mesh).WithMany(m => m.VolumeElements).HasForeignKey(m => m.MeshId).OnDelete(DeleteBehavior.Restrict);
		}

		public DbSet<Project> Projects { get; set; }
		public DbSet<Simulation> Simulations { get; set; }

		public DbSet<Model> Models { get; set; }

		public DbSet<Macro> Macros { get; set; }
		public DbSet<Entities.Attribute> Attributes { get; set; }
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
