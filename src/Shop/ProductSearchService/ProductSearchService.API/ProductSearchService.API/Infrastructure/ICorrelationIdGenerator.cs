namespace ProductSearchService.API.Infrastructure
{
    public interface ICorrelationIdGenerator
    {
        string Generate();
    }
}
