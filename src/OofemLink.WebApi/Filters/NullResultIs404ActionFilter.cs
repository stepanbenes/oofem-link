using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OofemLink.WebApi.Filters
{
	public class NullResultIs404ActionFilter : IActionFilter
	{
		public void OnActionExecuted(ActionExecutedContext context)
		{
			var objectResult = (context.Result as ObjectResult);
			if (objectResult != null && objectResult.Value == null)
			{
				context.Result = new NotFoundResult();
			}
		}

		public void OnActionExecuting(ActionExecutingContext context)
		{
			// do nothing
		}
	}
}
