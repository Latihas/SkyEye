using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SkyEye.SkyEye;

internal static class ImguiUtil {
    public static void DrawText(this ImDrawListPtr drawList, Vector2 pos, string text, uint col, bool stroke, bool centerAlignX = true, uint strokecol = 4278190080u) {
        if (centerAlignX) pos -= new Vector2(ImGui.CalcTextSize(text).X, 0f) / 2f;
        if (stroke) {
            drawList.AddText(pos + new Vector2(-1f, -1f), strokecol, text);
            drawList.AddText(pos + new Vector2(-1f, 1f), strokecol, text);
            drawList.AddText(pos + new Vector2(1f, -1f), strokecol, text);
            drawList.AddText(pos + new Vector2(1f, 1f), strokecol, text);
        }
        drawList.AddText(pos, col, text);
    }

    public static void DrawMapDot(this ImDrawListPtr drawList, Vector2 pos, uint fgcolor, uint bgcolor) {
        drawList.AddCircleFilled(pos, 4f, fgcolor);
        drawList.AddCircle(pos, 2f, bgcolor, 0, Configuration.Overlay2DDotStroke);
    }
}