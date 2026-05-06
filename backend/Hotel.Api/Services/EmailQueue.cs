
using System.Threading.Channels;

public interface IEmailQueue
{
    void Enqueue(Func<CancellationToken, Task> job);
}

// =========================
// 🔥 QUEUE IMPLEMENTATION
// =========================
public class EmailQueue : IEmailQueue
{
    private readonly Channel<Func<CancellationToken, Task>> _queue;

    public EmailQueue()
    {
        _queue = Channel.CreateUnbounded<Func<CancellationToken, Task>>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public void Enqueue(Func<CancellationToken, Task> job)
    {
        if (!_queue.Writer.TryWrite(job))
            throw new InvalidOperationException("Failed to enqueue email job");
    }

    public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}

