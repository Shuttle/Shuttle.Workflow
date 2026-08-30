namespace Shuttle.Workflow.RestClient;

public class StateSpecificationFactory : IStateSpecificationFactory
{
    public IStateSpecification Create(string type, string source, string value)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentNullException(nameof(source));
        }

        switch (type.ToUpperInvariant())
        {
            case "REGEX":
            case "CONTAINS":
            {
                return new RegexStateStateSpecification(source, value);
            }
            case "STARTSWITH":
            {
                return new RegexStateStateSpecification(source, $"^{value}");
            }
            case "ENDSWITH":
            {
                return new RegexStateStateSpecification(source, $"{value}$");
            }
            case "EQUALS":
            {
                return new RegexStateStateSpecification(source, $"^{value}$");
            }
            default:
            {
                throw new NotSupportedException($"State specification type '{type}' is not supported.");
            }
        }
    }
}