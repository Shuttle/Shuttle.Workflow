namespace Shuttle.Workflow;

public class Permissions
{
    public const string Administrator = "workflow://*";

    public class ProcessDefinitions
    {
        public const string Manage = "workflow://process-definitions/manage";
        public const string View = "workflow://process-definitions/view";
    }

    public class Processes
    {
        public const string Manage = "workflow://processes/manage";
        public const string View = "workflow://processes/view";
    }

    public class Semaphores
    {
        public const string Manage = "workflow://semaphores/manage";
        public const string View = "workflow://semaphores/view";
    }

    public class States
    {
        public const string Manage = "workflow://states/manage";
        public const string View = "workflow://states/view";
    }
}