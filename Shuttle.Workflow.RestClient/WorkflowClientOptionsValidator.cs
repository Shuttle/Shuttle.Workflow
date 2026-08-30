using Microsoft.Extensions.Options;

namespace Shuttle.Workflow.RestClient;

public class WorkflowClientOptionsValidator : IValidateOptions<WorkflowClientOptions>
{
    public ValidateOptionsResult Validate(string? name, WorkflowClientOptions clientOptions)
    {
        if (clientOptions.BaseAddress == null)
        {
            return ValidateOptionsResult.Fail("BaseAddress must be provided.");
        }

        return ValidateOptionsResult.Success;
    }
}