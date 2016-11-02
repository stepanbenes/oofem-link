using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Business.Services
{
	public interface IService<TViewDto, TEditDto, TPrimaryKey>
	{
		Task<IReadOnlyList<TViewDto>> GetAllAsync(Func<IQueryable<TViewDto>, IQueryable<TViewDto>> filter = null);
		Task<TViewDto> GetOneAsync(TPrimaryKey primaryKey);

		Task CreateAsync(TEditDto dto);
		Task UpdateAsync(TPrimaryKey primaryKey, TEditDto dto);
		Task DeleteAsync(TPrimaryKey primaryKey);
	}

	public interface IService<TDto, TPrimaryKey> : IService<TDto, TDto, TPrimaryKey>
	{ }
}
