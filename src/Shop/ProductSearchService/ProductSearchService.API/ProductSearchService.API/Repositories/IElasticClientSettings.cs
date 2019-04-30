namespace ProductSearchService.API.Repositories
{
    public interface IElasticClientSettings
    {
        string Uri { get; }

        int RequestTimoutInMinutes { get; }
        
        bool EnableHttpCompression { get; }
        
        bool EnableHttpPipelining { get; }
        
        bool PrettyJson { get; }
        
        int NumberOfShards { get; }
        
        int NumberOfReplicas { get; }
    }
}
