using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Business.Export
{
    abstract class OofemInputRecord
    {
    }

	class StringInputRecord : OofemInputRecord
	{
		readonly string text;
		public StringInputRecord(string text)
		{
			Debug.Assert(!text.Contains(Environment.NewLine));
			this.text = text;
		}
		public override string ToString() => text;
	}

	class CommentInputRecord : StringInputRecord
	{
		public CommentInputRecord(string comment)
			: base(comment)
		{ }
		public override string ToString() => "# " + base.ToString();
	} 
}
