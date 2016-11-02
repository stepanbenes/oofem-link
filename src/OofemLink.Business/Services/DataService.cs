using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper.QueryableExtensions;
using OofemLink.Data;
using Microsoft.EntityFrameworkCore;

namespace OofemLink.Business.Services
{
    public abstract class DataService<TEntity> where TEntity : class
	{
		protected DataContext Context { get; }

		protected DbSet<TEntity> Entities => Context.Set<TEntity>();

		protected DataService(DataContext context)
		{
			Context = context;
		}

		protected IQueryable<TDto> GetQuery<TDto>(Func<IQueryable<TDto>, IQueryable<TDto>> queryExtensionFunc)
		{
			var query = Context.Set<TEntity>().AsNoTracking().ProjectTo<TDto>();
			if (queryExtensionFunc == null)
				return query;
			return queryExtensionFunc(query);

			// AsNoTracking:
			// we don't need change tracking when we use it for one-way displaying
			// the result on the web, in stateless http request bound environment.
			// We can consider using tracked entities for more complex scenarios, but it this fragment isn't designed for it.

			// ProjectTo:
			// Map entities to dtos
			// this would load entire table into memory: !!!
			// return Mapper.Map<IQueryable<EditPersonDto>>(Context.People);
			// better - construct expression - EF will then generate SQL
		}
	}
}
