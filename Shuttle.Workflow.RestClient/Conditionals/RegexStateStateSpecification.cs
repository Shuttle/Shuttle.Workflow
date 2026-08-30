using System.Text.RegularExpressions;
using Shuttle.Workflow.RestClient.v1;
using Shuttle.Workflow.WebApi.Contracts.v1;

namespace Shuttle.Workflow.RestClient;

public class RegexStateStateSpecification(string source, string value) : IStateSpecification
{
    private readonly Regex _regex = new(value, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public string Source { get; } = !string.IsNullOrWhiteSpace(source) ? source : throw new ArgumentNullException(nameof(source));

    public bool IsSatisfiedBy(State candidate, DateTimeOffset? at = null)
    {
        return candidate == null
            ? throw new ArgumentNullException(nameof(candidate))
            : _regex.IsMatch(candidate.FindItem(Source, at)?.Value ?? string.Empty);
    }
}