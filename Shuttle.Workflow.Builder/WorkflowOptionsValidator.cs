using Microsoft.Extensions.Options;

namespace Shuttle.Workflow.Builder;

public class WorkflowOptionsValidator : IValidateOptions<WorkflowOptions>
{
    public ValidateOptionsResult Validate(string? name, WorkflowOptions options)
    {
        foreach (var semaphoreOptions in options.Semaphores)
        {
            if (semaphoreOptions.Lifetime < semaphoreOptions.MinimumLifetime)
            {
                return ValidateOptionsResult.Fail($"The semaphore 'Lifetime' may not be less than the 'MinimumLifetime' for key regex '{semaphoreOptions.KeyRegex}'.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}