using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.ModelEntities
{
    public abstract class ModelEntity
    {
		public int Id { get; set; }
		public virtual int ModelId { get; set; }
		public virtual Model Model { get; set; }
	}
}
