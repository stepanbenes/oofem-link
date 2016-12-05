using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OofemLink.Common.Enumerations;
using OofemLink.Common.Extensions;
using OofemLink.Data;
using OofemLink.Data.Entities;

namespace OofemLink.Services.Import.ESA
{
	class EsaImportService : IImportService
	{
		#region Fields, constructor

		readonly string location, taskName;
		readonly ILoggerFactory loggerFactory;
		readonly ILogger logger;

		public EsaImportService(string location, string taskName, ILoggerFactory loggerFactory)
		{
			this.location = location;
			this.taskName = taskName;
			this.loggerFactory = loggerFactory;
			this.logger = loggerFactory.CreateLogger<EsaImportService>();
		}

		#endregion

		#region Public methods

		public Simulation ImportSimulation()
		{
			logger.LogInformation("Starting import...");

			var simulation = new ProFileParser(location, taskName, loggerFactory).Parse();

			ModelDimensions dimensions;
			var model = new GeoFileParser(location, taskName, loggerFactory).Parse(out dimensions);

			var mesh = importMesh(dimensions);

			model.Meshes.Add(mesh);

			linkModelAndMeshTogether(model, mesh);

			importAttributesToModel(model);

			simulation.DimensionFlags = dimensions;
			simulation.Models.Add(model);

			logger.LogInformation("Import finished.");
			return simulation;
		}

		#endregion

		#region Private methods

		private Mesh importMesh(ModelDimensions dimensions)
		{
			Mesh mesh = new Mesh();
			// NODES
			foreach (var node in new XyzFileParser(location, taskName, loggerFactory).Parse(dimensions))
			{
				mesh.Nodes.Add(node);
			}
			// 1D ELEMENTS
			foreach (var element in new E1dFileParser(location, taskName, loggerFactory).Parse(startElementId: 1))
			{
				mesh.Elements.Add(element);
			}
			// 2D ELEMENTS
			foreach (var element in new E2dFileParser(location, taskName, loggerFactory).Parse(startElementId: mesh.Elements.Count + 1))
			{
				mesh.Elements.Add(element);
			}
			return mesh;
		}

		/// <summary>
		/// link model and mesh entities together (using e.g. file MTO and LIN)
		/// </summary>
		private void linkModelAndMeshTogether(Model model, Mesh mesh)
		{
			string mtoFileFullPath = Path.Combine(location, $"{taskName}.MTO");

			// parse MTO file
			var parser = new MtoFileParser(location, taskName, loggerFactory);
			foreach (var macroElementsLink in parser.Parse())
			{
				var macro = model.Macros.SingleOrDefault(m => m.Id == macroElementsLink.MacroId);
				if (macro == null)
					throw new KeyNotFoundException($"Macro with id {macroElementsLink.MacroId} was not found");
				switch (macroElementsLink.Dimension)
				{
					case MtoFileParser.MacroElementsLink.ElementDimension.OneD:
						{
							var macroCurveMapping = macro.MacroCurves.SingleOrDefault(c => c.CurveId == macroElementsLink.GeometryEntityId.Value);
							if (macroCurveMapping == null)
								throw new InvalidOperationException($"Curve with id {macroElementsLink.GeometryEntityId.Value} is not attached to macro with id {macroElementsLink.MacroId}.");
							for (int elementId = macroElementsLink.StartElementId; elementId <= macroElementsLink.EndElementId; elementId++)
							{
								var edge = new CurveElement { Model = model, Mesh = mesh, CurveId = macroCurveMapping.CurveId, ElementId = elementId };
								mesh.CurveElements.Add(edge);
							}
						}
						break;
					case MtoFileParser.MacroElementsLink.ElementDimension.TwoD:
						{
							var macroSurfaceMapping = macroElementsLink.GeometryEntityId.HasValue ? macro.MacroSurfaces.SingleOrDefault(s => s.SurfaceId == macroElementsLink.GeometryEntityId.Value) : macro.MacroSurfaces.SingleOrDefault();
							if (macroSurfaceMapping == null)
								throw new InvalidOperationException($"Macro with id {macro.Id} does not contain link to surface.");
							for (int elementId = macroElementsLink.StartElementId; elementId <= macroElementsLink.EndElementId; elementId++)
							{
								var face = new SurfaceElement { Model = model, Mesh = mesh, SurfaceId = macroSurfaceMapping.SurfaceId, ElementId = elementId };
								mesh.SurfaceElements.Add(face);
							}
						}
						break;
					case MtoFileParser.MacroElementsLink.ElementDimension.ThreeD:
						{
							var macroVolumeMapping = macroElementsLink.GeometryEntityId.HasValue ? macro.MacroVolumes.SingleOrDefault(v => v.VolumeId == macroElementsLink.GeometryEntityId.Value) : macro.MacroVolumes.SingleOrDefault();
							if (macroVolumeMapping == null)
								throw new InvalidOperationException($"Macro with id {macro.Id} does not contain link to volume.");
							for (int elementId = macroElementsLink.StartElementId; elementId <= macroElementsLink.EndElementId; elementId++)
							{
								var volumeElementMapping = new VolumeElement { Model = model, Mesh = mesh, VolumeId = macroVolumeMapping.VolumeId, ElementId = elementId };
								mesh.VolumeElements.Add(volumeElementMapping);
							}
						}
						break;
				}
			}
		}

		private void importAttributesToModel(Model model)
		{
			var globalTimeFunction = new TimeFunction
			{
				Id = 1, /**/
				Model = model,
				Type = TimeFunctionType.Constant
			};

			var istParser = new IstFileParser(location, taskName, loggerFactory);
			int count = addAttributesToModel(model, globalTimeFunction, startAttributeId: 1, attributes: istParser.Parse());

			// TODO: add Ix files parsing
		}

		private int addAttributesToModel(Model model, TimeFunction globalTimeFunction, int startAttributeId, IEnumerable<ModelAttribute> attributes)
		{
			int id = startAttributeId;
			foreach (var attribute in attributes)
			{
				attribute.Id = id;

				foreach (var vertexAttribute in attribute.VertexAttributes)
					vertexAttribute.TimeFunction = globalTimeFunction;
				foreach (var curveAttribute in attribute.CurveAttributes)
					curveAttribute.TimeFunction = globalTimeFunction;
				foreach (var surfaceAttribute in attribute.SurfaceAttributes)
					surfaceAttribute.TimeFunction = globalTimeFunction;
				foreach (var volumeAttribute in attribute.VolumeAttributes)
					volumeAttribute.TimeFunction = globalTimeFunction;

				model.Attributes.Add(attribute);
				id += 1;
				int addedCount = addAttributesToModel(model, globalTimeFunction, id, attribute.ChildAttributes.Select(c => c.ChildAttribute));
				id += addedCount;
			}
			return id - startAttributeId; // returns count of attributes that were added
		}

		#endregion
	}
}
