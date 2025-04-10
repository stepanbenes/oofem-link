﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Services.DataAccess
{
    public interface ICommand<TEditDto, TPrimaryKey>
	{
		Task CreateAsync(TEditDto dto);
		Task UpdateAsync(TPrimaryKey primaryKey, TEditDto dto);
		Task DeleteAsync(TPrimaryKey primaryKey);
	}
}
