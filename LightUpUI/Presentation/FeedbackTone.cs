using System;

namespace LightUpUI.Presentation;

public enum FeedbackTone
{
    Info,
    Success,
    Warning,
    Error,
    Busy
}

public static class FeedbackTonePolicy
{
    public static FeedbackTone FromStatus(string? status, bool isBusy = false)
    {
        if (isBusy)
            return FeedbackTone.Busy;

        if (string.IsNullOrWhiteSpace(status))
            return FeedbackTone.Info;

        if (status.Contains("失败", StringComparison.Ordinal)
            || status.Contains("错误", StringComparison.Ordinal)
            || status.Contains("不存在", StringComparison.Ordinal)
            || status.Contains("不可用", StringComparison.Ordinal)
            || status.Contains("不能为空", StringComparison.Ordinal))
            return FeedbackTone.Error;

        if (status.Contains("请确认", StringComparison.Ordinal)
            || status.Contains("取消", StringComparison.Ordinal)
            || status.Contains("跳过", StringComparison.Ordinal)
            || status.Contains("不能", StringComparison.Ordinal)
            || status.Contains("无法", StringComparison.Ordinal))
            return FeedbackTone.Warning;

        if (status.Contains("已", StringComparison.Ordinal)
            || status.Contains("成功", StringComparison.Ordinal))
            return FeedbackTone.Success;

        return FeedbackTone.Info;
    }
}
