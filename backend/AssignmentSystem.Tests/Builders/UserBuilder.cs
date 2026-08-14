using AssignmentSystem.Domain.Entities;
using Bogus;

namespace AssignmentSystem.Tests.Builders;

/// <summary>
/// Builds realistic <see cref="User"/> instances for unit tests.
/// Uses Bogus to generate non-trivial fake data so tests don't rely on
/// magic hardcoded strings.
/// </summary>
public class UserBuilder
{
    private readonly Faker<User> _faker;
    private readonly List<RefreshToken> _refreshTokens = [];

    public UserBuilder()
    {
        _faker = new Faker<User>()
            .RuleFor(u => u.Id, _ => Guid.NewGuid())
            .RuleFor(u => u.Name, f => f.Name.FullName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.UserName, (_, u) => u.Email)
            .RuleFor(u => u.EmailConfirmed, _ => true)
            .RuleFor(u => u.CreatedAt, f => f.Date.Past(1))
            .RuleFor(u => u.UpdatedAt, (_, u) => u.CreatedAt);
    }

    /// <summary>Attaches a specific refresh token string to the user.</summary>
    public UserBuilder WithRefreshToken(RefreshToken token)
    {
        _refreshTokens.Add(token);
        return this;
    }

    public User Build()
    {
        var user = _faker.Generate();
        foreach (var t in _refreshTokens)
            user.RefreshTokens.Add(t);
        return user;
    }

    /// <summary>Generates a list of users (all unique).</summary>
    public List<User> BuildMany(int count) =>
        Enumerable.Range(0, count).Select(_ => Build()).ToList();
}
