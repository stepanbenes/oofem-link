using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper.QueryableExtensions;
using OofemLink.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace OofemLink.Services.DataAccess
{
    public abstract class DataService
	{
		protected DataContext Context { get; }
		protected ILogger Logger { get; }

		protected DataService(DataContext context, ILoggerFactory loggerFactory)
		{
			Context = context;
			Logger = loggerFactory.CreateLogger(GetType());
		}

		protected IQueryable<TDto> GetQuery<TEntity, TDto>(Func<IQueryable<TDto>, IQueryable<TDto>> queryExtensionFunc) where TEntity : class
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
