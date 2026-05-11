using System.Threading.Channels;

namespace Hotel.Api.Services;

public interface IHotelPriceSummaryUpdater
{
    ValueTask EnqueueBranchAsync(string branchCode, CancellationToken cancellationToken = default);
    ValueTask EnqueueSlugAsync(string slug, CancellationToken cancellationToken = default);
}

public sealed record HotelPriceSummaryUpdateRequest(string? BranchCode, string? Slug);

public class HotelPriceSummaryUpdateQueue : IHotelPriceSummaryUpdater
{
    private readonly Channel<HotelPriceSummaryUpdateRequest> _channel;

    public HotelPriceSummaryUpdateQueue()
    {
        _channel = Channel.CreateUnbounded<HotelPriceSummaryUpdateRequest>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ChannelReader<HotelPriceSummaryUpdateRequest> Reader => _channel.Reader;

    public ValueTask EnqueueBranchAsync(string branchCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(branchCode))
            return ValueTask.CompletedTask;

        return _channel.Writer.WriteAsync(
            new HotelPriceSummaryUpdateRequest(branchCode.Trim().ToUpperInvariant(), null),
            cancellationToken);
    }

    public ValueTask EnqueueSlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return ValueTask.CompletedTask;

        return _channel.Writer.WriteAsync(
            new HotelPriceSummaryUpdateRequest(null, slug.Trim().ToLowerInvariant()),
            cancellationToken);
    }
}

public class HotelPriceSummaryUpdateWorker : BackgroundService
{
    private readonly HotelPriceSummaryUpdateQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HotelPriceSummaryUpdateWorker> _logger;

    public HotelPriceSummaryUpdateWorker(
        HotelPriceSummaryUpdateQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<HotelPriceSummaryUpdateWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pending = new Dictionary<string, HotelPriceSummaryUpdateRequest>(StringComparer.OrdinalIgnoreCase);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = await _queue.Reader.ReadAsync(stoppingToken);
                AddPending(pending, request);

                while (_queue.Reader.TryRead(out var next))
                {
                    AddPending(pending, next);
                }

                foreach (var update in pending.Values.ToList())
                {
                    await ProcessAsync(update, stoppingToken);
                }

                pending.Clear();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hotel price summary update worker failed");
            }
        }
    }

    private static void AddPending(
        Dictionary<string, HotelPriceSummaryUpdateRequest> pending,
        HotelPriceSummaryUpdateRequest request)
    {
        var key = request.BranchCode is not null
            ? $"branch:{request.BranchCode}"
            : $"slug:{request.Slug}";

        pending[key] = request;
    }

    private async Task ProcessAsync(HotelPriceSummaryUpdateRequest request, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<HotelPriceSummaryService>();

        if (!string.IsNullOrWhiteSpace(request.BranchCode))
        {
            await service.UpdateByBranchCodeAsync(request.BranchCode, ct);
            return;
        }

        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            await service.UpdateBySlugAsync(request.Slug, ct);
        }
    }
}
