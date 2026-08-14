using AssignmentSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AssignmentSystem.Tests.Helpers;

/// <summary>
/// Creates a fully-mocked UserManager&lt;User&gt; so test classes don't need
/// to wire up all 9 constructor arguments themselves.
/// </summary>
public static class MockUserManagerFactory
{
    public static Mock<UserManager<User>> Create()
    {
        var store = new Mock<IUserStore<User>>();

        // UserManager<T> needs these optional services — pass nulls via Options/Logger
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions());

        var passwordHasher = new Mock<IPasswordHasher<User>>();
        var userValidators = new List<IUserValidator<User>>();
        var passwordValidators = new List<IPasswordValidator<User>>();
        var lookupNormalizer = new Mock<ILookupNormalizer>();
        var identityErrorDescriber = new Mock<IdentityErrorDescriber>();
        var services = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<UserManager<User>>>();

        var manager = new Mock<UserManager<User>>(
            store.Object,
            options.Object,
            passwordHasher.Object,
            userValidators,
            passwordValidators,
            lookupNormalizer.Object,
            identityErrorDescriber.Object,
            services.Object,
            logger.Object);

        // Make virtual members mockable
        manager.CallBase = false;

        return manager;
    }
}
