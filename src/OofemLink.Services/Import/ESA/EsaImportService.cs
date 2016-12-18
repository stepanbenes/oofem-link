using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OofemLink.Common.Enumerations;
using OofemLink.Common.OofemNames;
using OofemLink.Data.Entities;

namespace OofemLink.Services.Import.ESA
{
	class EsaImportService : IImportService
	{
		#region Fields, constructor

		readonly string location, taskName;
		readonly ILoggerFactory loggerFactory;
		readonly ILogger logger;
		readonly bool importModelOnly;

		public EsaImportService(string location, string taskName, ILoggerFactory loggerFactory, bool importModelOnly = false)
		{
			this.location = location;
			this.taskName = taskName;
			this.importModelOnly = importModelOnly;
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

			if (!importModelOnly)
			{
				importAttributesToModel(model, simulation.TimeSteps); // import model attributes
			}

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

			// parse MTO file (Macro - Elements links)
			var mtoFileParser = new MtoFileParser(location, taskName, loggerFactory);
			foreach (var macroElementsLink in mtoFileParser.Parse())
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
								var edge = new CurveElement { Model = model, Mesh = mesh, CurveId = macroCurveMapping.CurveId, ElementId = elementId, Rank = 1 /*refers to single edge of 1D element*/ };
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
								var face = new SurfaceElement { Model = model, Mesh = mesh, SurfaceId = macroSurfaceMapping.SurfaceId, ElementId = elementId, Rank = 1 /*refers to single surface of 2D element*/ };
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

			// parse NUMESA file (Vertex - Node links)
			var numesaFileParser = new NumesaFileParser(location, taskName, loggerFactory);
			foreach (VertexNode vertexNode in numesaFileParser.ParseVertexNodes())
			{
				vertexNode.Model = model;
				vertexNode.Mesh = mesh;
				mesh.VertexNodes.Add(vertexNode);
			}
		}

		private void importAttributesToModel(Model model, IEnumerable<TimeStep> timeSteps)
		{
			// IST file parsing
			var istParser = new IstFileParser(location, taskName, loggerFactory);
			var cs_mat_bc_attributes = istParser.Parse().ToList();
			int attributesTotal = addAttributesToModel(cs_mat_bc_attributes, model, startAttributeId: 1);

			// Ixxxx files parsing
			int timeFunctionId = 1;
			foreach (var timeStep in timeSteps)
			{
				var timeFunction = new PiecewiseLinFunction
				{
					Id = timeFunctionId++,
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
				attributesTotal += addAttributesToModel(lc_attributes, model, startAttributeId: attributesTotal + 1);
			}
		}

		private int addAttributesToModel(IEnumerable<ModelAttribute> attributes, Model model, int startAttributeId)
		{
			int id = startAttributeId;
			foreach (var attribute in attributes)
			{
				attribute.Id = id;
				fillMissingMacroGeometryMappingsForAttribute(attribute, model);
				model.Attributes.Add(attribute);
				id += 1;
				int addedCount = addAttributesToModel(attribute.ChildAttributes.Select(c => c.ChildAttribute), model, id);
				id += addedCount;
			}
			return id - startAttributeId; // returns count of attributes that were added
		}

		private void fillMissingMacroGeometryMappingsForAttribute(ModelAttribute attribute, Model model)
		{
			// check if macro applies to whole model (e.g. DeadWeight attribute)
			if (attribute.AppliesToAllMacros)
			{
				attribute.MacroAttributes = model.Macros.Select(m => new MacroAttribute { MacroId = m.Id }).ToList(); // create dummy macro-attributes for all macros in model
			}

			// temporary Macro-Attributes that points to macros only (not to geometry entities) - they need to be redirected to corresponding geometry entities
			foreach (var macroAttribute in attribute.MacroAttributes)
			{
				Debug.Assert(macroAttribute.MacroId != 0);
				foreach (var ca in from macro in model.Macros
								   from macroCurve in macro.MacroCurves
								   where macroCurve.MacroId == macroAttribute.MacroId
								   select new CurveAttribute { MacroId = macroCurve.MacroId, CurveId = macroCurve.CurveId })
				{
					attribute.CurveAttributes.Add(ca);
				}
				foreach (var sa in from macro in model.Macros
								   from macroSurface in macro.MacroSurfaces
								   where macroSurface.MacroId == macroAttribute.MacroId
								   select new SurfaceAttribute { MacroId = macroSurface.MacroId, SurfaceId = macroSurface.SurfaceId })
				{
					attribute.SurfaceAttributes.Add(sa);
				}
				foreach (var va in from macro in model.Macros
								   from macroVolume in macro.MacroVolumes
								   where macroVolume.MacroId == macroAttribute.MacroId
								   select new VolumeAttribute { MacroId = macroVolume.MacroId, VolumeId = macroVolume.VolumeId })
				{
					attribute.VolumeAttributes.Add(va);
				}
			}

			attribute.MacroAttributes.Clear();

			// Curve-Attributes with missing MacroId or CurveId
			foreach (var curveAttribute in attribute.CurveAttributes.ToList())
			{
				Debug.Assert(curveAttribute.CurveId != 0);
				if (curveAttribute.MacroId == 0)
				{
					attribute.CurveAttributes.Remove(curveAttribute);
					foreach (var ca in from macro in model.Macros
									   from macroCurve in macro.MacroCurves
									   where macroCurve.CurveId == curveAttribute.CurveId
									   select new CurveAttribute { MacroId = macroCurve.MacroId, CurveId = macroCurve.CurveId })
					{
						attribute.CurveAttributes.Add(ca);
					}
				}
			}

			// Surface-Attributes with missing MacroId or SurfaceId
			foreach (var surfaceAttribute in attribute.SurfaceAttributes.ToList())
			{
				Debug.Assert(surfaceAttribute.SurfaceId != 0);
				if (surfaceAttribute.MacroId == 0)
				{
					attribute.SurfaceAttributes.Remove(surfaceAttribute);
					foreach (var sa in from macro in model.Macros
									   from macroSurface in macro.MacroSurfaces
									   where macroSurface.SurfaceId == surfaceAttribute.SurfaceId
									   select new SurfaceAttribute { MacroId = macroSurface.MacroId, SurfaceId = macroSurface.SurfaceId })
					{
						attribute.SurfaceAttributes.Add(sa);
					}
				}
			}

			// Volume-Attributes with missing MacroId or VolumeId
			foreach (var volumeAttribute in attribute.VolumeAttributes.ToList())
			{
				Debug.Assert(volumeAttribute.VolumeId != 0);
				if (volumeAttribute.MacroId == 0)
				{
					attribute.VolumeAttributes.Remove(volumeAttribute);
					foreach (var va in from macro in model.Macros
									   from macroVolume in macro.MacroVolumes
									   where macroVolume.VolumeId == volumeAttribute.VolumeId
									   select new VolumeAttribute { MacroId = macroVolume.MacroId, VolumeId = macroVolume.VolumeId })
					{
						attribute.VolumeAttributes.Add(va);
					}
				}
			}
		}

		#endregion
	}
}
