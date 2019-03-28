using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.HealthChecks;

namespace ProductSearchService.API.Checks
{
    public class AlwaysAvailableCheck : IHealthCheckResult
    {
        public CheckStatus CheckStatus => CheckStatus.Healthy;

        public string Description => "AlwaysAvailableCheck";

        public IReadOnlyDictionary<string, object> Data => new ReadOnlyDictionary<string, object>(dictionary: null);
    }
}