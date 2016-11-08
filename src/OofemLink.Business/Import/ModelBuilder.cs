﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Business.Import;
using OofemLink.Data;
using OofemLink.Data.Entities;

namespace OofemLink.Business.Import
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

		public ModelBuilder AddStraightLine(int lineId, int firstVertexId, int secondVertexId)
		{
			var straightLine = new Curve { Id = lineId, LocalNumber = lineId };
			straightLine.Vertices.Add(new VertexCurveMapping { Model = model, Curve = straightLine, VertexId = firstVertexId, Rank = 1 });
			straightLine.Vertices.Add(new VertexCurveMapping { Model = model, Curve = straightLine, VertexId = secondVertexId, Rank = 2 });
			model.Curves.Add(straightLine);
			return this;
		}

		public ModelBuilder AddBeamMacro(int macroId, int lineId)
		{
			var macro = new Macro { Model = model, Id = macroId };
			macro.BoundaryCurves.Add(new MacroBoundaryCurveMapping { Model = model, MacroId = macroId, BoundaryCurveId = lineId, Rank = 1 });
			model.Macros.Add(macro);
			return this;
		}

		public ModelBuilder AddSurface(int surfaceId, IEnumerable<int> boundaryLineIds)
		{
			var surface = new Surface { Model = model, Id = surfaceId };
			short rank = 1;
			foreach (var lineId in boundaryLineIds)
			{
				surface.Curves.Add(new CurveSurfaceMapping { Model = model, CurveId = lineId, Surface = surface, Rank = rank });
				rank += 1;
			}
			model.Surfaces.Add(surface);
			return this;
		}

		public ModelBuilder AddSurfaceMacro(int macroId, IEnumerable<int> boundaryLineIds, IEnumerable<int> openingLineIds, IEnumerable<int> internalLineIds, IEnumerable<int> internalVertexIds)
		{
			int surfaceId = macroId;
			AddSurface(surfaceId, boundaryLineIds);
			var macro = new Macro { Model = model, Id = macroId };
			
			// Boundary
			macro.BoundarySurfaces.Add(new MacroBoundarySurfaceMapping { Model = model, BoundarySurfaceId = surfaceId, MacroId = macroId, Rank = 1 });

			// Opening (hole)
			short rank = 1;
			foreach (var openingLineId in openingLineIds)
			{
				macro.OpeningCurves.Add(new MacroOpeningCurveMapping { Model = model, OpeningCurveId = openingLineId, MacroId = macroId, Rank = rank });
				rank += 1;
			}

			// Internal lines
			rank = 1;
			foreach (var internalLineId in internalLineIds)
			{
				macro.InternalCurves.Add(new MacroInternalCurveMapping { Model = model, InternalCurveId = internalLineId, MacroId = macroId, Rank = rank });
				rank += 1;
			}

			// Internal vertices
			foreach (var internalVertexId in internalVertexIds)
			{
				macro.InternalVertices.Add(new MacroInternalVertexMapping { Model = model, InternalVertexId = internalVertexId, MacroId = macroId });
			}

			model.Macros.Add(macro);
			return this;
		}
	}
}
