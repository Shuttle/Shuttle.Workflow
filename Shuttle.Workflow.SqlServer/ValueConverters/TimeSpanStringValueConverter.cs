using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Shuttle.Workflow.SqlServer.ValueConverters;

public class TimeSpanStringValueConverter(ConverterMappingHints? mappingHints = null) : ValueConverter<TimeSpan, string>(v => v.ToString(),
    v => TimeSpan.Parse(v),
    mappingHints)
{
    public TimeSpanStringValueConverter() : this(null)
    {
    }
}