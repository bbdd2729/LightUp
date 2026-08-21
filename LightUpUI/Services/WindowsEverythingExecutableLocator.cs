using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace LightUpUI.Services;

public sealed class WindowsEverythingExecutableLocator : IEverythingExecutableLocator
{
    public string? FindExecutablePath()
    {
        if (!OperatingSystem.IsWindows())
            return null;

        foreach (var candidate in GetCandidates())
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    [SupportedOSPlatform("windows")]
    private static IEnumerable<string> GetCandidates()
    {
        foreach (var path in GetRunningProcessPaths())
            yield return path;

        foreach (var path in GetRegistryPaths())
            yield return path;

        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Everything",
            "Everything.exe");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Everything",
            "Everything.exe");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "scoop",
            "apps",
            "everything",
            "current",
            "Everything.exe");

        var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            yield return Path.Combine(directory.Trim(), "Everything.exe");
    }

    private static IEnumerable<string> GetRunningProcessPaths()
    {
        Process[] processes;
        try
        {
            processes = Process.GetProcessesByName("Everything");
        }
        catch
        {
            yield break;
        }

        foreach (var process in processes)
        {
            using (process)
            {
                string? path = null;
                try
                {
                    path = process.MainModule?.FileName;
                }
                catch
                {
                    // Reading the module path can be denied for an elevated process.
                }

                if (!string.IsNullOrWhiteSpace(path))
                    yield return path;
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static IEnumerable<string> GetRegistryPaths()
    {
        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
            using var uninstallKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstallKey is null)
                continue;

            foreach (var keyName in uninstallKey.GetSubKeyNames())
            {
                using var key = uninstallKey.OpenSubKey(keyName);
                if (key?.GetValue("DisplayName") is not string displayName
                    || !displayName.Contains("Everything", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (key.GetValue("InstallLocation") is string installLocation
                    && !string.IsNullOrWhiteSpace(installLocation))
                {
                    yield return Path.Combine(installLocation, "Everything.exe");
                }

                foreach (var valueName in new[] { "DisplayIcon", "UninstallString" })
                {
                    if (key.GetValue(valueName) is not string command)
                        continue;

                    var executable = ExtractExecutablePath(command);
                    if (!string.IsNullOrWhiteSpace(executable))
                        yield return Path.Combine(Path.GetDirectoryName(executable) ?? string.Empty, "Everything.exe");
                }
            }
        }
    }

    private static string? ExtractExecutablePath(string command)
    {
        var trimmed = command.Trim();
        if (trimmed.Length == 0)
            return null;

        if (trimmed[0] == '\"')
        {
            var closingQuote = trimmed.IndexOf('\"', 1);
            return closingQuote > 1 ? trimmed[1..closingQuote] : null;
        }

        var separator = trimmed.IndexOf(' ');
        return separator < 0 ? trimmed : trimmed[..separator];
    }
}
