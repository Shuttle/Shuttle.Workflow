using Microsoft.Extensions.Options;

namespace Shuttle.Workflow.SqlServer;

public class WorkflowSqlServerOptionsValidator : IValidateOptions<WorkflowSqlServerOptions>
{
    public ValidateOptionsResult Validate(string? name, WorkflowSqlServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrEmpty(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail("Options 'ConnectionString' may not be empty.");
        }

        return ValidateOptionsResult.Success;
    }
}