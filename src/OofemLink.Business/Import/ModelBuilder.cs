using System;
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
		readonly Dictionary<int, int> curveLocalNumberToIdMap;

		private ModelBuilder(Model model)
		{
			Debug.Assert(model != null);
			this.model = model;
			this.curveLocalNumberToIdMap = new Dictionary<int, int>();
		}

		public ModelBuilder()
			: this(new Model())
		{ }

		public static ModelBuilder CreateFromExistingModel(Model model) => new ModelBuilder(model);

		public Model Model => model;
		
		public ModelBuilder AddVertex(int id, double x, double y, double z)
		{
			var vertex = new Vertex { Id = id, X = x, Y = y, Z = z };
			model.Vertices.Add(vertex);
			return this;
		}

		public ModelBuilder AddStraightLine(int lineId, int firstVertexId, int secondVertexId)
		{
			int entityId = model.GeometryEntities.Count + 1;
			var straightLine = new Curve { Id = entityId, LocalNumber = lineId };
			straightLine.Vertices.Add(new VertexCurveMapping { Model = model, Curve = straightLine, VertexId = firstVertexId, Rank = 1 });
			straightLine.Vertices.Add(new VertexCurveMapping { Model = model, Curve = straightLine, VertexId = secondVertexId, Rank = 2 });
			model.GeometryEntities.Add(straightLine);
			curveLocalNumberToIdMap.Add(straightLine.LocalNumber, straightLine.Id);
			return this;
		}

		public ModelBuilder AddBeamMacro(int macroId, int lineId)
		{
			var macro = new Macro { Model = model, Id = macroId, GeometryEntityId = curveLocalNumberToIdMap[lineId] };
			model.Macros.Add(macro);
			return this;
		}

		public ModelBuilder AddSurfaceMacro(int macroId, IEnumerable<int> perimeterLineIds)
		{
			int surfaceId = model.GeometryEntities.Count + 1;
			var surface = new Surface { Model = model, Id = surfaceId };
			short rank = 1;
			foreach (var lineId in perimeterLineIds)
			{
				surface.Curves.Add(new CurveSurfaceMapping { Model = model, CurveId = curveLocalNumberToIdMap[lineId], SurfaceId = surfaceId, Rank = rank });
				rank += 1;
			}
			var macro = new Macro { Model = model, Id = macroId, GeometryEntityId = surfaceId };
			model.GeometryEntities.Add(surface);
			model.Macros.Add(macro);
			return this;
		}
	}
}
