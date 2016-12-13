﻿using System;
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

			var simulation = new ProFileParser(location, taskName, loggerFactory).Parse(loadCasesToIgnore: new[] { 9998, 9999 });

			ModelDimensions dimensions;
			var model = new GeoFileParser(location, taskName, loggerFactory).Parse(out dimensions);
			var mesh = importMesh(dimensions);
			model.Meshes.Add(mesh);

			linkModelAndMeshTogether(model, mesh);

			importAttributesToModel(model, simulation.TimeSteps);

			simulation.DimensionFlags = dimensions;
			simulation.Model = model;

			simulation.State = SimulationState.ModelReady;

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

		private void importAttributesToModel(Model model, IEnumerable<TimeStep> timeSteps)
		{
			// IST file parsing
			var istParser = new IstFileParser(location, taskName, loggerFactory);
			var cs_mat_bc_attributes = istParser.Parse().ToList();
			int attributesTotal = addAttributesToModel(model, startAttributeId: 1, attributes: cs_mat_bc_attributes);

			// Ixxxx files parsing
			int timeFunctionId = 1;
			foreach (var timeStep in timeSteps)
			{
				var timeFunction = new TimeFunction
				{
					Id = timeFunctionId++,
					Type = TimeFunctionType.PiecewiseLinear,
					Values = { new TimeFunctionValue { TimeStep = timeStep, Value = 1.0 } }
				};
				model.TimeFunctions.Add(timeFunction);
				int loadCaseNumber = timeStep.Number;
				var ixxxxFileParser = new IxxxxFileParser(loadCaseNumber, location, taskName, loggerFactory);
				var lc_attributes = ixxxxFileParser.Parse().ToList();
				foreach (var loadCase in lc_attributes)
				{
					loadCase.TimeFunction = timeFunction;
				}
				attributesTotal += addAttributesToModel(model, startAttributeId: attributesTotal + 1, attributes: lc_attributes);
			}
		}

		private int addAttributesToModel(Model model, int startAttributeId, IEnumerable<ModelAttribute> attributes)
		{
			int id = startAttributeId;
			foreach (var attribute in attributes)
			{
				attribute.Id = id;
				model.Attributes.Add(attribute);
				id += 1;
				int addedCount = addAttributesToModel(model, id, attribute.ChildAttributes.Select(c => c.ChildAttribute));
				id += addedCount;
			}
			return id - startAttributeId; // returns count of attributes that were added
		}

		#endregion
	}
}
