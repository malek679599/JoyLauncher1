using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JoyLauncher.App.Services;

internal sealed record AndroidCrashLogSummary(string Text, bool CrashFound);

internal static class AndroidCrashLogParser
{
    private static readonly string[] CrashMarkers =
    [
        "FATAL EXCEPTION",
        "Fatal signal",
        "am_crash",
        "ANR in ",
        "Force finishing activity",
        "has died"
    ];

    public static AndroidCrashLogSummary Extract(
        string? logcatOutput,
        string packageName,
        int maxCharacters = 8_000)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);
        if (maxCharacters < 64)
            throw new ArgumentOutOfRangeException(nameof(maxCharacters), "The summary limit must be at least 64 characters.");

        var lines = (logcatOutput ?? string.Empty)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var packagePattern = $@"(?<![A-Za-z0-9_.]){Regex.Escape(packageName)}(?![A-Za-z0-9_.])";
        var blocks = new List<string>();

        for (var index = 0; index < lines.Length; index++)
        {
            if (!IsCrashMarker(lines[index]))
                continue;

            var end = Math.Min(lines.Length, index + 120);
            for (var next = index + 1; next < end; next++)
            {
                if (IsCrashMarker(lines[next]))
                {
                    end = next;
                    break;
                }
            }

            var block = string.Join(Environment.NewLine, lines[index..end]);
            if (Regex.IsMatch(
                block,
                packagePattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                blocks.Add(block);
                index = end - 1;
            }
        }

        var latestBlocks = blocks.TakeLast(3).ToArray();
        if (latestBlocks.Length == 0)
        {
            return new AndroidCrashLogSummary(
                $"No recent Android crash entry was found for {packageName} in the captured logcat window.",
                CrashFound: false);
        }

        var summary = string.Join(
            Environment.NewLine + Environment.NewLine,
            new[]
            {
                $"Android crash summary for {packageName} · {latestBlocks.Length} matching event(s)",
                "Only crash blocks naming this package are included.",
                string.Join(Environment.NewLine + Environment.NewLine, latestBlocks)
            });

        const string truncatedMarker = "\n[summary truncated]";
        if (summary.Length > maxCharacters)
            summary = summary[..(maxCharacters - truncatedMarker.Length)] + truncatedMarker;

        return new AndroidCrashLogSummary(summary, CrashFound: true);
    }

    private static bool IsCrashMarker(string line) =>
        CrashMarkers.Any(marker => line.Contains(marker, StringComparison.OrdinalIgnoreCase));
}
