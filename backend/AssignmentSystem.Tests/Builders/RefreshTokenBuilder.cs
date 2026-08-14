using AssignmentSystem.Domain.Entities;
using Bogus;

namespace AssignmentSystem.Tests.Builders;

/// <summary>
/// Builds <see cref="RefreshToken"/> entities with controllable state for
/// testing token rotation, expiry, and revocation logic.
/// </summary>
public class RefreshTokenBuilder
{
    private string _token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    private DateTime _expiresAt = DateTime.UtcNow.AddDays(7);
    private bool _isRevoked = false;

    public RefreshTokenBuilder WithToken(string token)
    {
        _token = token;
        return this;
    }

    /// <summary>Creates a token that is still valid (expires in the future).</summary>
    public RefreshTokenBuilder Valid()
    {
        _isRevoked = false;
        _expiresAt = DateTime.UtcNow.AddDays(7);
        return this;
    }

    /// <summary>Creates a token that has already expired.</summary>
    public RefreshTokenBuilder Expired()
    {
        _expiresAt = DateTime.UtcNow.AddDays(-1);
        return this;
    }

    /// <summary>Creates a token that has been revoked.</summary>
    public RefreshTokenBuilder Revoked()
    {
        _isRevoked = true;
        return this;
    }

    public RefreshToken Build() => new RefreshToken
    {
        Token = _token,
        ExpiresAt = _expiresAt,
        IsRevoked = _isRevoked,
        CreatedAt = DateTime.UtcNow.AddHours(-1)
    };
}
