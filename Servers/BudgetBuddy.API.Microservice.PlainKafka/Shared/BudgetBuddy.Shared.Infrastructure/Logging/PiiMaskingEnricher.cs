using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace BudgetBuddy.Shared.Infrastructure.Logging;

/// <summary>
/// Serilog enricher that automatically masks Personally Identifiable Information (PII)
/// in log messages to comply with GDPR and security best practices
/// </summary>
public partial class PiiMaskingEnricher : ILogEventEnricher
{
    private readonly bool _enabled;

    public PiiMaskingEnricher(bool enabled = true)
    {
        _enabled = enabled;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!_enabled)
        {
            return;
        }

        // S-05: scan all string-valued properties and replace any that contain PII.
        // LogEvent.AddOrUpdateProperty replaces an existing property with the same name,
        // which is the correct way to mutate properties in a Serilog enricher.
        foreach (var (key, value) in logEvent.Properties)
        {
            if (value is not ScalarValue { Value: string stringValue })
            {
                continue;
            }

            var masked = MaskPiiInText(stringValue);
            if (masked != stringValue)
            {
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(key, masked));
            }
        }
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
