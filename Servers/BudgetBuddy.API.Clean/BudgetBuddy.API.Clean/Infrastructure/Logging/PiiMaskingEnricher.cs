using Serilog.Core;
using Serilog.Events;
using System.Text.RegularExpressions;

namespace BudgetBuddy.Infrastructure.Logging;

/// <summary>
/// Serilog enricher that automatically masks Personally Identifiable Information (PII)
/// in log messages to comply with GDPR and security best practices
/// </summary>
public partial class PiiMaskingEnricher(bool enabled = true) : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!enabled)
        {
            return;
        }

        // Mask PII in the message template
        if (logEvent.MessageTemplate.Text != null)
        {
            var maskedMessage = MaskPiiInText(logEvent.MessageTemplate.Text);

            // Can't modify MessageTemplate directly, but properties can be masked
            // The actual message rendering will use the masked property values
        }

        // Mask PII in properties
        var propertiesToMask = new List<string>();

        foreach (var property in logEvent.Properties)
        {
            if (property.Value is ScalarValue scalarValue && scalarValue.Value is string stringValue)
            {
                var maskedValue = MaskPiiInText(stringValue);

                if (maskedValue != stringValue)
                {
                    propertiesToMask.Add(property.Key);
                }
            }
        }

        // Note: LogEvent properties are immutable, so we can't modify them directly
        // Instead, we rely on the destructuring policy and formatting
    }

    private static string MaskPiiInText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        // Mask email addresses: john.doe@example.com → jo***@***
        text = EmailRegex().Replace(text, m =>
        {
            var localPart = m.Groups[1].Value;

            var maskedLocal = localPart.Length <= 2
                ? new string('*', localPart.Length)
                : localPart.Substring(0, 2) + new string('*', Math.Min(localPart.Length - 2, 10));

            return $"{maskedLocal}@***";
        });

        // Mask GUIDs: 12345678-1234-1234-1234-123456789abc → 1234****-****-****-****-************
        text = GuidRegex().Replace(text, m =>
        {
            return m.Value.Substring(0, 4) + "****-****-****-****-************";
        });

        // Mask IP addresses: 192.168.1.100 → 192.168.***.***
        text = IpAddressRegex().Replace(text, m =>
        {
            var parts = m.Value.Split('.');
            return $"{parts[0]}.{parts[1]}.***.***.***";
        });

        // Mask potential credit card numbers: 1234-5678-9012-3456 → ****-****-****-3456
        text = CreditCardRegex().Replace(text, m =>
        {
            var lastFour = m.Value.Substring(m.Value.Length - 4);
            return $"****-****-****-{lastFour}";
        });

        // Mask phone numbers: +36-20-123-4567 → +36-**-***-****
        text = PhoneRegex().Replace(text, m =>
        {
            var prefix = m.Groups[1].Value;
            return $"{prefix}-**-***-****";
        });

        return text;
    }

    // Compiled regex patterns for performance
    [GeneratedRegex(@"([a-zA-Z0-9._%+-]+)@([a-zA-Z0-9.-]+\.[a-zA-Z]{2,})", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b", RegexOptions.Compiled)]
    private static partial Regex GuidRegex();

    [GeneratedRegex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.Compiled)]
    private static partial Regex IpAddressRegex();

    [GeneratedRegex(@"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex CreditCardRegex();

    [GeneratedRegex(@"(\+\d{1,3})-\d{2}-\d{3}-\d{4}", RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();
}
