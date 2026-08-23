namespace AssignmentSystem.Application.Features.Submissions.DTOs;

/// <summary>
/// Payload that is enqueued when a student submits an assignment.
/// The file is pre-saved to a temp path on disk so we don't hold
/// large byte arrays in memory inside the Channel.
/// </summary>
public record SubmissionJob(
    Guid SubmissionId,

    /// <summary>
    /// Absolute path to the temporarily saved file (e.g. wwwroot/uploads/temp/&lt;guid&gt;.pdf).
    /// Null when the student submitted only text (no attachment).
    /// </summary>
    string? TempFilePath,

    /// <summary>Original filename supplied by the student (for the final file name).</summary>
    string? OriginalFileName,

    /// <summary>Base URL of the server, used to build the public attachment URL.</summary>
    string BaseUrl);
