using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.DbEntities
{
	public class Project
	{
		public int Id { get; set; }
		[Required]
		public string Name { get; set; }
		public virtual ICollection<Simulation> Simulations { get; set; } = new List<Simulation>();
	}
}
