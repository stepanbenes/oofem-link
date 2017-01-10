using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.MathPhys;
using OofemLink.Data.Entities;

namespace OofemLink.Services.Import
{
	public class CoordinateTransformService
	{
		readonly Model model;

		public CoordinateTransformService(Model model)
		{
			this.model = model;
		}

		public Vector3d CalculateZAxisForLineFromAngleAroundLocalXAxis(int lineId, double angle /*alpha*/)
		{
			throw new NotImplementedException();
		}

		public Vector3d CalculateLocalZAxisForLineFromGlobalYAxisTargetPoint(int lineId, Vector3d yTargetPoint)
		{
			int vertex1Id, vertex2Id;
			if (!tryGetLineVertices(lineId, out vertex1Id, out vertex2Id))
				throw new KeyNotFoundException($"Line with id {lineId} was not found");

			Vertex v1, v2;
			if (!tryGetVertex(vertex1Id, out v1))
				throw new KeyNotFoundException($"Vertex with id {vertex1Id} was not found");
			if (!tryGetVertex(vertex2Id, out v2))
				throw new KeyNotFoundException($"Vertex with id {vertex2Id} was not found");

			Vector3d point1 = new Vector3d(v1.X, v1.Y, v1.Z);
			Vector3d point2 = new Vector3d(v2.X, v2.Y, v2.Z);

			Vector3d xAxis = point2 - point1;
			Vector3d yAxis = yTargetPoint - point1;
			Vector3d zAxis;
			Vector3d.Cross(ref xAxis, ref yAxis, out zAxis);
			zAxis.Normalize();
			return zAxis;
		}

		public Vector3d CalculateLocalZAxisForLineFromGlobalZAxisTargetPoint(int lineId, Vector3d zTargetPoint)
		{
			Curve line;
			if (!tryGetCurve(lineId, out line))
				throw new KeyNotFoundException($"Line with id {lineId} was not found");

			Debug.Assert(line.CurveVertices.Count == 2);
			int vertex1Id = line.CurveVertices.ElementAt(0).VertexId;

			Vertex v1;
			if (!tryGetVertex(vertex1Id, out v1))
				throw new KeyNotFoundException($"Vertex with id {vertex1Id} was not found");

			Vector3d point1 = new Vector3d(v1.X, v1.Y, v1.Z);
			Vector3d zAxis = zTargetPoint - point1;
			zAxis.Normalize();
			return zAxis;
		}

		#region Private methods

		private bool tryGetLineVertices(int lineId, out int vertex1Id, out int vertex2Id)
		{
			Curve line;
			if (!tryGetCurve(lineId, out line))
			{
				vertex1Id = vertex2Id = 0;
				return false;
			}
			Debug.Assert(line.CurveVertices.Count == 2);
			vertex1Id = line.CurveVertices.ElementAt(0).VertexId;
			vertex2Id = line.CurveVertices.ElementAt(1).VertexId;
			return true;
		}

		private bool tryGetCurve(int curveId, out Curve curve)
		{
			// TODO: [Optimization] lazy initialize cache for all model lines
			curve = model.Curves.Where(c => c.Id == curveId).SingleOrDefault();
			return curve != null;
		}

		private bool tryGetVertex(int vertexId, out Vertex vertex)
		{
			// TODO: [Optimization] lazy initialize cache for all model vertices
			vertex = model.Vertices.Where(v => v.Id == vertexId).SingleOrDefault();
			return vertex != null;
		}

		#endregion
	}
}
