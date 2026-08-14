using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AssignmentSystem.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace AssignmentSystem.Tests.Unit.Infrastructure.Services;

/// <summary>
/// Unit tests for <see cref="TokenService"/>.
/// No mocks needed — IConfiguration is wired up via InMemoryCollection.
/// </summary>
[Trait("Category", "Unit")]
public class TokenServiceTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static TokenService CreateService(
        string? secretKey = "super-secret-key-that-is-long-enough-for-hmac-sha256",
        string? issuer = "TestIssuer",
        string? audience = "TestAudience",
        string? expirationMinutes = "60",
        string? refreshExpirationDays = "7")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt__SecretKey"] = secretKey,
                ["Jwt__Issuer"] = issuer,
                ["Jwt__Audience"] = audience,
                ["Jwt__AccessTokenExpirationMinutes"] = expirationMinutes,
                ["Jwt__RefreshTokenExpirationDays"] = refreshExpirationDays,
            })
            .Build();

        return new TokenService(config);
    }

    private static JwtSecurityToken DecodeToken(string rawToken)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.ReadJwtToken(rawToken);
    }

    // ── GenerateAccessToken ────────────────────────────────────────────────────

    [Fact]
    public void GenerateAccessToken_ReturnsNonEmptyString()
    {
        var sut = CreateService();
        var userId = Guid.NewGuid();

        var token = sut.GenerateAccessToken(userId, "user@test.com", "Student");

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwtFormat()
    {
        // A JWT must have exactly 3 dot-separated segments: header.payload.signature
        var sut = CreateService();

        var token = sut.GenerateAccessToken(Guid.NewGuid(), "user@test.com", "Student");

        token.Split('.').Should().HaveCount(3,
            because: "a valid JWT has header, payload, and signature separated by dots");
    }

    [Fact]
    public void GenerateAccessToken_ContainsCorrectUserIdInSubClaim()
    {
        var sut = CreateService();
        var userId = Guid.NewGuid();

        var rawToken = sut.GenerateAccessToken(userId, "user@test.com", "Student");
        var jwt = DecodeToken(rawToken);

        var subClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        subClaim.Should().NotBeNull();
        subClaim!.Value.Should().Be(userId.ToString());
    }

    [Fact]
    public void GenerateAccessToken_ContainsCorrectEmailClaim()
    {
        var sut = CreateService();
        const string expectedEmail = "jane.doe@example.com";

        var rawToken = sut.GenerateAccessToken(Guid.NewGuid(), expectedEmail, "Teacher");
        var jwt = DecodeToken(rawToken);

        var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        emailClaim.Should().NotBeNull();
        emailClaim!.Value.Should().Be(expectedEmail);
    }

    [Fact]
    public void GenerateAccessToken_ContainsCorrectRoleClaim()
    {
        var sut = CreateService();
        const string expectedRole = "Admin";

        var rawToken = sut.GenerateAccessToken(Guid.NewGuid(), "admin@test.com", expectedRole);
        var jwt = DecodeToken(rawToken);

        // ASP.NET Identity writes role into ClaimTypes.Role which maps to a long URI
        var roleClaim = jwt.Claims.FirstOrDefault(c =>
            c.Type == ClaimTypes.Role ||
            c.Type == "role");

        roleClaim.Should().NotBeNull(because: "every access token must carry the user's role");
        roleClaim!.Value.Should().Be(expectedRole);
    }

    [Fact]
    public void GenerateAccessToken_TokenExpiresAtConfiguredMinutes()
    {
        const int expirationMinutes = 30;
        var sut = CreateService(expirationMinutes: expirationMinutes.ToString());
        var before = DateTime.UtcNow;

        var rawToken = sut.GenerateAccessToken(Guid.NewGuid(), "user@test.com", "Student");
        var jwt = DecodeToken(rawToken);

        var expectedExpiry = before.AddMinutes(expirationMinutes);
        jwt.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void GenerateAccessToken_WhenSecretKeyNotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange: config with NO secret key entry
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt__Issuer"] = "TestIssuer",
            })
            .Build();
        var sut = new TokenService(config);

        // Act & Assert
        var act = () => sut.GenerateAccessToken(Guid.NewGuid(), "user@test.com", "Student");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*JWT SecretKey*");
    }

    // ── GenerateRefreshToken ───────────────────────────────────────────────────

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        var sut = CreateService();

        var token = sut.GenerateRefreshToken();

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsDifferentValueOnEachCall()
    {
        var sut = CreateService();

        var token1 = sut.GenerateRefreshToken();
        var token2 = sut.GenerateRefreshToken();

        token1.Should().NotBe(token2,
            because: "each refresh token must be cryptographically unique");
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsValidBase64String()
    {
        var sut = CreateService();

        var token = sut.GenerateRefreshToken();

        // If this throws, the token is not valid Base64
        var act = () => Convert.FromBase64String(token);
        act.Should().NotThrow(because: "refresh tokens are Base64-encoded random bytes");
    }
}
