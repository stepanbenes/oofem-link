using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OofemLink.Common.OofemNames;
using static System.FormattableString;

namespace OofemLink.Services.Export.OOFEM
{
	abstract class InputRecord
	{ }

	abstract class HeaderRecord : InputRecord
	{ }

	class OutputFileRecord : HeaderRecord
	{
		public OutputFileRecord(string outputFileFullPath)
		{
			OutputFileFullPath = outputFileFullPath;
		}
		public string OutputFileFullPath { get; }
		public override string ToString() => OutputFileFullPath;
	}

	class DescriptionRecord : HeaderRecord
	{
		public DescriptionRecord(string description)
		{
			Description = description;
		}
		public string Description { get; }
		public override string ToString() => Description;
	}

	class EngineeringModelRecord : HeaderRecord
	{
		public EngineeringModelRecord(string engineeringModelName, int numberOfTimeSteps, int numberOfExportModules)
		{
			EngineeringModelName = engineeringModelName;
			NumberOfTimeSteps = numberOfTimeSteps;
			NumberOfExportModules = numberOfExportModules;
		}
		public string EngineeringModelName { get; }
		public int NumberOfTimeSteps { get; }
		public int NumberOfExportModules { get; }
		public override string ToString() => $"{EngineeringModelName} {Keyword.nsteps} {NumberOfTimeSteps} {Keyword.nmodules} {NumberOfExportModules}";
	}

	abstract class ExportModuleRecord : HeaderRecord
	{ }

	class VtkXmlExportModuleRecord : ExportModuleRecord
	{
		public override string ToString() => "vtkxml tstep_all domain_all primvars 1 1"; // TODO: this is hard-coded now, enable this to be configurable
	}

	class DomainRecord : HeaderRecord
	{
		public DomainRecord(string domainType)
		{
			DomainType = domainType;
		}
		public string DomainType { get; }
		public override string ToString() => $"{Keyword.domain} {DomainType}";
	}

	class OutputManagerRecord : HeaderRecord
	{
		public override string ToString() => "OutputManager tstep_all dofman_all element_all"; // TODO: avoid hard-coded string
	}

	abstract class DofManagerRecord : InputRecord
	{
		public DofManagerRecord(int id, double x, double y, double z)
		{
			Id = id;
			X = x;
			Y = y;
			Z = z;
		}
		public int Id { get; }
		public double X { get; }
		public double Y { get; }
		public double Z { get; }
	}

	class NodeRecord : DofManagerRecord
	{
		public NodeRecord(int id, double x, double y, double z)
			: base(id, x, y, z)
		{ }
		public override string ToString() => Invariant($"{DofManagerNames.node} {Id} {Keyword.coords} 3 {X} {Y} {Z}");
	}

	class RigidArmNodeRecord : DofManagerRecord
	{
		public RigidArmNodeRecord(int id, double x, double y, double z, int masterId, string parameters)
			: base(id, x, y, z)
		{
			MasterId = masterId;
			Parameters = parameters;
		}
		public int MasterId { get; }
		public string Parameters { get; }
		public override string ToString() => Invariant($"{DofManagerNames.RigidArmNode} {Id} {Keyword.coords} 3 {X} {Y} {Z} {Keyword.master} {MasterId} {Parameters}");
	}

	abstract class NamedRecord : InputRecord
	{
		public NamedRecord(string name, int id)
		{
			Name = name;
			Id = id;
		}
		public string Name { get; }
		public int Id { get; }
		public override string ToString() => $"{Name} {Id}";
	}

	class ElementRecord : NamedRecord
	{
		public ElementRecord(string name, int id, IReadOnlyList<int> nodeIds, string parameters = null)
			: base(name, id)
		{
			NodeIds = nodeIds;
			Parameters = parameters;
		}
		public IReadOnlyList<int> NodeIds { get; }
		public string Parameters { get; }
		public override string ToString()
		{
			string text = $"{Name} {Id} {Keyword.nodes} {NodeIds.Count} {string.Join(" ", NodeIds)}";
			if (string.IsNullOrEmpty(Parameters))
				return text;
			return text + " " + Parameters;
		}

		public ElementRecord WithReplacedNode(int oldNodeId, int newNodeId)
		{
			int[] updatedNodeIds = NodeIds.Select(id => id == oldNodeId ? newNodeId : id).ToArray();
			return new ElementRecord(Name, Id, updatedNodeIds, Parameters);
		}
	}

	class CrossSectionRecord : NamedRecord
	{
		public CrossSectionRecord(string name, int id, string parameters, int materialId, int setId)
			: base(name, id)
		{
			Parameters = parameters;
			MaterialId = materialId;
			SetId = setId;
		}
		public string Parameters { get; }
		public int MaterialId { get; }
		public int SetId { get; }
		public override string ToString() => $"{Name} {Id} {Parameters} {Keyword.material} {MaterialId} {Keyword.set} {SetId}";
	}

	class MaterialRecord : NamedRecord
	{
		public MaterialRecord(string name, int id, string parameters)
			: base(name, id)
		{
			Parameters = parameters;
		}
		public string Parameters { get; }
		public override string ToString() => $"{Name} {Id} {Parameters}";
	}

	class BoundaryConditionRecord : NamedRecord
	{
		public BoundaryConditionRecord(string name, int id, string parameters, int timeFunctionId, int setId)
			: base(name, id)
		{
			Parameters = parameters;
			TimeFunctionId = timeFunctionId;
			SetId = setId;
		}
		public string Parameters { get; }
		public int TimeFunctionId { get; }
		public int SetId { get; }
		public override string ToString() => $"{Name} {Id} {Parameters} {Keyword.loadTimeFunction} {TimeFunctionId} {Keyword.set} {SetId}";
	}

	class TimeFunctionRecord : NamedRecord
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

		public override string ToString()
		{
			var text = new StringBuilder();
			text.Append($"{Name} {Id}");
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

	class SetRecord : InputRecord
	{
		public SetRecord(Set set)
		{
			Set = set;
		}
		public Set Set { get; }
		public override string ToString()
		{
			var text = new StringBuilder();
			text.Append($"{Keyword.set} {Set.Id}");
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
				text.Append($" {Keyword.elementedges} {Set.ElementEdges.Count * 2} {string.Join(" ", Set.ElementEdges.Select(pair => $"{pair.Key} {pair.Value}"))}");
			}
			if (Set.ElementSurfaces.Count > 0)
			{
				text.Append($" {Keyword.elementboundaries} {Set.ElementSurfaces.Count * 2} {string.Join(" ", Set.ElementSurfaces.Select(pair => $"{pair.Key} {pair.Value}"))}");
			}
			return text.ToString();
		}
	}
}
