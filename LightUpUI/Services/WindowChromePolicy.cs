using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using FluentIcons.Common;

namespace LightUpUI.Services;

public static class WindowChromePolicy
{
    public static bool ToggleTopmost(bool currentTopmost) => !currentTopmost;

    public static bool CanStartMoveDrag(bool isInteractiveChild) => !isInteractiveChild;

    public static bool CanStartMoveDrag(Visual? source, Visual dragSurface)
    {
        for (var current = source; current is not null; current = current.GetVisualParent())
        {
            if (current is Button or TextBox)
                return false;

            if (ReferenceEquals(current, dragSurface))
                break;
        }

        return true;
    }

    public static bool ShouldHideOnDeactivated(bool isTopmost, bool staysOpenWhenDeactivated)
        => !isTopmost && !staysOpenWhenDeactivated;

    public static IconVariant GetTopmostIconVariant(bool isTopmost)
        => isTopmost ? IconVariant.Filled : IconVariant.Regular;

    public static string GetTopmostToolTip(bool isTopmost)
        => isTopmost ? "已置顶，点击取消置顶" : "置顶窗口";
}
