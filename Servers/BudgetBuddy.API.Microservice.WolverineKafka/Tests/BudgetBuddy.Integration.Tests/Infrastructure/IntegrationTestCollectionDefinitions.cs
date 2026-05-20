using Xunit;

namespace BudgetBuddy.Integration.Tests.Infrastructure;

[CollectionDefinition(nameof(InvestmentsApiCollection))]
public class InvestmentsApiCollection : ICollectionFixture<InvestmentsApiFactory>;

[CollectionDefinition(nameof(TransactionsApiCollection))]
public class TransactionsApiCollection : ICollectionFixture<TransactionsApiFactory>;

[CollectionDefinition(nameof(AnalyticsApiCollection))]
public class AnalyticsApiCollection : ICollectionFixture<AnalyticsApiFactory>;

[CollectionDefinition(nameof(E2ETestCollection))]
public class E2ETestCollection : ICollectionFixture<E2ETestFixture>;

/// <summary>
/// Collection for Wolverine Kafka transport tests.
/// One KafkaContainer is shared across all tests in this collection.
/// </summary>
[CollectionDefinition(nameof(KafkaMessagingCollection))]
public class KafkaMessagingCollection : ICollectionFixture<KafkaFixture>;
