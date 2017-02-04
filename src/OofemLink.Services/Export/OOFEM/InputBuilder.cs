﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Globalization;
using System.Diagnostics;

namespace OofemLink.Services.Export.OOFEM
{
	class InputBuilder
	{
		#region Fields, constructor

		OutputFileRecord outputFileRecord;
		DescriptionRecord descriptionRecord;
		EngineeringModelRecord engineeringModelRecord;
		DomainRecord domainRecord;
		OutputManagerRecord outputManagerRecord;

		// TODO: use SortedDictionary<,> class
		
		readonly Dictionary<int, BoundaryConditionRecord> boundaryConditionRecords;
		readonly Dictionary<int, TimeFunctionRecord> timeFunctionRecords;

		readonly Dictionary<int, DofManagerRecord> dofManagerRecords;
		readonly Dictionary<int, ElementRecord> elementRecords;

		readonly Dictionary<int, CrossSectionRecord> crossSectionRecords;
		readonly Dictionary<int, MaterialRecord> materialRecords;

		// TODO: remove set record list, sets can be generated from attributes
		readonly List<SetRecord> setRecords;

		int maxDofManagerId, maxElementId; // TODO: can be removed if SortedDictionaries are used

		public InputBuilder()
		{
			dofManagerRecords = new Dictionary<int, DofManagerRecord>();
			elementRecords = new Dictionary<int, ElementRecord>();
			crossSectionRecords = new Dictionary<int, CrossSectionRecord>();
			materialRecords = new Dictionary<int, MaterialRecord>();
			boundaryConditionRecords = new Dictionary<int, BoundaryConditionRecord>();
			timeFunctionRecords = new Dictionary<int, TimeFunctionRecord>();
			setRecords = new List<SetRecord>();
		}

		public int MaxDofManagerId => maxDofManagerId;
		public int MaxElementId => maxElementId;

		#endregion

		#region Public methods

		public void WriteToFile(string fileFullPath)
		{
			// TODO: lock builder during writing

			assignIndexToAllIndexableRecords();

			using (var stream = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write, FileShare.None))
			using (var streamWriter = new StreamWriter(stream))
			{
				// Write header comment
				streamWriter.WriteLine($"# OOFEM input file. Generated by {getProgramDescription()} on machine {Environment.MachineName}");
				if (outputFileRecord != null)
					streamWriter.WriteLine(outputFileRecord.ToString());
				if (descriptionRecord != null)
					streamWriter.WriteLine(descriptionRecord.ToString());
				if (engineeringModelRecord != null)
				{
					streamWriter.WriteLine(engineeringModelRecord.ToString());
					foreach (var record in engineeringModelRecord.ExportModules)
						streamWriter.WriteLine(record.ToString());
				}

				if (domainRecord != null)
					streamWriter.WriteLine(domainRecord.ToString());
				if (outputManagerRecord != null)
					streamWriter.WriteLine(outputManagerRecord.ToString());

				// calculate and print numbers of records
				streamWriter.WriteLine(buildRecordCountsString());

				streamWriter.WriteLine($"# DOF-MANAGERS");
				foreach (var record in dofManagerRecords.Values)
					streamWriter.WriteLine(record);

				streamWriter.WriteLine($"# ELEMENTS");
				foreach (var record in elementRecords.Values)
					streamWriter.WriteLine(record);

				streamWriter.WriteLine($"# CROSS-SECTIONS");
				foreach (var record in crossSectionRecords.Values)
					streamWriter.WriteLine(record);

				streamWriter.WriteLine($"# MATERIALS");
				foreach (var record in materialRecords.Values)
					streamWriter.WriteLine(record);

				streamWriter.WriteLine($"# BOUNDARY CONDITIONS");
				foreach (var record in boundaryConditionRecords.Values)
					streamWriter.WriteLine(record);

				streamWriter.WriteLine($"# TIME FUNCTIONS");
				foreach (var record in timeFunctionRecords.Values)
					streamWriter.WriteLine(record);

				streamWriter.WriteLine($"# SETS");
				foreach (var record in setRecords)
					streamWriter.WriteLine(record);
			}
		}

		public void AddOutputFileRecord(OutputFileRecord record)
		{
			if (outputFileRecord != null)
				throw new ArgumentException("OutputFile record was already set");
			outputFileRecord = record;
		}

		public void AddDescriptionRecord(DescriptionRecord record)
		{
			if (descriptionRecord != null)
				throw new ArgumentException("Description record was already set");
			descriptionRecord = record;
		}

		public void AddEngineeringModelRecord(EngineeringModelRecord record)
		{
			if (engineeringModelRecord != null)
				throw new ArgumentException("EngineeringModel record was already set");
			engineeringModelRecord = record;
		}

		public void AddDomainRecord(DomainRecord record)
		{
			if (domainRecord != null)
				throw new ArgumentException("Domain record was already set");
			domainRecord = record;
		}

		public void AddOutputManagerRecord(OutputManagerRecord record)
		{
			if (outputManagerRecord != null)
				throw new ArgumentException("OutputManager record was already set");
			outputManagerRecord = record;
		}

		public void AddDofManagerRecord(DofManagerRecord record)
		{
			dofManagerRecords.Add(record.Id, record);
			maxDofManagerId = Math.Max(maxDofManagerId, record.Id);
		}

		public void AddElementRecord(ElementRecord record)
		{
			elementRecords.Add(record.Id, record);
			maxElementId = Math.Max(maxElementId, record.Id);
		}

		public void AddCrossSectionRecord(CrossSectionRecord record)
		{
			crossSectionRecords.Add(record.Id, record);
		}

		public void AddMaterialRecord(MaterialRecord record)
		{
			materialRecords.Add(record.Id, record);
		}

		public void AddBoundaryConditionRecord(BoundaryConditionRecord record)
		{
			boundaryConditionRecords.Add(record.Id, record);
		}

		public void AddTimeFunctionRecord(TimeFunctionRecord record)
		{
			timeFunctionRecords.Add(record.Id, record);
		}

		public void AddSetRecord(SetRecord record)
		{
			setRecords.Add(record);
		}

		public IReadOnlyDictionary<int, DofManagerRecord> DofManagerRecords => dofManagerRecords;
		public IReadOnlyDictionary<int, ElementRecord> ElementRecords => elementRecords;
		public IReadOnlyDictionary<int, CrossSectionRecord> CrossSectionRecords => crossSectionRecords;
		public IReadOnlyDictionary<int, MaterialRecord> MaterialRecords => materialRecords;
		public IReadOnlyList<SetRecord> SetRecords => setRecords;

		public IReadOnlyDictionary<int, BoundaryConditionRecord> BoundaryConditionRecords => boundaryConditionRecords;
		public IReadOnlyDictionary<int, TimeFunctionRecord> TimeFunctionRecords => timeFunctionRecords;

		#endregion

		#region Private methods

		private static string getProgramDescription()
		{
			var assembly = Assembly.GetEntryAssembly();
			var assemblyTitleAttribute = assembly.GetCustomAttribute<AssemblyTitleAttribute>();
			var assemblyFileVersionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
			return $"{assemblyTitleAttribute.Title} version {assemblyFileVersionAttribute.Version}";
		}

		private string buildRecordCountsString()
		{
			int dofManagerCount = dofManagerRecords.Count;
			int elementCount = elementRecords.Count;
			int crossSectionCount = crossSectionRecords.Count;
			int materialCount = materialRecords.Count;
			int boundaryConditionCount = boundaryConditionRecords.Count;
			int initialConditionCount = 0; /*no initial conditions for time-independent analysis (statics)*/
			int timeFunctionCount = timeFunctionRecords.Count;
			int setCount = setRecords.Count;

			return $"{Keyword.ndofman} {dofManagerCount} {Keyword.nelem} {elementCount} {Keyword.ncrosssect} {crossSectionCount} {Keyword.nmat} {materialCount} {Keyword.nbc} {boundaryConditionCount} {Keyword.nic} {initialConditionCount} {Keyword.nltf} {timeFunctionCount} {Keyword.nset} {setCount}";
		}

		private void assignIndexToAllIndexableRecords()
		{
			int index = 1;
			foreach (IIndexableRecord record in crossSectionRecords.Values)
				record.InputIndex = index++;
			index = 1;
			foreach (IIndexableRecord record in materialRecords.Values)
				record.InputIndex = index++;
			index = 1;
			foreach (IIndexableRecord record in boundaryConditionRecords.Values)
				record.InputIndex = index++;
			index = 1;
			foreach (IIndexableRecord record in timeFunctionRecords.Values)
				record.InputIndex = index++;
			index = 1;
			foreach (IIndexableRecord record in setRecords)
				record.InputIndex = index++;
		}

		#endregion
	}
}
