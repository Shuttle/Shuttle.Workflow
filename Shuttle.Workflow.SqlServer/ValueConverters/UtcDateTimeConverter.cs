using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Shuttle.Workflow.SqlServer.ValueConverters;

public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
{
    // writing to DB — value is already UTC, store as-is
    // reading from DB — SQL Server has no concept of Kind, so tag it as Utc
}