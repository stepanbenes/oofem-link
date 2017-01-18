using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Data.Entities;

namespace OofemLink.Services.Import
{
	public class AttributeMapper
	{
		readonly Model model;

		public AttributeMapper(Model model)
		{
			this.model = model;
		}

		public void MapToMacro(ModelAttribute attribute, int macroId)
		{
			foreach (int curveId in from macro in model.Macros
									from macroCurve in macro.MacroCurves
									where macroCurve.MacroId == macroId
									select macroCurve.CurveId)
			{
				MapToCurve(attribute, curveId, macroId);
			}

			foreach (int surfaceId in from macro in model.Macros
									  from macroSurface in macro.MacroSurfaces
									  where macroSurface.MacroId == macroId
									  select macroSurface.SurfaceId)
			{
				MapToSurface(attribute, surfaceId, macroId);
			}

			foreach (int volumeId in from macro in model.Macros
									 from macroVolume in macro.MacroVolumes
									 where macroVolume.MacroId == macroId
									 select macroVolume.VolumeId)
			{
				MapToVolume(attribute, volumeId, macroId);
			}
		}

		public void MapToAllMacros(ModelAttribute attribute)
		{
			foreach (var macro in model.Macros)
			{
				MapToMacro(attribute, macro.Id);
			}
		}

		public void MapToVertex(ModelAttribute attribute, int vertexId)
		{
			attribute.VertexAttributes.Add(new VertexAttribute { VertexId = vertexId });
		}

		public void MapToCurve(ModelAttribute attribute, int curveId, int macroId)
		{
			attribute.CurveAttributes.Add(new CurveAttribute { MacroId = macroId, CurveId = curveId });
		}

		public void MapToCurve(ModelAttribute attribute, int curveId)
		{
			// look in macro boundary curves
			foreach (int macroId in from macro in model.Macros
									from macroCurve in macro.MacroCurves
									where macroCurve.CurveId == curveId
									select macroCurve.MacroId)
			{
				MapToCurve(attribute, curveId, macroId);
			}

			// look in macro opening curves
			foreach (int macroId in from macro in model.Macros
									from macroCurve in macro.MacroOpeningCurves
									where macroCurve.OpeningCurveId == curveId
									select macroCurve.MacroId)
			{
				MapToCurve(attribute, curveId, macroId);
			}

			// look in macro internal curves
			foreach (int macroId in from macro in model.Macros
									from macroCurve in macro.MacroInternalCurves
									where macroCurve.InternalCurveId == curveId
									select macroCurve.MacroId)
			{
				MapToCurve(attribute, curveId, macroId);
			}

			// look in macro surfaces
			foreach (int macroId in from macro in model.Macros
									from macroSurface in macro.MacroSurfaces
									join surface in model.Surfaces on macroSurface.SurfaceId equals surface.Id
									from surfaceCurve in surface.SurfaceCurves
									where surfaceCurve.CurveId == curveId
									select macro.Id)
			{
				MapToCurve(attribute, curveId, macroId);
			}
		}

		public void MapToSurface(ModelAttribute attribute, int surfaceId, int macroId)
		{
			attribute.SurfaceAttributes.Add(new SurfaceAttribute { MacroId = macroId, SurfaceId = surfaceId });
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

		#endregion
	}
}
