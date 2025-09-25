using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;

namespace ScrumPoker.Infrastructure.Persistence;

public class TableRetrySettings
{
    public bool Enabled { get; set; } = true;
    public int MaxRetries { get; set; } = 5;
    public int DelayMs { get; set; } = 200;
    public int MaxDelayMs { get; set; } = 5000;
    public double RandomizationFactor { get; set; } = 0.2;
}

public interface ITableClientFactory
{
    TableClient GetClient(string tableName);
}

public class TableClientFactory : ITableClientFactory
{
    private readonly IConfiguration _config;
    public TableClientFactory(IConfiguration config) => _config = config;

    public TableClient GetClient(string tableName)
    {
        var conn = _config["Storage:ConnectionString"] ?? _config["AzureWebJobsStorage"] ?? "UseDevelopmentStorage=true";
        var retrySection = _config.GetSection("Storage:Retry");
        var settings = new TableRetrySettings();
        retrySection.Bind(settings);
        var options = new TableClientOptions();
        if (settings.Enabled)
        {
            options.Retry.MaxRetries = settings.MaxRetries;
            options.Retry.Delay = TimeSpan.FromMilliseconds(settings.DelayMs);
            options.Retry.MaxDelay = TimeSpan.FromMilliseconds(settings.MaxDelayMs);
        }
        var service = new TableServiceClient(conn, options);
        var client = service.GetTableClient(tableName);
        client.CreateIfNotExists();
        return client;
    }
}
