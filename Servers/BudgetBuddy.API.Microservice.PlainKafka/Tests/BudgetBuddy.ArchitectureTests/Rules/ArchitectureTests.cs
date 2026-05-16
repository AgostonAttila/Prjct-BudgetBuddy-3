using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace BudgetBuddy.ArchitectureTests.Rules;

public class ArchitectureTests
{
    private static readonly Assembly KernelAssembly =
        typeof(BudgetBuddy.Shared.Kernel.Exceptions.NotFoundException).Assembly;

    private static readonly Assembly MessagesAssembly =
        typeof(BudgetBuddy.Shared.Messages.Integration.IIntegrationEvent).Assembly;

    private static readonly Assembly InfraAssembly =
        typeof(BudgetBuddy.Shared.Infrastructure.HttpContextCurrentUserService).Assembly;

    // ── Dependency Rules ────────────────────────────────────────────────────────

    [Fact]
    public void SharedKernel_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(KernelAssembly)
            .Should()
            .NotHaveDependencyOn("BudgetBuddy.Shared.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Shared.Kernel nem függhet Shared.Infrastructure-tól (dependency inversion)");
    }

    [Fact]
    public void SharedMessages_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(MessagesAssembly)
            .Should()
            .NotHaveDependencyOn("BudgetBuddy.Shared.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Shared.Messages nem függhet Shared.Infrastructure-tól");
    }

    // ── Naming Conventions ──────────────────────────────────────────────────────

    [Fact]
    public void Exceptions_ShouldEndWith_Exception()
    {
        var result = Types.InAssembly(KernelAssembly)
            .That().Inherit(typeof(Exception))
            .Should().HaveNameEndingWith("Exception")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Minden exception osztálynak 'Exception'-re kell végződnie");
    }

    [Fact]
    public void IntegrationEvents_ShouldInherit_IntegrationEventBase()
    {
        var result = Types.InAssembly(MessagesAssembly)
            .That().ResideInNamespaceStartingWith("BudgetBuddy.Shared.Messages.Events")
            .And().HaveNameEndingWith("Event")
            .Should().Inherit(typeof(BudgetBuddy.Shared.Messages.Integration.IntegrationEvent))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Minden integration event az IntegrationEvent abstract base record-ból örökljön");
    }

    [Fact]
    public void BackgroundServices_InInfra_ShouldEndWith_ServiceOrConsumerOrJob()
    {
        var result = Types.InAssembly(InfraAssembly)
            .That().Inherit(typeof(Microsoft.Extensions.Hosting.BackgroundService)).And().AreNotAbstract()
            .Should().HaveNameEndingWith("Service")
            .Or().HaveNameEndingWith("Consumer")
            .Or().HaveNameEndingWith("Job")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "BackgroundService leszármazottaknak 'Service', 'Consumer' vagy 'Job' végűnek kell lenniük");
    }

    // ── Namespace Rules ─────────────────────────────────────────────────────────

    [Fact]
    public void KernelExceptions_ShouldResideIn_ExceptionsNamespace()
    {
        var result = Types.InAssembly(KernelAssembly)
            .That().Inherit(typeof(Exception))
            .Should().ResideInNamespace("BudgetBuddy.Shared.Kernel.Exceptions")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Minden kernel exception a Exceptions névtérben legyen");
    }

    [Fact]
    public void IntegrationEvents_ShouldResideIn_EventsNamespace()
    {
        var result = Types.InAssembly(MessagesAssembly)
            .That().HaveNameEndingWith("Event").And().AreNotAbstract()
            .Should().ResideInNamespaceStartingWith("BudgetBuddy.Shared.Messages.Events")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Minden integration event a Shared.Messages.Events névtérben legyen");
    }
}
