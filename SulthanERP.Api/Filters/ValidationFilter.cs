using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Sulthan.Core.Common;

namespace SulthanERP.Api.Filters;

public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
            return;

        var errors = context.ModelState
            .Where(x => x.Value!.Errors.Count > 0)
            .ToDictionary(
                x => x.Key,
                x => x.Value!.Errors
                    .Select(e => e.ErrorMessage)
                    .ToArray());

        context.Result = new BadRequestObjectResult(new
        {
            Success = false,
            Message = "Validation failed.",
            Errors = errors
        });
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}