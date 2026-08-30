using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Shuttle.Workflow.Builder;

public sealed class SemaphoreOptionsResolver : ISemaphoreOptionsResolver
{
    private readonly Compiled[] _compiled;

    public SemaphoreOptionsResolver(IOptions<WorkflowOptions> options)
    {
        _compiled = options.Value.Semaphores
            .Select(s => new Compiled(new(s.KeyRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase), s))
            .ToArray();
    }

    public SemaphoreOptions? Resolve(string key)
    {
        return _compiled.FirstOrDefault(c => c.Regex.IsMatch(key))?.Options;
    }

    private sealed record Compiled(Regex Regex, SemaphoreOptions Options);
}