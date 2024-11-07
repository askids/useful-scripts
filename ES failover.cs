using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class ElasticsearchService
{
    private readonly HttpClient _httpClient;
    private readonly string _primaryClusterUrl;
    private readonly string _backupClusterUrl;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly TimeSpan _primaryCheckInterval;
    
    private bool _useBackupCluster;
    private Timer _primaryCheckTimer;

    public ElasticsearchService(HttpClient httpClient, IConfiguration configuration, ILogger<ElasticsearchService> logger)
    {
        _httpClient = httpClient;
        _primaryClusterUrl = configuration["Elasticsearch:PrimaryClusterUrl"];
        _backupClusterUrl = configuration["Elasticsearch:BackupClusterUrl"];
        _primaryCheckInterval = TimeSpan.FromMinutes(10); // Interval to recheck primary cluster
        _logger = logger;

        // Start with primary cluster; no fallback initially
        _useBackupCluster = false;

        // Initialize timer to check the primary cluster availability in the background
        _primaryCheckTimer = new Timer(CheckPrimaryClusterAvailability, null, _primaryCheckInterval, _primaryCheckInterval);
    }

    public async Task<string> SearchAsync(string queryJson)
    {
        string url = GetCurrentClusterUrl();
        var content = new StringContent(queryJson, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync($"{url}/_search", content);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    HandleFailover();
                    return await SearchAsync(queryJson); // Retry with the backup cluster if primary fails
                }
                response.EnsureSuccessStatusCode(); // Throws if the response status is not success
            }

            // Reset to primary on successful response
            _useBackupCluster = false;
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Elasticsearch request failed.");
            HandleFailover();
            return await SearchAsync(queryJson); // Retry with the backup cluster if primary fails
        }
    }

    private string GetCurrentClusterUrl()
    {
        return _useBackupCluster ? _backupClusterUrl : _primaryClusterUrl;
    }

    private void HandleFailover()
    {
        if (!_useBackupCluster)
        {
            _logger.LogWarning("Primary cluster unavailable, switching to backup cluster.");
            _useBackupCluster = true;
        }
    }

    private async void CheckPrimaryClusterAvailability(object state)
    {
        if (_useBackupCluster)
        {
            try
            {
                // Make a lightweight health check call to the primary cluster
                var response = await _httpClient.GetAsync($"{_primaryClusterUrl}/_cluster/health");
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Primary Elasticsearch cluster is back online. Switching back.");
                    _useBackupCluster = false;
                }
                else
                {
                    _logger.LogInformation("Primary cluster still unavailable.");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to check primary cluster availability.");
            }
        }
    }
}
