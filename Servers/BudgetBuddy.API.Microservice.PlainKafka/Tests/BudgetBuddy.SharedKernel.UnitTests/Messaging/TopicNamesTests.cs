using BudgetBuddy.Shared.Messages.Topics;
using FluentAssertions;
using Xunit;

namespace BudgetBuddy.SharedKernel.UnitTests.Messaging;

public class TopicNamesTests
{
    [Theory]
    [InlineData(TopicNames.TransactionsCreated, TopicNames.TransactionsDlq)]
    [InlineData(TopicNames.AccountsCreated,     TopicNames.AccountsDlq)]
    [InlineData(TopicNames.BudgetsCreated,      TopicNames.BudgetsDlq)]
    public void DlqTopics_ShouldBeDerivedFrom_ServiceName(string sourceTopic, string expectedDlq)
    {
        // DLQ topicok a service nevét tartalmazzák
        var servicePrefix = sourceTopic.Split('.')[1]; // pl. "transactions"
        expectedDlq.Should().Contain(servicePrefix);
    }

    [Theory]
    [InlineData("budgetbuddy.transactions.created")]
    [InlineData("budgetbuddy.accounts.created")]
    [InlineData("budgetbuddy.budgets.alert-triggered")]
    public void ToDlq_ShouldAppend_DlqSuffix(string topic)
    {
        var dlq = TopicNames.ToDlq(topic);

        dlq.Should().Be($"{topic}.dlq");
        dlq.Should().EndWith(".dlq");
    }

    [Fact]
    public void AllTopics_ShouldStartWith_BudgetbuddyPrefix()
    {
        var topicFields = typeof(TopicNames)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!);

        foreach (var topic in topicFields)
        {
            topic.Should().StartWith("budgetbuddy.",
                because: $"minden Kafka topic a 'budgetbuddy.' prefixszel kezdődik, de ez nem: '{topic}'");
        }
    }

    [Fact]
    public void DlqTopics_ShouldEndWith_Dlq()
    {
        var dlqFields = typeof(TopicNames)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string) && f.Name.EndsWith("Dlq"))
            .Select(f => (string)f.GetValue(null)!);

        dlqFields.Should().NotBeEmpty(because: "legalább egy DLQ topic konstansnak léteznie kell");

        foreach (var dlqTopic in dlqFields)
        {
            dlqTopic.Should().EndWith(".dlq",
                because: $"DLQ topicoknak '.dlq' végűnek kell lenniük, de ez nem: '{dlqTopic}'");
        }
    }
}
