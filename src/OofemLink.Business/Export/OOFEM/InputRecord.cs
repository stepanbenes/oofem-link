using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Business.Export.OOFEM
{
	// TODO: come up with fluent API to build records

    abstract class InputRecord
    {
		public string Keyword { get; }
		public InputRecord(string keyword)
		{
			Debug.Assert(!keyword.Contains(Environment.NewLine));
			Keyword = keyword;
		}
		public override string ToString() => Keyword;
	}

	class KeyRecord : InputRecord
	{
		public int Id { get; }
		public IReadOnlyList<InputRecord> Parameters { get; }
		public KeyRecord(string keyword, int id, params InputRecord[] parameters)
			: base(keyword)
		{
			Id = id;
			Parameters = parameters;
		}
		public override string ToString() => Keyword + " " + Id + " " + string.Join(" ", Parameters);
	}

	class EmptyRecord : InputRecord
	{
		public EmptyRecord(string keyword)
			: base(keyword)
		{ }
	}

	class CommentRecord : InputRecord
	{
		public CommentRecord(string comment)
			: base(comment)
		{ }
		public override string ToString() => "# " + Keyword;
	}

	class ValueRecord<T> : InputRecord
	{
		public T Value { get; }
		public ValueRecord(string keyword, T value)
			: base(keyword)
		{
			Value = value;
		}
		public override string ToString() => Keyword + " " + Value;
	}

	class StringRecord : ValueRecord<string>
	{
		public StringRecord(string keyword, string text)
			: base(keyword, text)
		{ }
	}

	class ArrayRecord<T> : InputRecord
	{
		public IReadOnlyList<T> Values { get; }
		public ArrayRecord(string keyword, params T[] values)
			: base(keyword)
		{
			Values = values;
		}
		public override string ToString() => Keyword + " " + Values.Count + " " + string.Join(" ", Values);
	}
}
