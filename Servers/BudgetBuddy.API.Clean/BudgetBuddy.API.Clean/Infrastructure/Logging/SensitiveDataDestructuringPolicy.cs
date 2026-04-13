using Serilog.Core;
using Serilog.Events;
using System.Reflection;

namespace BudgetBuddy.Infrastructure.Logging;

/// <summary>
/// Serilog destructuring policy that automatically masks properties marked with [SensitiveData]
/// </summary>
public class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue? result)
    {
        var type = value.GetType();

        // Only process custom types (not primitives or system types)
        if (type.IsPrimitive || type.Namespace?.StartsWith("System") == true)
        {
            result = null;
            return false;
        }

        var properties = new List<LogEventProperty>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var sensitiveAttr = prop.GetCustomAttribute<SensitiveDataAttribute>();
            var propValue = prop.GetValue(value);

            if (sensitiveAttr != null && propValue is string stringValue)
            {
                // Mask sensitive property
                var maskedValue = MaskValue(stringValue, sensitiveAttr.Strategy);
                properties.Add(new LogEventProperty(prop.Name, new ScalarValue(maskedValue)));
            }
            else if (propValue != null)
            {
                // Normal property
                properties.Add(new LogEventProperty(prop.Name, propertyValueFactory.CreatePropertyValue(propValue, true)));
            }
            else
            {
                properties.Add(new LogEventProperty(prop.Name, new ScalarValue(null)));
            }
        }

        result = new StructureValue(properties);
        return true;
    }

    private static string MaskValue(string value, MaskingStrategy strategy)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return strategy switch
        {
            MaskingStrategy.Full => new string('*', Math.Min(value.Length, 8)),

            MaskingStrategy.Partial => value.Length <= 2
                ? new string('*', value.Length)
                : value.Substring(0, 2) + new string('*', Math.Min(value.Length - 2, 10)),

            MaskingStrategy.PartialBoth => value.Length <= 4
                ? new string('*', value.Length)
                : value.Substring(0, 2) + new string('*', value.Length - 4) + value.Substring(value.Length - 2),

            MaskingStrategy.Email => MaskEmail(value),

            _ => new string('*', Math.Min(value.Length, 8))
        };
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
        {
            return new string('*', Math.Min(email.Length, 8));
        }

        var localPart = email.Substring(0, atIndex);
        var domain = email.Substring(atIndex);

        var maskedLocal = localPart.Length <= 2
            ? new string('*', localPart.Length)
            : localPart.Substring(0, 2) + new string('*', Math.Min(localPart.Length - 2, 10));

        return maskedLocal + domain;
    }
}
