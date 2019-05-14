namespace ProductSearchService.API.Infrastructure
{
    public interface ICorrelationIdFactory
    {
        string Build();
    }
}
