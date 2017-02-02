using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.MathPhys;
using OofemLink.Data.DbEntities;

namespace OofemLink.Services.Import
{
	public class CoordinateTransformService
	{
		readonly Model model;
		readonly Dictionary<int, CS> curveLcsCache;

		public CoordinateTransformService(Model model)
		{
			this.model = model;
			this.curveLcsCache = new Dictionary<int, CS>();
		}

		public Vector3d CalculateLocalZAxisForLineFromAngleAroundLocalXAxis(int lineId, double angle /*alpha in DEGrees*/)
		{
			CS curveLcs = calculateLcsForCurve(lineId);
			double angleInRad = ComputationalGeometry.Deg2Rad(angle);
			Vector3d rotatedYAxis = ComputationalGeometry.RotateVector(curveLcs.YAxis, angleInRad, curveLcs.XAxis); // rotate zAxis around xAxis by angleInRad
			CS newCurveLcs = new CS(curveLcs.XAxis, rotatedYAxis);

			curveLcsCache[lineId] = newCurveLcs;

			return newCurveLcs.ZAxis;
		}

		public Vector3d CalculateLocalZAxisForLineFromGlobalYAxisTargetPoint(int lineId, Vector3d yTargetPoint)
		{
			Vertex v1, v2;
			GetVerticesOfLine(lineId, out v1, out v2);

			Vector3d point1 = new Vector3d(v1.X, v1.Y, v1.Z);
			Vector3d point2 = new Vector3d(v2.X, v2.Y, v2.Z);

			Vector3d xAxis = Vector3d.Normalize(point2 - point1);
			Vector3d yAxis = Vector3d.Normalize(yTargetPoint - point1);
			Vector3d zAxis = Vector3d.Cross(xAxis, yAxis);

			curveLcsCache[lineId] = new CS(xAxis, yAxis);

			return zAxis;
		}

		public Vector3d CalculateLocalZAxisForLineFromGlobalZAxisTargetPoint(int lineId, Vector3d zTargetPoint)
		{
			Vertex v1, v2;
			GetVerticesOfLine(lineId, out v1, out v2);

			Vector3d point1 = new Vector3d(v1.X, v1.Y, v1.Z);
			Vector3d point2 = new Vector3d(v2.X, v2.Y, v2.Z);

			Vector3d xAxis = Vector3d.Normalize(point2 - point1);
			Vector3d zAxis = Vector3d.Normalize(zTargetPoint - point1);
			Vector3d yAxis = Vector3d.Cross(zAxis, xAxis);

			curveLcsCache[lineId] = new CS(xAxis, yAxis);

			return zAxis;
		}

		public void GetLocalCoordinateSystemAxesForLine(int lineId, out Vector3d xAxis, out Vector3d yAxis, out Vector3d zAxis)
		{
			CS lcs = getOrCalculateLcsForCurve(lineId);
			xAxis = lcs.XAxis;
			yAxis = lcs.YAxis;
			zAxis = lcs.ZAxis;
		}

		public void GetVerticesOfLine(int lineId, out Vertex v1, out Vertex v2)
		{
			int vertex1Id, vertex2Id;
			if (!tryGetLineVertices(lineId, out vertex1Id, out vertex2Id))
				throw new KeyNotFoundException($"Line with id {lineId} was not found");

			if (!tryGetVertex(vertex1Id, out v1))
				throw new KeyNotFoundException($"Vertex with id {vertex1Id} was not found");
			if (!tryGetVertex(vertex2Id, out v2))
				throw new KeyNotFoundException($"Vertex with id {vertex2Id} was not found");
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

		private CS getOrCalculateLcsForCurve(int curveId)
		{
			CS curveLcs;
			if (curveLcsCache.TryGetValue(curveId, out curveLcs))
				return curveLcs;
			curveLcs = calculateLcsForCurve(curveId);
			curveLcsCache[curveId] = curveLcs;
			return curveLcs;

		}

		private CS calculateLcsForCurve(int curveId)
		{
			int vertex1Id, vertex2Id;
			if (!tryGetLineVertices(curveId, out vertex1Id, out vertex2Id))
				throw new KeyNotFoundException($"Line with id {curveId} was not found");

			Vertex v1, v2;
			if (!tryGetVertex(vertex1Id, out v1))
				throw new KeyNotFoundException($"Vertex with id {vertex1Id} was not found");
			if (!tryGetVertex(vertex2Id, out v2))
				throw new KeyNotFoundException($"Vertex with id {vertex2Id} was not found");

			Vector3d point1 = new Vector3d(v1.X, v1.Y, v1.Z);
			Vector3d point2 = new Vector3d(v2.X, v2.Y, v2.Z);

			Vector3d xAxis = Vector3d.Normalize(point2 - point1);
			Vector3d globalZAxis = Vector3d.UnitZ; // WARNING: zAxis should not be parallel with xAxis
			Vector3d yAxis = Vector3d.Normalize(Vector3d.Cross(globalZAxis, xAxis));

			return new CS(xAxis, yAxis);
		}

		#endregion

		private struct CS
		{
			public CS(Vector3d normalizedXAxis, Vector3d normalizedYAxis)
			{
				XAxis = normalizedXAxis;
				YAxis = normalizedYAxis;
			}
			public Vector3d XAxis { get; }
			public Vector3d YAxis { get; }
			public Vector3d ZAxis => Vector3d.Cross(XAxis, YAxis);
		}
	}
}
