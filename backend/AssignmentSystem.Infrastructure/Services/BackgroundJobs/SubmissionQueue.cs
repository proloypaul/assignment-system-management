using System.Threading.Channels;
using AssignmentSystem.Application.Features.Submissions.DTOs;
using AssignmentSystem.Application.Features.Submissions.Interfaces;

namespace AssignmentSystem.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// In-memory queue backed by <see cref="Channel{T}"/>.
/// Registered as a <b>Singleton</b> so the same channel instance
/// is shared between the HTTP writers and the BackgroundService reader.
/// </summary>
public sealed class SubmissionQueue : ISubmissionQueue
{
    // UnboundedChannel: no back-pressure, jobs are accepted immediately.
    // For high-throughput systems, consider BoundedChannel with a sensible capacity.
    private readonly Channel<SubmissionJob> _channel =
        Channel.CreateUnbounded<SubmissionJob>(new UnboundedChannelOptions
        {
            SingleReader = true,   // Only SubmissionBackgroundWorker reads
            SingleWriter = false   // Multiple HTTP requests may write concurrently
        });

    /// <inheritdoc/>
    public ValueTask EnqueueAsync(SubmissionJob job, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(job, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<SubmissionJob> DequeueAsync(CancellationToken cancellationToken = default)
        => _channel.Reader.ReadAsync(cancellationToken);
}
