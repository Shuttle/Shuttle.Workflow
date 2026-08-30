using System.Diagnostics.CodeAnalysis;

namespace Shuttle.Workflow.SqlServer;

public static class RecordNotFoundExtensions
{
    extension<T>([NotNull] T? entity) where T : class
    {
        [return: NotNull]
        public T GuardAgainstRecordNotFound(object id)
        {
            return entity ?? throw RecordNotFoundException.For<T>(id);
        }
    }
}