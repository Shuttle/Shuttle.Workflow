using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Shuttle.Workflow.SqlServer.ValueConverters;

public class NullableTimeSpanStringValueConverter(ConverterMappingHints? mappingHints = null) : ValueConverter<TimeSpan?, string?>(v => v.HasValue ? v.ToString() : null,
    v => v == null ? null : TimeSpan.Parse(v),
    mappingHints)
{
    public NullableTimeSpanStringValueConverter() : this(null)
    {
    }
}