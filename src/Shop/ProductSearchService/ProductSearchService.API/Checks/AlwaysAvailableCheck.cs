using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.HealthChecks;

namespace ProductSearchService.API.Checks
{
    public class AlwaysAvailableCheck : IHealthCheckResult
    {
        //public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        //    => Task.FromResult(result: new HealthCheckResult(status: HealthStatus.Healthy));
        public CheckStatus CheckStatus => CheckStatus.Healthy;

        public string Description => "AlwaysAvailableCheck";

        public IReadOnlyDictionary<string, object> Data => new ReadOnlyDictionary<string, object>(dictionary: null);
    }
}