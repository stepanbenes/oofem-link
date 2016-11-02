using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Business.Services
{
	public interface IService<TViewDto, TEditDto, TPrimaryKey>
	{
		TViewDto Get(TPrimaryKey primaryKey);
		IReadOnlyList<TViewDto> GetAll(Func<IQueryable<TViewDto>, IQueryable<TViewDto>> filter = null);

		void Create(TEditDto dto);
		void Update(TPrimaryKey primaryKey, TEditDto dto);
		void Delete(TPrimaryKey primaryKey);
	}

	public interface IService<TDto, TPrimaryKey> : IService<TDto, TDto, TPrimaryKey>
	{ }
}
