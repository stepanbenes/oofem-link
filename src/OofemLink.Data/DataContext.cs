using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using OofemLink.Data.DbEntities;

namespace OofemLink.Data
{
	public class DataContext : DbContext
	{
		public DataContext(DbContextOptions<DataContext> options)
			: base(options)
		{
//#if DEBUG
			// TODO: do not call this in RELEASE configuration
			Database.EnsureCreated();
//#endif
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			// MODEL
			modelBuilder.Entity<Project>().HasIndex(p => p.Name).IsUnique();

			modelBuilder.Entity<Simulation>()
				.HasOne(s => s.Model)
				.WithMany(m => m.Simulations)
				.HasForeignKey(s => s.ModelId)
				.IsRequired(false)
				.OnDelete(DeleteBehavior.SetNull);

			modelBuilder.Entity<TimeFunction>().HasKey(tf => new { tf.ModelId, tf.Id });
			modelBuilder.Entity<TimeFunction>().HasOne(tf => tf.Model).WithMany(m => m.TimeFunctions).HasForeignKey(tf => tf.ModelId);
			modelBuilder.Entity<ConstantFunction>(); // let the data model know about existence of concrete implementations of TimeFunction abstract base class
			modelBuilder.Entity<PeakFunction>();
			modelBuilder.Entity<PiecewiseLinFunction>();

			modelBuilder.Entity<TimeFunctionValue>().HasKey(v => new { v.ModelId, v.TimeFunctionId, v.TimeStepId });
			modelBuilder.Entity<TimeFunctionValue>().HasOne(v => v.Model).WithMany().HasForeignKey(v => v.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<TimeFunctionValue>().HasOne(v => v.TimeStep).WithMany().HasForeignKey(v => v.TimeStepId);
			modelBuilder.Entity<TimeFunctionValue>().HasOne(v => v.TimeFunction).WithMany(tf => tf.Values).HasForeignKey(v => new { v.ModelId, v.TimeFunctionId });

			// GEOMETRY ENTITIES
			modelBuilder.Entity<Vertex>().HasKey(e => new { e.ModelId, e.Id });
			modelBuilder.Entity<Curve>().HasKey(e => new { e.ModelId, e.Id });
			modelBuilder.Entity<Surface>().HasKey(e => new { e.ModelId, e.Id });
			modelBuilder.Entity<Volume>().HasKey(e => new { e.ModelId, e.Id });

			modelBuilder.Entity<VertexCurve>().HasKey(m => new { m.ModelId, m.VertexId, m.CurveId });
			modelBuilder.Entity<VertexCurve>().HasOne(m => m.Vertex).WithMany(v => v.VertexCurves).HasForeignKey(m => new { m.ModelId, m.VertexId });
			modelBuilder.Entity<VertexCurve>().HasOne(m => m.Curve).WithMany(c => c.CurveVertices).HasForeignKey(m => new { m.ModelId, m.CurveId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<VertexCurve>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<VertexCurve>().ToTable("VertexCurves");

			modelBuilder.Entity<SurfaceCurve>().HasKey(m => new { m.ModelId, m.CurveId, m.SurfaceId });
			modelBuilder.Entity<SurfaceCurve>().HasOne(m => m.Curve).WithMany(c => c.CurveSurfaces).HasForeignKey(m => new { m.ModelId, m.CurveId });
			modelBuilder.Entity<SurfaceCurve>().HasOne(m => m.Surface).WithMany(s => s.SurfaceCurves).HasForeignKey(m => new { m.ModelId, m.SurfaceId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<SurfaceCurve>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<SurfaceCurve>().ToTable("SurfaceCurves");

			modelBuilder.Entity<SurfaceVolume>().HasKey(m => new { m.ModelId, m.SurfaceId, m.VolumeId });
			modelBuilder.Entity<SurfaceVolume>().HasOne(m => m.Surface).WithMany(s => s.SurfaceVolumes).HasForeignKey(m => new { m.ModelId, m.SurfaceId });
			modelBuilder.Entity<SurfaceVolume>().HasOne(m => m.Volume).WithMany(v => v.VolumeSurfaces).HasForeignKey(m => new { m.ModelId, m.VolumeId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<SurfaceVolume>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<SurfaceVolume>().ToTable("SurfaceVolumes");

			modelBuilder.Entity<Macro>().HasKey(e => new { e.ModelId, e.Id });

			modelBuilder.Entity<MacroCurve>().HasKey(m => new { m.ModelId, m.MacroId, m.CurveId });
			modelBuilder.Entity<MacroCurve>().HasOne(m => m.Curve).WithMany().HasForeignKey(m => new { m.ModelId, m.CurveId });
			modelBuilder.Entity<MacroCurve>().HasOne(m => m.Macro).WithMany(m => m.MacroCurves).HasForeignKey(m => new { m.ModelId, m.MacroId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroCurve>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroCurve>().ToTable("MacroCurves");

			modelBuilder.Entity<MacroSurface>().HasKey(m => new { m.ModelId, m.MacroId, m.SurfaceId });
			modelBuilder.Entity<MacroSurface>().HasOne(m => m.Surface).WithMany().HasForeignKey(m => new { m.ModelId, m.SurfaceId });
			modelBuilder.Entity<MacroSurface>().HasOne(m => m.Macro).WithMany(m => m.MacroSurfaces).HasForeignKey(m => new { m.ModelId, m.MacroId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroSurface>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroSurface>().ToTable("MacroSurfaces");

			modelBuilder.Entity<MacroVolume>().HasKey(m => new { m.ModelId, m.MacroId, m.VolumeId });
			modelBuilder.Entity<MacroVolume>().HasOne(m => m.Volume).WithMany().HasForeignKey(m => new { m.ModelId, m.VolumeId });
			modelBuilder.Entity<MacroVolume>().HasOne(m => m.Macro).WithMany(m => m.MacroVolumes).HasForeignKey(m => new { m.ModelId, m.MacroId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroVolume>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroVolume>().ToTable("MacroVolumes");

			modelBuilder.Entity<MacroInternalCurve>().HasKey(m => new { m.ModelId, m.MacroId, m.InternalCurveId });
			modelBuilder.Entity<MacroInternalCurve>().HasOne(m => m.InternalCurve).WithMany().HasForeignKey(m => new { m.ModelId, m.InternalCurveId });
			modelBuilder.Entity<MacroInternalCurve>().HasOne(m => m.Macro).WithMany(m => m.MacroInternalCurves).HasForeignKey(m => new { m.ModelId, m.MacroId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroInternalCurve>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroInternalCurve>().ToTable("MacroInternalCurves");

			modelBuilder.Entity<MacroInternalVertex>().HasKey(m => new { m.ModelId, m.MacroId, m.InternalVertexId });
			modelBuilder.Entity<MacroInternalVertex>().HasOne(m => m.InternalVertex).WithMany().HasForeignKey(m => new { m.ModelId, m.InternalVertexId });
			modelBuilder.Entity<MacroInternalVertex>().HasOne(m => m.Macro).WithMany(m => m.MacroInternalVertices).HasForeignKey(m => new { m.ModelId, m.MacroId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroInternalVertex>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroInternalVertex>().ToTable("MacroInternalVertices");

			modelBuilder.Entity<MacroOpeningCurve>().HasKey(m => new { m.ModelId, m.MacroId, m.OpeningCurveId });
			modelBuilder.Entity<MacroOpeningCurve>().HasOne(m => m.OpeningCurve).WithMany().HasForeignKey(m => new { m.ModelId, m.OpeningCurveId });
			modelBuilder.Entity<MacroOpeningCurve>().HasOne(m => m.Macro).WithMany(m => m.MacroOpeningCurves).HasForeignKey(m => new { m.ModelId, m.MacroId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroOpeningCurve>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MacroOpeningCurve>().ToTable("MacroOpeningCurves");

			// ATTRIBUTES
			modelBuilder.Entity<ModelAttribute>().HasKey(a => new { a.ModelId, a.Id });
			modelBuilder.Entity<ModelAttribute>().HasOne(a => a.Model).WithMany(m => m.Attributes).HasForeignKey(a => a.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<ModelAttribute>().HasOne(a => a.TimeFunction).WithMany().HasForeignKey(a => new { a.ModelId, a.TimeFunctionId });

			modelBuilder.Entity<AttributeComposition>().HasKey(c => new { c.ModelId, c.ParentAttributeId, c.ChildAttributeId });
			modelBuilder.Entity<AttributeComposition>().HasOne(c => c.Model).WithMany().HasForeignKey(c => c.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<AttributeComposition>().HasOne(c => c.ParentAttribute).WithMany(a => a.ChildAttributes).HasForeignKey(c => new { c.ModelId, c.ParentAttributeId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<AttributeComposition>().HasOne(c => c.ChildAttribute).WithMany(a => a.ParentAttributes).HasForeignKey(c => new { c.ModelId, c.ChildAttributeId });
			modelBuilder.Entity<AttributeComposition>().ToTable("AttributeCompositions");


			modelBuilder.Entity<VertexAttribute>().HasOne(a => a.Model).WithMany().HasForeignKey(a => a.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<VertexAttribute>().HasOne(a => a.Vertex).WithMany(v => v.VertexAttributes).HasForeignKey(a => new { a.ModelId, a.VertexId });
			modelBuilder.Entity<VertexAttribute>().HasOne(m => m.Attribute).WithMany(a => a.VertexAttributes).HasForeignKey(m => new { m.ModelId, m.AttributeId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<VertexAttribute>().ToTable("VertexAttributes");

			modelBuilder.Entity<CurveAttribute>().HasOne(a => a.Model).WithMany().HasForeignKey(a => a.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<CurveAttribute>().HasOne(a => a.Curve).WithMany(c => c.CurveAttributes).HasForeignKey(a => new { a.ModelId, a.CurveId });
			modelBuilder.Entity<CurveAttribute>().HasOne(m => m.Attribute).WithMany(a => a.CurveAttributes).HasForeignKey(m => new { m.ModelId, m.AttributeId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<CurveAttribute>().HasOne(m => m.Macro).WithMany().HasForeignKey(m => new { m.ModelId, m.MacroId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<CurveAttribute>().ToTable("CurveAttributes");

			modelBuilder.Entity<SurfaceAttribute>().HasOne(a => a.Model).WithMany().HasForeignKey(a => a.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<SurfaceAttribute>().HasOne(a => a.Surface).WithMany(s => s.SurfaceAttributes).HasForeignKey(a => new { a.ModelId, a.SurfaceId });
			modelBuilder.Entity<SurfaceAttribute>().HasOne(m => m.Attribute).WithMany(a => a.SurfaceAttributes).HasForeignKey(m => new { m.ModelId, m.AttributeId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<SurfaceAttribute>().HasOne(m => m.Macro).WithMany().HasForeignKey(m => new { m.ModelId, m.MacroId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<SurfaceAttribute>().ToTable("SurfaceAttributes");

			modelBuilder.Entity<VolumeAttribute>().HasOne(a => a.Model).WithMany().HasForeignKey(a => a.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<VolumeAttribute>().HasOne(a => a.Volume).WithMany(v => v.VolumeAttributes).HasForeignKey(a => new { a.ModelId, a.VolumeId });
			modelBuilder.Entity<VolumeAttribute>().HasOne(m => m.Attribute).WithMany(a => a.VolumeAttributes).HasForeignKey(m => new { m.ModelId, m.AttributeId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<VolumeAttribute>().HasOne(m => m.Macro).WithMany().HasForeignKey(m => new { m.ModelId, m.MacroId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<VolumeAttribute>().ToTable("VolumeAttributes");

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
				.HasForeignKey(en => new { en.MeshId, en.NodeId })
				.OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<ElementNode>()
				.HasKey(m => new { m.MeshId, m.ElementId, m.NodeId });
			modelBuilder.Entity<ElementNode>()
				.ToTable("ElementNodes");

			modelBuilder.Entity<Node>()
				.HasOne(n => n.Mesh)
				.WithMany(m => m.Nodes);
			modelBuilder.Entity<Element>()
				.HasOne(e => e.Mesh)
				.WithMany(m => m.Elements);

			// MESH-MODEL RELATIONSHIP
			modelBuilder.Entity<Mesh>()
				.HasOne(m => m.Model)
				.WithMany(m => m.Meshes);

			modelBuilder.Entity<VertexNode>().HasKey(m => new { m.ModelId, m.VertexId, m.MeshId, m.NodeId });
			modelBuilder.Entity<VertexNode>().HasOne(m => m.Vertex).WithMany(v => v.VertexNodes).HasForeignKey(m => new { m.ModelId, m.VertexId });
			modelBuilder.Entity<VertexNode>().HasOne(m => m.Node).WithMany(n => n.VertexNodes).HasForeignKey(m => new { m.MeshId, m.NodeId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<VertexNode>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<VertexNode>().HasOne(m => m.Mesh).WithMany(m => m.VertexNodes).HasForeignKey(m => m.MeshId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<VertexNode>().ToTable("VertexNodes");

			modelBuilder.Entity<CurveElement>().HasKey(e => new { e.ModelId, e.CurveId, e.MeshId, e.ElementId });
			modelBuilder.Entity<CurveElement>().HasOne(e => e.Curve).WithMany(c => c.CurveElements).HasForeignKey(e => new { e.ModelId, e.CurveId });
			modelBuilder.Entity<CurveElement>().HasOne(e => e.Element).WithMany(c => c.Edges).HasForeignKey(e => new { e.MeshId, e.ElementId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<CurveElement>().HasOne(e => e.Model).WithMany().HasForeignKey(e => e.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<CurveElement>().HasOne(e => e.Mesh).WithMany(m => m.CurveElements).HasForeignKey(e => e.MeshId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<CurveElement>().ToTable("CurveElements");

			modelBuilder.Entity<SurfaceElement>().HasKey(f => new { f.ModelId, f.SurfaceId, f.MeshId, f.ElementId });
			modelBuilder.Entity<SurfaceElement>().HasOne(f => f.Surface).WithMany(f => f.SurfaceElements).HasForeignKey(f => new { f.ModelId, f.SurfaceId });
			modelBuilder.Entity<SurfaceElement>().HasOne(f => f.Element).WithMany(f => f.Faces).HasForeignKey(f => new { f.MeshId, f.ElementId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<SurfaceElement>().HasOne(f => f.Model).WithMany().HasForeignKey(f => f.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<SurfaceElement>().HasOne(f => f.Mesh).WithMany(m => m.SurfaceElements).HasForeignKey(f => f.MeshId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<SurfaceElement>().ToTable("SurfaceElements");

			modelBuilder.Entity<VolumeElement>().HasKey(m => new { m.ModelId, m.VolumeId, m.MeshId, m.ElementId });
			modelBuilder.Entity<VolumeElement>().HasOne(m => m.Volume).WithMany(v => v.VolumeElements).HasForeignKey(m => new { m.ModelId, m.VolumeId });
			modelBuilder.Entity<VolumeElement>().HasOne(m => m.Element).WithMany(e => e.Volumes).HasForeignKey(m => new { m.MeshId, m.ElementId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<VolumeElement>().HasOne(m => m.Model).WithMany().HasForeignKey(m => m.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<VolumeElement>().HasOne(m => m.Mesh).WithMany(m => m.VolumeElements).HasForeignKey(m => m.MeshId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<VolumeElement>().ToTable("VolumeElements");

			modelBuilder.Entity<CurveNode>().HasKey(e => new { e.ModelId, e.CurveId, e.MeshId, e.NodeId });
			modelBuilder.Entity<CurveNode>().HasOne(e => e.Curve).WithMany(c => c.CurveNodes).HasForeignKey(e => new { e.ModelId, e.CurveId });
			modelBuilder.Entity<CurveNode>().HasOne(e => e.Node).WithMany().HasForeignKey(e => new { e.MeshId, e.NodeId }).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<CurveNode>().HasOne(e => e.Model).WithMany().HasForeignKey(e => e.ModelId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<CurveNode>().HasOne(e => e.Mesh).WithMany(m => m.CurveNodes).HasForeignKey(e => e.MeshId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<CurveNode>().ToTable("CurveNodes");
		}

		public DbSet<Project> Projects { get; set; }
		public DbSet<Simulation> Simulations { get; set; }

		public DbSet<Model> Models { get; set; }

		public DbSet<Macro> Macros { get; set; }
		public DbSet<ModelAttribute> Attributes { get; set; }
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
