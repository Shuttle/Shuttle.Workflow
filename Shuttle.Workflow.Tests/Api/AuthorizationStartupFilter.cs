using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Shuttle.Workflow.Tests.Api;

internal class AuthorizationStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            builder.UseMiddleware<AuthorizationMiddleware>();
            next(builder);
        };
    }
}

internal class AuthorizationMiddleware(RequestDelegate requestDelegate)
{
    public async Task Invoke(HttpContext httpContext)
    {
        httpContext.User.AddIdentity(new("Bearer"));

        await requestDelegate.Invoke(httpContext);
    }
}