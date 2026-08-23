using AssignmentSystem.Application.Features.Submissions.Interfaces;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AssignmentSystem.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Long-running background service that drains the submission queue
/// and performs the heavy file I/O + database update off the HTTP thread.
///
/// Lifecycle: Singleton (BackgroundService is always singleton).
/// Uses <see cref="IServiceScopeFactory"/> to resolve scoped services
/// (ApplicationDbContext) per job, avoiding the captive-dependency problem.
/// </summary>
public sealed class SubmissionBackgroundWorker : BackgroundService
{
    private readonly ISubmissionQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubmissionBackgroundWorker> _logger;

    public SubmissionBackgroundWorker(
        ISubmissionQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<SubmissionBackgroundWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Submission background worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queue.DequeueAsync(stoppingToken);
                await ProcessJobAsync(job, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown — stop token fired, exit the loop cleanly.
                break;
            }
            catch (Exception ex)
            {
                // Log unexpected errors but keep the loop alive so the worker
                // continues processing subsequent jobs.
                _logger.LogError(ex, "Unexpected error in submission background worker loop.");
            }
        }

        _logger.LogInformation("Submission background worker stopped.");
    }

    private async Task ProcessJobAsync(
        Application.Features.Submissions.DTOs.SubmissionJob job,
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Processing submission job {SubmissionId}.", job.SubmissionId);

        // Create a fresh DI scope per job so we get a clean DbContext.
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var submission = await db.Submissions.FindAsync(
            new object[] { job.SubmissionId }, stoppingToken);

        if (submission is null)
        {
            _logger.LogWarning(
                "Submission {SubmissionId} not found in database — skipping job.",
                job.SubmissionId);
            return;
        }

        try
        {
            string? attachmentUrl = null;

            if (!string.IsNullOrEmpty(job.TempFilePath) && File.Exists(job.TempFilePath))
            {
                // ── Move file from temp → final directory ─────────────────────
                var finalDir = Path.Combine(
                    Directory.GetCurrentDirectory(), "wwwroot", "uploads", "submissions");

                if (!Directory.Exists(finalDir))
                    Directory.CreateDirectory(finalDir);

                var finalFileName = $"{Guid.NewGuid()}_{Path.GetFileName(job.OriginalFileName)}";
                var finalPath = Path.Combine(finalDir, finalFileName);

                File.Move(job.TempFilePath, finalPath, overwrite: true);

                attachmentUrl = $"{job.BaseUrl}/uploads/submissions/{finalFileName}";

                _logger.LogInformation(
                    "File for submission {SubmissionId} moved to {FinalPath}.",
                    job.SubmissionId, finalPath);
            }

            // ── Update submission record ───────────────────────────────────────
            submission.Status = SubmissionStatus.Submitted;
            submission.AttachmentFileUrl = attachmentUrl;

            await db.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "Submission {SubmissionId} processed successfully.", job.SubmissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process submission {SubmissionId}. Marking as Failed.",
                job.SubmissionId);

            // Mark as Failed so the student and teacher can see something went wrong.
            submission.Status = SubmissionStatus.Failed;

            try
            {
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx,
                    "Could not persist Failed status for submission {SubmissionId}.",
                    job.SubmissionId);
            }

            // Clean up orphaned temp file to avoid disk bloat.
            if (!string.IsNullOrEmpty(job.TempFilePath) && File.Exists(job.TempFilePath))
            {
                try { File.Delete(job.TempFilePath); }
                catch { /* best effort */ }
            }
        }
    }
}
