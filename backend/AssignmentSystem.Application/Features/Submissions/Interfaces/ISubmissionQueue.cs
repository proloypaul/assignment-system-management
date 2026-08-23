using AssignmentSystem.Application.Features.Submissions.DTOs;

namespace AssignmentSystem.Application.Features.Submissions.Interfaces;

/// <summary>
/// Abstraction over the underlying Channel&lt;SubmissionJob&gt;.
/// Registered as Singleton so both the HTTP pipeline (writer)
/// and the BackgroundService (reader) share the same instance.
/// </summary>
public interface ISubmissionQueue
{
    /// <summary>Enqueue a new submission job for background processing.</summary>
    ValueTask EnqueueAsync(SubmissionJob job, CancellationToken cancellationToken = default);

    /// <summary>Dequeue the next job; blocks asynchronously until one is available.</summary>
    ValueTask<SubmissionJob> DequeueAsync(CancellationToken cancellationToken = default);
}
