using NUnit.Framework;

namespace Shuttle.Workflow.Tests.Api;

[SetUpFixture]
public class ApiFixture
{
    [OneTimeSetUp]
    public void Setup()
    {
        Environment.SetEnvironmentVariable("CONFIGURATION_FOLDER", ".");
    }
}