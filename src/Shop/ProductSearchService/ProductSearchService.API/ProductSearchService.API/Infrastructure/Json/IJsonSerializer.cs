﻿namespace ProductSearchService.API.Infrastructure.Json
{
    public interface IJsonSerializer
    {
        string Serialize(object value);

        T Deserialize<T>(string json);

        dynamic Parse(string json);
    }
}
