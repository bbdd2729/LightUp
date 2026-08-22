using System;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace LightUpUI.Services;

public sealed class WindowsStartupRegistrationService : IStartupRegistrationService
{
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "LightUp";
    private readonly Func<string?> _applicationPath;

    public WindowsStartupRegistrationService(Func<string?>? applicationPath = null)
        => _applicationPath = applicationPath ?? GetApplicationPath;

    public string? Apply(bool enabled)
    {
        if (!OperatingSystem.IsWindows())
            return "开机启动目前仅支持 Windows。";

        return ApplyOnWindows(enabled);
    }

    [SupportedOSPlatform("windows")]
    private string? ApplyOnWindows(bool enabled)
    {
        try
        {
            using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (runKey is null)
                return "无法访问当前用户的启动项。";

            if (!enabled)
            {
                runKey.DeleteValue(ValueName, throwOnMissingValue: false);
                return null;
            }

            var applicationPath = _applicationPath();
            if (string.IsNullOrWhiteSpace(applicationPath) || !File.Exists(applicationPath))
                return "未找到 LightUp 可执行文件，无法创建开机启动项。";

            runKey.SetValue(ValueName, Quote(applicationPath), RegistryValueKind.String);
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return "没有权限修改当前用户的启动项。";
        }
        catch (Exception exception)
        {
            return $"更新开机启动项失败：{exception.Message}";
        }
    }

    private static string? GetApplicationPath()
    {
        var publishedExecutable = Path.Combine(AppContext.BaseDirectory, "LightUpUI.exe");
        if (File.Exists(publishedExecutable))
            return publishedExecutable;

        return Environment.ProcessPath;
    }

    private static string Quote(string path) => $"\"{path}\"";
}
