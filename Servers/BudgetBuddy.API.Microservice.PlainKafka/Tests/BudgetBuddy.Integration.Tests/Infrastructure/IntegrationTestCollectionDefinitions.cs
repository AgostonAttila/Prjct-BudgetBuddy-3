using Xunit;

namespace BudgetBuddy.Integration.Tests.Infrastructure;

[CollectionDefinition(nameof(InvestmentsApiCollection))]
public class InvestmentsApiCollection : ICollectionFixture<InvestmentsApiFactory>;

[CollectionDefinition(nameof(TransactionsApiCollection))]
public class TransactionsApiCollection : ICollectionFixture<TransactionsApiFactory>;

[CollectionDefinition(nameof(AnalyticsApiCollection))]
public class AnalyticsApiCollection : ICollectionFixture<AnalyticsApiFactory>;
