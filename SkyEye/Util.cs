using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SkyEye.SkyEye;

internal static class Util {
    private const ImGuiTableFlags ImGuiTableFlag = ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg;

    internal static readonly Vector4 green = new(0, 1, 0, 0.4f);
    internal static readonly Vector4 green_alt = new(0, 1, 0, 0.3f);
    internal static readonly Vector4 red = new(1, 0, 0, 0.4f);
    internal static readonly Vector4 red_alt = new(1, 0, 0, 0.3f);
    internal static readonly Vector4 blue = new(0, 0, 1, 0.4f);
    internal static readonly Vector4 blue_alt = new(0, 0, 1, 0.3f);
    internal static readonly Vector4 cyan = new(0.3f, 0.3f, 1, 0.4f);
    internal static readonly Vector4 cyan_alt = new(0.3f, 0.3f, 1, 0.3f);
    internal static readonly Vector4 black = new(0, 0, 0, 0.8f);
    internal static readonly Vector4 black_alt = new(0.2f, 0.2f, 0.2f, 1);
    internal static readonly Vector4 white = new(1, 1, 1, 0.4f);
    internal static readonly Vector4 white_alt = new(1, 1, 1, 0.3f);

    internal static void DrawText(this ImDrawListPtr drawList, Vector2 pos, string text, uint col, bool stroke, bool centerAlignX = true, uint strokecol = 4278190080u) {
        if (centerAlignX) pos -= new Vector2(ImGui.CalcTextSize(text).X, 0f) / 2f;
        if (stroke) {
            drawList.AddText(pos + new Vector2(-1f, -1f), strokecol, text);
            drawList.AddText(pos + new Vector2(-1f, 1f), strokecol, text);
            drawList.AddText(pos + new Vector2(1f, -1f), strokecol, text);
            drawList.AddText(pos + new Vector2(1f, 1f), strokecol, text);
        }
        drawList.AddText(pos, col, text);
    }

    internal static void DrawMapDot(this ImDrawListPtr drawList, Vector2 pos, uint fgcolor, uint bgcolor) {
        drawList.AddCircleFilled(pos, 4f, fgcolor);
        drawList.AddCircle(pos, 2f, bgcolor, 0, 2f);
    }

    internal static Vector2 ToVector2(Vector3 v) => new(v.X, v.Z);

    internal static Vector3 ToVector3(Vector2 v) => new(v.X, 0f, v.Y);

    private static float MapToWorld(float value, int scale, float offset) => -offset * (scale / 100.0f) + 50.0f * (value - offset) * (scale / 100.0f);

    internal static Vector2 MapToWorld(Vector2 coordinates, int SizeFactor, float OffsetX, float OffsetY) =>
        (new Vector2(MapToWorld(coordinates.X, SizeFactor, OffsetX), MapToWorld(coordinates.Y, SizeFactor, OffsetY))
         - new Vector2(1024f, 1024f)) / SizeFactor * 100F;


    internal static void NewTable<T>(string[] header, T[] data, Action<T>[] acts) {
        if (ImGui.BeginTable("Table", acts.Length, ImGuiTableFlag)) {
            foreach (var item in header) {
                if (item == "") ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 24);
                else if (item.Contains("序号")) ImGui.TableSetupColumn(item, ImGuiTableColumnFlags.WidthFixed, 96);
                else ImGui.TableSetupColumn(item);
            }
            ImGui.TableHeadersRow();
            foreach (var res in data) {
                ImGui.TableNextRow();
                for (var i = 0; i < acts.Length; i++) {
                    ImGui.TableSetColumnIndex(i);
                    acts[i](res);
                }
            }
            ImGui.EndTable();
        }
    }
}