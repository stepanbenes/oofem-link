using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public class Attribute : IModelEntity
    {
		public int ModelId { get; set; }
		public virtual Model Model { get; set; }
		public int Id { get; set; }
	}
}
