using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Services.DataAccess
{
    public interface IQuery<TViewDto, TPrimaryKey>
	{
		Task<IReadOnlyList<TViewDto>> GetAllAsync(Func<IQueryable<TViewDto>, IQueryable<TViewDto>> filter = null);
		Task<TViewDto> GetOneAsync(TPrimaryKey primaryKey);
	}
}
