using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;
using OofemLink.Common.MathPhys;
using OofemLink.Common.OofemNames;
using OofemLink.Data.DbEntities;

namespace OofemLink.Services.Import
{
	public class AttributeMapper
	{
		readonly Model model;

		public AttributeMapper(Model model)
		{
			this.model = model;
		}

		public void MapToBeamMacro(ModelAttribute attribute, int macroId, double? relativeStart = null, double? relativeEnd = null)
		{
			foreach (int curveId in from macro in model.Macros
									where macro.Id == macroId
									from macroCurve in macro.MacroCurves
									select macroCurve.CurveId)
			{
				MapToCurve(attribute, curveId, macroId, relativeStart, relativeEnd);
			}
		}

		public void MapToSurfaceMacro(ModelAttribute attribute, int macroId)
		{
			foreach (int surfaceId in from macro in model.Macros
									  where macro.Id == macroId
									  from macroSurface in macro.MacroSurfaces
									  select macroSurface.SurfaceId)
			{
				MapToSurface(attribute, surfaceId, macroId);
			}
		}

		public void MapToVolumeMacro(ModelAttribute attribute, int macroId)
		{
			foreach (int volumeId in from macro in model.Macros
									 where macro.Id == macroId
									 from macroVolume in macro.MacroVolumes
									 select macroVolume.VolumeId)
			{
				MapToVolume(attribute, volumeId, macroId);
			}
		}

		public void MapToAllMacros(ModelAttribute attribute)
		{
			foreach (var macro in model.Macros)
			{
				MapToBeamMacro(attribute, macro.Id);
				MapToSurfaceMacro(attribute, macro.Id);
				MapToVolumeMacro(attribute, macro.Id);
			}
		}

		public void MapToVertex(ModelAttribute attribute, int vertexId)
		{
			attribute.VertexAttributes.Add(new VertexAttribute { VertexId = vertexId });
		}

		public void MapToCurve(ModelAttribute attribute, int curveId, int macroId, double? relativeStart = null, double? relativeEnd = null)
		{
			attribute.CurveAttributes.Add(new CurveAttribute { MacroId = macroId, CurveId = curveId, RelativeStart = relativeStart, RelativeEnd = relativeEnd });
		}

		public void MapToCurve(ModelAttribute attribute, int curveId, double? relativeStart = null, double? relativeEnd = null)
		{
			// look in macro boundary curves
			foreach (int macroId in from macro in model.Macros
									from macroCurve in macro.MacroCurves
									where macroCurve.CurveId == curveId
									select macroCurve.MacroId)
			{
				MapToCurve(attribute, curveId, macroId, relativeStart, relativeEnd);
			}

			// look in macro opening curves
			foreach (int macroId in from macro in model.Macros
									from macroCurve in macro.MacroOpeningCurves
									where macroCurve.OpeningCurveId == curveId
									select macroCurve.MacroId)
			{
				MapToCurve(attribute, curveId, macroId, relativeStart, relativeEnd);
			}

			// look in macro internal curves
			foreach (int macroId in from macro in model.Macros
									from macroCurve in macro.MacroInternalCurves
									where macroCurve.InternalCurveId == curveId
									select macroCurve.MacroId)
			{
				MapToCurve(attribute, curveId, macroId, relativeStart, relativeEnd);
			}

			// look in macro surfaces
			foreach (int macroId in from macro in model.Macros
									from macroSurface in macro.MacroSurfaces
									join surface in model.Surfaces on macroSurface.SurfaceId equals surface.Id
									from surfaceCurve in surface.SurfaceCurves
									where surfaceCurve.CurveId == curveId
									select macro.Id)
			{
				MapToCurve(attribute, curveId, macroId, relativeStart, relativeEnd);
			}
		}

		public void MapToSurface(ModelAttribute attribute, int surfaceId, int macroId)
		{
			attribute.SurfaceAttributes.Add(new SurfaceAttribute { MacroId = macroId, SurfaceId = surfaceId });

			// TODO: avoid following hack; try to remove requirement to set normal vector, because attribute can be assigned to multiple (non-parallel) plates/walls

			// SimpleCS cross-section attribute is required to have directorx, directory and directorz parameters specifying normal vector of the plane on which it is applied
			if (attribute.Type == AttributeType.CrossSection && attribute.Name == CrossSectionNames.SimpleCS)
			{
				if (!attribute.Parameters.Contains("directorx")) // if parameter is not already set
				{
					Vector3d n = getSurfaceNormalVector(surfaceId);
					attribute.Parameters += FormattableString.Invariant($" directorx {n.X} directory {n.Y} directorz {n.Z}");
				}
			}
		}

		public void MapToSurface(ModelAttribute attribute, int surfaceId)
		{
			foreach (int macroId in from macro in model.Macros
									from macroSurface in macro.MacroSurfaces
									where macroSurface.SurfaceId == surfaceId
									select macroSurface.MacroId)
			{
				MapToSurface(attribute, surfaceId, macroId);
			}
		}

		public void MapToVolume(ModelAttribute attribute, int volumeId, int macroId)
		{
			attribute.VolumeAttributes.Add(new VolumeAttribute { MacroId = macroId, VolumeId = volumeId });
		}

		public void MapToVolume(ModelAttribute attribute, int volumeId)
		{
			foreach (int macroId in from macro in model.Macros
									from macroVolume in macro.MacroVolumes
									where macroVolume.VolumeId == volumeId
									select macroVolume.MacroId)
			{
				MapToVolume(attribute, volumeId, macroId);
			}
		}

		public void CreateParentChildRelation(ModelAttribute parentAttribute, ModelAttribute childAttribute)
		{
			var relation = new AttributeComposition { ParentAttribute = parentAttribute, ChildAttribute = childAttribute };
			parentAttribute.ChildAttributes.Add(relation);
			childAttribute.ParentAttributes.Add(relation);
		}

		public void GetStartCurveAndVertexOfBeamMacro(int macroId, out int curveId, out int vertexId)
		{
			getStartCurveAndVertexOfBeamMacro(macroId, true, out curveId, out vertexId);
		}

		public void GetEndCurveAndVertexOfBeamMacro(int macroId, out int curveId, out int vertexId)
		{
			getStartCurveAndVertexOfBeamMacro(macroId, false, out curveId, out vertexId);
		}

		public double GetLengthOfBeam(int? macroId, int? lineId)
		{
			var coordinateTransformService = new CoordinateTransformService(model); // TODO: needs refactoring

			if (!lineId.HasValue)
			{
				var macro = model.Macros.SingleOrDefault(m => m.Id == macroId);
				if (macro == null)
					throw new KeyNotFoundException($"Macro with id {macroId} was not found");
				lineId = macro.MacroCurves.Single().CurveId;
			}

			Vertex v1, v2;
			coordinateTransformService.GetVerticesOfLine(lineId.Value, out v1, out v2);

			Vector3d point1 = new Vector3d(v1.X, v1.Y, v1.Z);
			Vector3d point2 = new Vector3d(v2.X, v2.Y, v2.Z);

			return (point2 - point1).Length;
		}

		#region Private methods

		private void getStartCurveAndVertexOfBeamMacro(int macroId, bool isStartVertexRequested /*true to apply to start vertex, false to end vertex*/, out int curveId, out int vertexId)
		{
			var macro = model.Macros.SingleOrDefault(m => m.Id == macroId);
			if (macro == null)
				throw new KeyNotFoundException($"Macro with id {macroId} was not found");

			MacroCurve macroCurve;
			if (isStartVertexRequested)
				macroCurve = macro.MacroCurves.OrderBy(mc => mc.Rank).First();
			else
				macroCurve = macro.MacroCurves.OrderByDescending(mc => mc.Rank).First();

			// map the attribute to start vertex of curve
			var curve = model.Curves.Single(c => c.Id == macroCurve.CurveId);
			VertexCurve vertexCurve;
			if (isStartVertexRequested)
				vertexCurve = curve.CurveVertices.OrderBy(cv => cv.Rank).First();
			else
				vertexCurve = curve.CurveVertices.OrderByDescending(cv => cv.Rank).First();

			curveId = curve.Id;
			vertexId = vertexCurve.VertexId;
		}

		private Vector3d getSurfaceNormalVector(int surfaceId)
		{
			var surface = model.Surfaces.Single(c => c.Id == surfaceId);
			var surfaceVertices = from surfaceCurve in surface.SurfaceCurves
								  orderby surfaceCurve.Rank
								  join curve in model.Curves on surfaceCurve.CurveId equals curve.Id
								  join vertex in model.Vertices on surfaceCurve.IsInversed ? curve.CurveVertices.First().VertexId : curve.CurveVertices.Last().VertexId equals vertex.Id
								  select vertex;
			var vertexPositions = surfaceVertices.Select(v => new Vector3d(v.X, v.Y, v.Z)).ToArray();
			Debug.Assert(vertexPositions.Length == 4);
			Vector3d a = vertexPositions[3] - vertexPositions[0];
			Vector3d b = vertexPositions[1] - vertexPositions[0];
			return Vector3d.Normalize(Vector3d.Cross(a, b));
		}

		#endregion
	}
}
