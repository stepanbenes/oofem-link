using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Business.Import;
using OofemLink.Data;
using OofemLink.Data.ModelEntities;

namespace OofemLink.Business
{
    public class ModelBuilder
    {
		readonly Model model;

		private ModelBuilder(Model model)
		{
			Debug.Assert(model != null);
			this.model = model;
		}

		public ModelBuilder()
			: this(new Model())
		{ }

		public static ModelBuilder CreateFromExistingModel(Model model) => new ModelBuilder(model);

		//public ModelBuilder AddMacro(Macro newMacro) { }
	}
}
