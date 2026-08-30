using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Shuttle.Workflow.SqlServer.ValueConverters;

public class UtcNullableDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(v => v,
    v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);