using System.Reflection;
using JasperFx;
using JasperFx.CodeGeneration;
using Wolverine.Configuration;
using Wolverine.Runtime.Handlers;

namespace BudgetBuddy.Shared.Infrastructure.Messaging;

/// <summary>
/// Wolverine handler policy applied at startup that:
///   1. Scans handler chains for <see cref="SupportedSchemaVersionsAttribute"/>.
///   2. Registers message-type → supported-versions in <see cref="SchemaVersionRegistry"/>.
///   3. Prepends <see cref="SchemaVersionMiddleware"/> to any chain whose handler carries
///      the attribute — no middleware overhead for chains without it.
///
/// Registered automatically by <see cref="BudgetBuddy.Shared.Infrastructure.Extensions.WolverineExtensions.AddWolverine"/>.
/// </summary>
public sealed class SchemaVersionPolicy : IHandlerPolicy
{
    public void Apply(
        IReadOnlyList<HandlerChain> chains,
        GenerationRules rules,
        IServiceContainer container)
    {
        var registry = container.GetInstance<SchemaVersionRegistry>();

        foreach (var chain in chains)
        {
            var attr = chain.Handlers
                .Select(h => h.HandlerType.GetCustomAttribute<SupportedSchemaVersionsAttribute>())
                .FirstOrDefault(a => a is not null);

            if (attr is null)
            {
                continue;
            }

            registry.Register(chain.MessageType, attr.SupportedVersions);

            chain.AddMiddleware(
                typeof(SchemaVersionMiddleware),
                nameof(SchemaVersionMiddleware.Before));
        }
    }
}
