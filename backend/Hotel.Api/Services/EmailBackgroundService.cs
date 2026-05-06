
public class EmailBackgroundService : BackgroundService
{
    private readonly EmailQueue _queue;
    private readonly ILogger<EmailBackgroundService> _logger;

    public EmailBackgroundService(EmailQueue queue, ILogger<EmailBackgroundService> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queue.DequeueAsync(stoppingToken);
                await job(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email queue");
            }
        }

        _logger.LogInformation("Email background service stopped");
    }
}

