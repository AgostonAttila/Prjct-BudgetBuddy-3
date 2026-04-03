using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Features.Audit.GetEntityAuditHistory;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using NodaTime;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Audit;

public class GetEntityAuditHistoryHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly GetEntityAuditHistoryHandler _handler;

    public GetEntityAuditHistoryHandlerTests()
    {
        _handler = new GetEntityAuditHistoryHandler(_db);
    }

    [Fact]
    public async Task Handle_WhenNoAuditLogs_ReturnsEmpty()
    {
        var result = await _handler.Handle(new GetEntityAuditHistoryQuery("Account", "some-id"), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsHistoryForMatchingEntity()
    {
        var entityId = Guid.NewGuid().ToString();
        _db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityName = "Account", EntityId = entityId, Operation = AuditOperation.Update, CreatedAt = Instant.FromUtc(2024, 6, 15, 10, 0) });
        _db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityName = "Account", EntityId = "other-id", Operation = AuditOperation.Update, CreatedAt = Instant.FromUtc(2024, 6, 15, 11, 0) });
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetEntityAuditHistoryQuery("Account", entityId), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().EntityId.Should().Be(entityId);
    }

    [Fact]
    public async Task Handle_ReturnsLogsOrderedByDateDescending()
    {
        var entityId = Guid.NewGuid().ToString();
        _db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityName = "Account", EntityId = entityId, Operation = AuditOperation.Insert, CreatedAt = Instant.FromUtc(2024, 1, 1, 0, 0) });
        _db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityName = "Account", EntityId = entityId, Operation = AuditOperation.Update, CreatedAt = Instant.FromUtc(2024, 6, 15, 0, 0) });
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetEntityAuditHistoryQuery("Account", entityId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.First().Operation.Should().Be(AuditOperation.Update);
    }

    public void Dispose() => _db.Dispose();
}
