using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Services.Import;
using OofemLink.Common.Enumerations;
using OofemLink.Data;
using OofemLink.Data.Entities;

namespace OofemLink.Services.Import
{
	public class ModelBuilder
	{
		readonly Model model;

		private ModelBuilder(Model model)
		{
			Debug.Assert(model != null);
			this.model = model;
		}

		public ModelBuilder()
			: this(new Model())
		{ }

		public static ModelBuilder CreateFromExistingModel(Model model)
		{
			return new ModelBuilder(model);
		}

		public Model Model => model;

		public ModelBuilder AddVertex(int vertexId, double x, double y, double z)
		{
			var vertex = new Vertex { Id = vertexId, X = x, Y = y, Z = z };
			model.Vertices.Add(vertex);
			return this;
		}

		public ModelBuilder AddCurve(int lineId, CurveType type, IEnumerable<int> vertexIds)
		{
			var curve = new Curve { Id = lineId, Type = type };
			short rank = 1;
			foreach (var vertexId in vertexIds)
			{
				curve.CurveVertices.Add(new VertexCurve { Model = model, Curve = curve, VertexId = vertexId, Rank = rank });
				rank += 1;
			}
			model.Curves.Add(curve);
			return this;
		}

		public ModelBuilder AddSurface(int surfaceId, SurfaceType type, IEnumerable<int> boundaryLineIds)
		{
			var surface = new Surface { Model = model, Id = surfaceId, Type = type };
			short rank = 1;
			foreach (var lineId in boundaryLineIds)
			{
				surface.SurfaceCurves.Add(new SurfaceCurve { Model = model, CurveId = Math.Abs(lineId), Surface = surface, Rank = rank, IsInversed = lineId < 0 });
				rank += 1;
			}
			model.Surfaces.Add(surface);
			return this;
		}

		public ModelBuilder AddBeamMacro(int macroId, IEnumerable<int> lineIds)
		{
			var macro = new Macro { Model = model, Id = macroId };
			short rank = 1;
			foreach (var lineId in lineIds)
			{
				macro.MacroCurves.Add(new MacroCurve { Model = model, MacroId = macroId, CurveId = lineId, Rank = rank });
				rank += 1;
			}
			model.Macros.Add(macro);
			return this;
		}

		public ModelBuilder AddGeneralSurfaceMacro(int macroId, IEnumerable<int> boundaryLineIds, IEnumerable<int> openingLineIds, IEnumerable<int> internalLineIds, IEnumerable<int> internalVertexIds)
		{
			int surfaceId = macroId;
			AddSurface(surfaceId, SurfaceType.General, boundaryLineIds);
			var macro = new Macro { Model = model, Id = macroId };
			
			// Boundary
			macro.MacroSurfaces.Add(new MacroSurface { Model = model, SurfaceId = surfaceId, MacroId = macroId });

			// Opening (hole)
			short rank = 1;
			foreach (var openingLineId in openingLineIds)
			{
				Debug.Assert(openingLineId > 0); // TODO: really?
				macro.MacroOpeningCurves.Add(new MacroOpeningCurve { Model = model, OpeningCurveId = openingLineId, MacroId = macroId, Rank = rank });
				rank += 1;
			}

			// Internal lines
			rank = 1;
			foreach (var internalLineId in internalLineIds)
			{
				Debug.Assert(internalLineId > 0); // TODO: really?
				macro.MacroInternalCurves.Add(new MacroInternalCurve { Model = model, InternalCurveId = internalLineId, MacroId = macroId, Rank = rank });
				rank += 1;
			}

			// Internal vertices
			foreach (var internalVertexId in internalVertexIds)
			{
				macro.MacroInternalVertices.Add(new MacroInternalVertex { Model = model, InternalVertexId = internalVertexId, MacroId = macroId });
			}

			model.Macros.Add(macro);
			return this;
		}
	}
}
