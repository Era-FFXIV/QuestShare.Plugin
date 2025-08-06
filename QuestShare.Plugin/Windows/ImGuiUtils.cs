using Dalamud.Interface.Utility;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace QuestShare.Windows
{
    /// <summary>
    /// Contains utility functions for ImGui.
    /// </summary>
    internal static class ImGuiUtils
    {
        public static Vector2 WindowSize = ImGuiHelpers.ScaledVector2(700, 600);
        public static Vector2 IconSize = ImGuiHelpers.ScaledVector2(40, 40);
        public static Vector2 SmallIconSize = ImGuiHelpers.ScaledVector2(20, 20);
        public static Vector2 LineIconSize = new(ImGui.GetFrameHeight(), 0);
        public static Vector2 ItemSpacing = ImGui.GetStyle().ItemSpacing;
        public static Vector2 FramePadding = ImGui.GetStyle().FramePadding;
        public static Vector2 IconButtonSize = new(ImGui.GetFrameHeight(), 0);
        public static float SelectorWidth = Math.Max(ImGui.GetWindowSize().X * 0.15f, 150 * Scale);
        public static Vector2 HorizontalSpace = Vector2.Zero;
        public static float TextHeight = ImGui.GetTextLineHeight();
        public static float TextHeightSpacing = ImGui.GetTextLineHeightWithSpacing();
        public static float Scale = ImGuiHelpers.GlobalScale;
        public static float TextWidth(string text) => ImGui.CalcTextSize(text).X + ItemSpacing.X;
    }
}
