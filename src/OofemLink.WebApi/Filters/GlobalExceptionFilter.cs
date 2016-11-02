using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OofemLink.WebApi.Filters
{
	public class GlobalExceptionFilter : IExceptionFilter
	{
		public void OnException(ExceptionContext context)
		{
			context.Result = new ContentResult { Content = context.Exception.GetType().FullName + ": " + context.Exception.Message, StatusCode = (int)HttpStatusCode.InternalServerError };
		}
	}
}
