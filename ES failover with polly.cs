public class ElasticsearchService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _primaryClusterUrl;
    private readonly string _backupClusterUrl;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly TimeSpan _primaryCheckInterval;

    private AsyncPolicyWrap<string> _resiliencePolicy;
    private AsyncCircuitBreakerPolicy _primaryCircuitBreaker;
    private Timer _primaryCheckTimer;

    public ElasticsearchService(HttpClient httpClient, IConfiguration configuration, ILogger<ElasticsearchService> logger)
    {
        _httpClient = httpClient;
        _primaryClusterUrl = configuration["Elasticsearch:PrimaryClusterUrl"];
        _backupClusterUrl = configuration["Elasticsearch:BackupClusterUrl"];
        _primaryCheckInterval = TimeSpan.FromMinutes(10);
        _logger = logger;

        InitializePollyPolicies();
        StartPrimaryCheckTimer();
    }

    private void InitializePollyPolicies()
    {
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        _primaryCircuitBreaker = Policy
            .Handle<HttpRequestException>()
            .CircuitBreakerAsync(5, TimeSpan.FromMinutes(2));

        var fallbackPolicy = Policy<string>
            .Handle<BrokenCircuitException>()
            .FallbackAsync(async (ct) => await SearchOnBackupClusterAsync());

        _resiliencePolicy = fallbackPolicy.WrapAsync(_primaryCircuitBreaker).WrapAsync(retryPolicy);
    }

    public async Task<string> SearchAsync(string queryJson)
    {
        return await _resiliencePolicy.ExecuteAsync(async () =>
        {
            var url = $"{_primaryClusterUrl}/_search";
            var content = new StringContent(queryJson, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        });
    }

    private async Task<string> SearchOnBackupClusterAsync()
    {
        var url = $"{_backupClusterUrl}/_search";
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private void StartPrimaryCheckTimer()
    {
        _primaryCheckTimer = new Timer(CheckPrimaryClusterAvailability, null, _primaryCheckInterval, _primaryCheckInterval);
    }

    private async void CheckPrimaryClusterAvailability(object state)
    {
        if (_primaryCircuitBreaker.CircuitState == CircuitState.Open)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_primaryClusterUrl}/_cluster/health");
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Primary Elasticsearch cluster is back online. Resetting circuit breaker.");
                    _primaryCircuitBreaker.Reset();
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to check primary cluster availability.");
            }
        }
    }

    public void Dispose()
    {
        _primaryCheckTimer?.Dispose();
    }
}
