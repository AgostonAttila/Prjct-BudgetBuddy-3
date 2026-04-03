using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Infrastructure.Security.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Common;

/// <summary>
/// Creates a fresh in-memory AppDbContext per test — each test gets an isolated database.
/// IEncryptionService is mocked as pass-through since encryption is irrelevant for unit tests.
/// </summary>
public static class DbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var encryptionService = Substitute.For<IEncryptionService>();
        encryptionService.Encrypt(Arg.Any<string?>(), Arg.Any<string>()).Returns(x => x.ArgAt<string?>(0));
        encryptionService.Decrypt(Arg.Any<string?>(), Arg.Any<string>()).Returns(x => x.ArgAt<string?>(0));

        return new AppDbContext(options, encryptionService);
    }
}
