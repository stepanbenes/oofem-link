using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;
using OofemLink.Common.OofemNames;
using OofemLink.Data.MeshEntities;
using static System.FormattableString;

namespace OofemLink.Services.Export.OOFEM
{
	abstract class InputRecord
	{ }

	interface IIndexableRecord
	{
		int InputIndex { get; set; }
	}

	abstract class IdentityRecord : InputRecord
	{
		public IdentityRecord(int id)
		{
			Id = id;
		}
		public int Id { get; }
		public abstract string Name { get; }
	}

	abstract class NamedRecord : IdentityRecord
	{
		public NamedRecord(string name, int id)
			: base(id)
		{
			Name = name;
		}
		public override string Name { get; }
	}

	class OutputFileRecord : InputRecord
	{
		public OutputFileRecord(string outputFileFullPath)
		{
			OutputFileFullPath = outputFileFullPath;
		}
		public string OutputFileFullPath { get; }
		public override string ToString() => OutputFileFullPath;
	}

	class DescriptionRecord : InputRecord
	{
		public DescriptionRecord(string description)
		{
			Description = description;
		}
		public string Description { get; }
		public override string ToString() => Description;
	}

	class EngineeringModelRecord : InputRecord
	{
		public EngineeringModelRecord(string engineeringModelName, int numberOfTimeSteps, IReadOnlyList<ExportModuleRecord> exportModules)
		{
			EngineeringModelName = engineeringModelName;
			NumberOfTimeSteps = numberOfTimeSteps;
			ExportModules = exportModules;
		}
		public string EngineeringModelName { get; }
		public int NumberOfTimeSteps { get; }
		public IReadOnlyList<ExportModuleRecord> ExportModules { get; }
		public override string ToString() => $"{EngineeringModelName} {Keyword.nsteps} {NumberOfTimeSteps} {Keyword.nmodules} {ExportModules.Count} profileopt 1";
	}

	abstract class ExportModuleRecord : InputRecord
	{ }

	class VtkXmlExportModuleRecord : ExportModuleRecord
	{
		public VtkXmlExportModuleRecord(IReadOnlyList<int> primVars, IReadOnlyList<int> vars, IReadOnlyList<int> cellVars, IReadOnlyList<SetRecord> regionSets)
		{
			PrimVars = primVars;
			Vars = vars;
			CellVars = cellVars;
			RegionSets = regionSets;
		}

		public IReadOnlyList<int> PrimVars { get; }
		public IReadOnlyList<int> Vars { get; }
		public IReadOnlyList<int> CellVars { get; }
		public IReadOnlyList<SetRecord> RegionSets { get; }

		public override string ToString()
		{
			StringBuilder text = new StringBuilder();
			text.Append("vtkxml tstep_all domain_all");
			text.Append($" primvars {PrimVars.Count}");
			if (PrimVars.Count > 0)
				text.Append(" " + string.Join(" ", PrimVars));
			text.Append($" vars {Vars.Count}");
			if (Vars.Count > 0)
				text.Append(" " + string.Join(" ", Vars));
			text.Append($" cellvars {CellVars.Count}");
			if (CellVars.Count > 0)
				text.Append(" " + string.Join(" ", CellVars));
			if (RegionSets.Count > 0)
			{
				text.Append($" regionsets {RegionSets.Count}");
				text.Append(" " + string.Join(" ", RegionSets.Select(s => ((IIndexableRecord)s).InputIndex.ToString())));
			}
			return text.ToString();
		}
	}

	class DomainRecord : InputRecord
	{
		public DomainRecord(string domainType)
		{
			DomainType = domainType;
		}
		public string DomainType { get; }
		public override string ToString() => $"{Keyword.domain} {DomainType}";
	}

	class OutputManagerRecord : InputRecord
	{
		public override string ToString() => "OutputManager tstep_all dofman_all element_all"; // TODO: avoid hard-coded string
	}

	abstract class DofManagerRecord : IdentityRecord
	{
		public DofManagerRecord(int id, double x, double y, double z)
			: base(id)
		{
			X = x;
			Y = y;
			Z = z;
		}
		public double X { get; }
		public double Y { get; }
		public double Z { get; }
	}

	class NodeRecord : DofManagerRecord
	{
		public NodeRecord(int id, double x, double y, double z)
			: base(id, x, y, z)
		{ }
		public override string Name => DofManagerNames.node;
		public override string ToString() => Invariant($"{Name} {Id} {Keyword.coords} 3 {X} {Y} {Z}");
	}

	class RigidArmNodeRecord : DofManagerRecord
	{
		public RigidArmNodeRecord(int id, double x, double y, double z, int masterId, string parameters)
			: base(id, x, y, z)
		{
			MasterId = masterId;
			Parameters = parameters;
		}
		public override string Name => DofManagerNames.RigidArmNode;
		public int MasterId { get; }
		public string Parameters { get; }
		public override string ToString() => Invariant($"{Name} {Id} {Keyword.coords} 3 {X} {Y} {Z} {Keyword.master} {MasterId} {Parameters}");
	}

	class ElementRecord : NamedRecord
	{
		public ElementRecord(string name, int id, CellType type, IReadOnlyList<int> nodeIds, string parameters = null)
			: base(name, id)
		{
			Type = type;
			NodeIds = nodeIds;
			Parameters = parameters;
		}
		public CellType Type { get; }
		public IReadOnlyList<int> NodeIds { get; private set; }
		public string Parameters { get; internal set; }

		public override string ToString()
		{
			string text = $"{Name} {Id} {Keyword.nodes} {NodeIds.Count} {string.Join(" ", NodeIds)}";
			if (string.IsNullOrEmpty(Parameters))
				return text;
			return text + " " + Parameters;
		}

		public void ReplaceNode(int oldNodeId, int newNodeId)
			=> NodeIds = NodeIds.Select(id => id == oldNodeId ? newNodeId : id).ToArray();

		public ElementRecord WithNodes(int elementId, params int[] newNodeIds)
			=> new ElementRecord(Name, elementId, Type, newNodeIds, Parameters);
	}

	class CrossSectionRecord : NamedRecord, IIndexableRecord
	{
		public CrossSectionRecord(string name, int id, string parameters, MaterialRecord material, SetRecord set)
			: base(name, id)
		{
			Parameters = parameters;
			Material = material;
			Set = set;
		}
		int IIndexableRecord.InputIndex { get; set; }
		public string Parameters { get; }
		public MaterialRecord Material { get; }
		public SetRecord Set { get; internal set; }

		public override string ToString() => $"{Name} {((IIndexableRecord)this).InputIndex} {Parameters} {Keyword.material} {((IIndexableRecord)Material).InputIndex} {Keyword.set} {((IIndexableRecord)Set).InputIndex}";
	}

	class MaterialRecord : NamedRecord, IIndexableRecord
	{
		public MaterialRecord(string name, int id, string parameters)
			: base(name, id)
		{
			Parameters = parameters;
		}
		int IIndexableRecord.InputIndex { get; set; }
		public string Parameters { get; }
		public override string ToString() => $"{Name} {((IIndexableRecord)this).InputIndex} {Parameters}";
	}

	class BoundaryConditionRecord : NamedRecord, IIndexableRecord
	{
		public BoundaryConditionRecord(string name, int id, string parameters, TimeFunctionRecord timeFunction, SetRecord set)
			: base(name, id)
		{
			Parameters = parameters;
			TimeFunction = timeFunction;
			Set = set;
		}
		int IIndexableRecord.InputIndex { get; set; }
		public string Parameters { get; }
		public TimeFunctionRecord TimeFunction { get; }
		public SetRecord Set { get; }
		public override string ToString() => $"{Name} {((IIndexableRecord)this).InputIndex} {Parameters} {Keyword.loadTimeFunction} {((IIndexableRecord)TimeFunction).InputIndex} {Keyword.set} {((IIndexableRecord)Set).InputIndex}";
	}

	class TimeFunctionRecord : NamedRecord, IIndexableRecord
	{
		readonly double? time, value;
		readonly IReadOnlyList<KeyValuePair<double, double>> timeValuePairs;

		public TimeFunctionRecord(string name, int id, IReadOnlyList<KeyValuePair<double, double>> timeValuePairs)
			: base(name, id)
		{
			this.timeValuePairs = timeValuePairs;
		}

		public TimeFunctionRecord(string name, int id, double time, double value)
			: base(name, id)
		{
			this.time = time;
			this.value = value;
		}

		public TimeFunctionRecord(string name, int id, double value)
			: base(name, id)
		{
			this.value = value;
		}

		int IIndexableRecord.InputIndex { get; set; }

		public override string ToString()
		{
			var text = new StringBuilder();
			text.Append($"{Name} {((IIndexableRecord)this).InputIndex}");
			if (time.HasValue)
				text.Append(Invariant($" t {time.Value}"));
			if (value.HasValue)
				text.Append(Invariant($" f(t) {value.Value}"));
			if (timeValuePairs != null)
			{
				string times = string.Join(" ", timeValuePairs.Select(pair => pair.Key.ToString(CultureInfo.InvariantCulture)));
				string values = string.Join(" ", timeValuePairs.Select(pair => pair.Value.ToString(CultureInfo.InvariantCulture)));
				text.Append($" {Keyword.nPoints} {timeValuePairs.Count} t {timeValuePairs.Count} {times} f(t) {timeValuePairs.Count} {values}");
			}
			return text.ToString();
		}
	}

	class SetRecord : InputRecord, IIndexableRecord
	{
		public SetRecord(MeshEntitySet set)
		{
			Set = set;
		}
		int IIndexableRecord.InputIndex { get; set; }
		public MeshEntitySet Set { get; internal set; }
		public override string ToString()
		{
			var text = new StringBuilder();
			text.Append($"{Keyword.set} {((IIndexableRecord)this).InputIndex}");
			if (Set.Nodes.Count > 0)
			{
				text.Append($" {Keyword.nodes} {Set.Nodes.Count} {string.Join(" ", Set.Nodes)}");
			}
			if (Set.Elements.Count > 0)
			{
				text.Append($" {Keyword.elements} {Set.Elements.Count} {string.Join(" ", Set.Elements)}");
			}
			if (Set.ElementEdges.Count > 0)
			{
				text.Append($" {Keyword.elementedges} {Set.ElementEdges.Count * 2} {string.Join(" ", Set.ElementEdges.Select(edge => $"{edge.ElementId} {edge.EdgeRank}"))}");
			}
			if (Set.ElementSurfaces.Count > 0)
			{
				text.Append($" {Keyword.elementboundaries} {Set.ElementSurfaces.Count * 2} {string.Join(" ", Set.ElementSurfaces.Select(surface => $"{surface.ElementId} {surface.SurfaceRank}"))}");
			}
			return text.ToString();
		}
	}
}
