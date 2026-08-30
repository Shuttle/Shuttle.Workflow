using System.Reflection;
using Asp.Versioning;
using Asp.Versioning.Builder;
using Shuttle.Access.AspNetCore;

namespace Shuttle.Workflow.WebApi.Endpoints;

public static class ServerEndpoints
{
    extension(WebApplication app)
    {
        public WebApplication MapServerEndpoints(ApiVersionSet versionSet)
        {
            var apiVersion1 = new ApiVersion(1, 0);

            app.MapGet("/v{version:apiVersion}/server/version", (HttpContext _) =>
                {
                    var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

                    return new { Version = $"{version.Major}.{version.Minor}.{version.Build}" };
                })
                .Produces(200, typeof(object))
                .RequireSession()
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            return app;
        }
    }
}