using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;
using OofemLink.Data.DbEntities;

namespace OofemLink.Services.Import
{
	public class ModelMeshMapper
	{
		readonly Model model;
		readonly Mesh mesh;

		public ModelMeshMapper(Model model, Mesh mesh)
		{
			this.model = model;
			this.mesh = mesh;
		}

		public void MapCurveTo1DElements(int? curveId, int macroId, int startElementId, int endElementId)
		{
			var macro = getMacroWithId(macroId);

			var macroCurveMapping = curveId.HasValue ? macro.MacroCurves.SingleOrDefault(c => c.CurveId == curveId.Value) : macro.MacroCurves.SingleOrDefault();
			if (macroCurveMapping == null)
				throw new InvalidOperationException($"Curve with id {curveId} is not attached to macro with id {macroId}.");
			for (int elementId = startElementId; elementId <= endElementId; elementId++)
			{
				var edge = new CurveElement { Model = model, Mesh = mesh, CurveId = macroCurveMapping.CurveId, ElementId = elementId, Rank = 0 /*refers to single edge of 1D element*/ };
				mesh.CurveElements.Add(edge);
			}
		}

		public void MapSurfaceTo2DElements(int? surfaceId, int macroId, int startElementId, int endElementId)
		{
			var macro = getMacroWithId(macroId);

			var macroSurfaceMapping = surfaceId.HasValue ? macro.MacroSurfaces.SingleOrDefault(s => s.SurfaceId == surfaceId.Value) : macro.MacroSurfaces.SingleOrDefault();
			if (macroSurfaceMapping == null)
				throw new InvalidOperationException($"Macro with id {macro.Id} does not contain link to surface.");
			for (int elementId = startElementId; elementId <= endElementId; elementId++)
			{
				var face = new SurfaceElement { Model = model, Mesh = mesh, SurfaceId = macroSurfaceMapping.SurfaceId, ElementId = elementId, Rank = 0 /*refers to single surface of 2D element*/ };
				mesh.SurfaceElements.Add(face);
			}
		}

		public void MapVolumeTo3DElements(int? volumeId, int macroId, int startElementId, int endElementId)
		{
			var macro = getMacroWithId(macroId);

			var macroVolumeMapping = volumeId.HasValue ? macro.MacroVolumes.SingleOrDefault(v => v.VolumeId == volumeId.Value) : macro.MacroVolumes.SingleOrDefault();
			if (macroVolumeMapping == null)
				throw new InvalidOperationException($"Macro with id {macro.Id} does not contain link to volume.");
			for (int elementId = startElementId; elementId <= endElementId; elementId++)
			{
				var volumeElementMapping = new VolumeElement { Model = model, Mesh = mesh, VolumeId = macroVolumeMapping.VolumeId, ElementId = elementId };
				mesh.VolumeElements.Add(volumeElementMapping);
			}
		}

		public void MapVertexToNode(int vertexId)
		{
			var vertexNode = new VertexNode
			{
				VertexId = vertexId,
				NodeId = vertexId, // node id is same as vertex id!
				Model = model,
				Mesh = mesh
			};
			mesh.VertexNodes.Add(vertexNode);
		}

		public void MapCurveTo2dOr3dElementEdge(int curveId, int firstNodeId, int secondNodeId)
		{
			foreach (var element in mesh.Elements)
			{
				if (element.Type != CellType.TriangleLinear && element.Type != CellType.QuadLinear)
					continue;
				var firstElementNode = element.ElementNodes.FirstOrDefault(en => en.NodeId == firstNodeId);
				if (firstElementNode == null)
					continue;
				var secondElementNode = element.ElementNodes.FirstOrDefault(en => en.NodeId == secondNodeId);
				if (secondElementNode == null)
					continue;
				short edgeRank;
				if (element.Type == CellType.TriangleLinear)
					edgeRank = getEdgeRankFromTriangleElementNodeRanks(firstElementNode.Rank, secondElementNode.Rank);
				else
					edgeRank = getEdgeRankFromQuadElementNodeRanks(firstElementNode.Rank, secondElementNode.Rank);
				var curveElement = new CurveElement { Model = model, Mesh = mesh, CurveId = curveId, ElementId = element.Id, Rank = edgeRank };
				mesh.CurveElements.Add(curveElement);
			}
		}

		public void MapCurveToNode(int curveId, int nodeId)
		{
			var curveNode = new CurveNode { Model = model, Mesh = mesh, CurveId = curveId, NodeId = nodeId };
			mesh.CurveNodes.Add(curveNode);
		}

		#region Private methods

		private Macro getMacroWithId(int macroId)
		{
			var macro = model.Macros.SingleOrDefault(m => m.Id == macroId);
			if (macro == null)
				throw new KeyNotFoundException($"Macro with id {macroId} was not found");
			return macro;
		}

		private short getEdgeRankFromTriangleElementNodeRanks(int node1Rank, int node2Rank)
		{
			if ((node1Rank == 1 && node2Rank == 2) || (node1Rank == 2 && node2Rank == 1))
				return 1;
			if ((node1Rank == 2 && node2Rank == 3) || (node1Rank == 3 && node2Rank == 2))
				return 2;
			if ((node1Rank == 3 && node2Rank == 1) || (node1Rank == 1 && node2Rank == 3))
				return 3;
			throw new InvalidDataException();
		}

		private short getEdgeRankFromQuadElementNodeRanks(int node1Rank, int node2Rank)
		{
			if ((node1Rank == 1 && node2Rank == 2) || (node1Rank == 2 && node2Rank == 1))
				return 1;
			if ((node1Rank == 2 && node2Rank == 3) || (node1Rank == 3 && node2Rank == 2))
				return 2;
			if ((node1Rank == 3 && node2Rank == 4) || (node1Rank == 4 && node2Rank == 3))
				return 3;
			if ((node1Rank == 4 && node2Rank == 1) || (node1Rank == 1 && node2Rank == 4))
				return 4;
			throw new InvalidDataException();
		}

		#endregion
	}
}
