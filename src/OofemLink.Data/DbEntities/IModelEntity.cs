using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.DbEntities
{
	public interface IModelEntity
	{
		int ModelId { get; }
		Model Model { get; }
		int Id { get; }
	}
}
