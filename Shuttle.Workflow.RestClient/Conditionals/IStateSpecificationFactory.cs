namespace Shuttle.Workflow.RestClient;

public interface IStateSpecificationFactory
{
    IStateSpecification Create(string type, string source, string value);
}