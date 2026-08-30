using System.Reflection;
using Shuttle.Mediator;
using Shuttle.Workflow.Builder;

namespace Shuttle.Workflow.Application;

public static class WorkflowBuilderExtensions
{
    extension(WorkflowBuilder workflowBuilder)
    {
        public WorkflowBuilder RegisterParticipants()
        {
            ArgumentNullException.ThrowIfNull(workflowBuilder);

            workflowBuilder.Services.AddMediator()
                .AddParticipantsFrom(Assembly.Load("Shuttle.Workflow.Application"));

            return workflowBuilder;
        }
    }
}