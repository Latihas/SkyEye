using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Utility;

namespace SkyEye.SkyEye;

internal static class Util {
    internal const ImGuiTableFlags ImGuiTableFlag = ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp;

    internal static readonly Vector4 green = new(0, 1, 0, 0.4f),
        green_alt = new(0, 1, 0, 0.3f),
        red = new(1, 0, 0, 0.4f),
        red_alt = new(1, 0, 0, 0.3f),
        blue = new(0, 0, 1, 0.4f),
        blue_alt = new(0, 0, 1, 0.3f),
        cyan = new(0.3f, 0.3f, 1, 0.4f),
        cyan_alt = new(0.3f, 0.3f, 1, 0.3f),
        black = new(0, 0, 0, 0.8f),
        black_alt = new(0.2f, 0.2f, 0.2f, 1),
        white = new(1, 1, 1, 0.4f),
        white_alt = new(1, 1, 1, 0.3f);

    internal static void DrawText(this ImDrawListPtr drawList, Vector2 pos, string text, uint col, uint strokecol = 0xFF000000) {
        pos -= new Vector2(ImGui.CalcTextSize(text).X, 0f) / 2f;
        drawList.AddText(pos + new Vector2(-1f, -1f), strokecol, text);
        drawList.AddText(pos + new Vector2(-1f, 1f), strokecol, text);
        drawList.AddText(pos + new Vector2(1f, -1f), strokecol, text);
        drawList.AddText(pos + new Vector2(1f, 1f), strokecol, text);
        drawList.AddText(pos, col, text);
    }

    internal static void DrawMapDot(this ImDrawListPtr drawList, Vector2 pos, uint fgcolor, uint bgcolor, float radius = 4f) {
        drawList.AddCircleFilled(pos, radius, fgcolor);
        drawList.AddCircle(pos, radius / 2, bgcolor, 0, radius / 2);
    }

    internal static Vector2 ToVector2(Vector3 v) => new(v.X, v.Z);

    internal static Vector3 ToVector3(Vector2 v) => new(v.X, 0f, v.Y);

    private static float MapToWorld(float value, int scale, float offset) => -offset * (scale / 100.0f) + 50.0f * (value - offset) * (scale / 100.0f);

    internal static Vector2 MapToWorld(Vector2 coordinates, int SizeFactor, float OffsetX, float OffsetY) =>
        (new Vector2(MapToWorld(coordinates.X, SizeFactor, OffsetX), MapToWorld(coordinates.Y, SizeFactor, OffsetY))
         - new Vector2(1024f, 1024f)) / SizeFactor * 100F;

    internal static long getT(string d) {
        if (DateTimeOffset.TryParse(d, CultureInfo.InvariantCulture, out var dateTime))
            return DateTime.Now.Ticks - dateTime.Ticks;
        try {
            var dateTime1 = DateTime.ParseExact(d, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            return DateTime.Now.Ticks - dateTime1.Ticks;
        }
        catch {
            try {
                var dateTime2 = DateTime.ParseExact(d, "yyyy-MM-dd H:mm:ss", CultureInfo.InvariantCulture);
                return DateTime.Now.Ticks - dateTime2.Ticks;
            }
            catch {
                return 0;
            }
        }
    }

    internal static void NewTable<T>(string[] header, T[] data, Action<T>[] acts, Func<T, string>[]? filter = null, string? filterTag = null) {
        var datax = (data.Clone() as T[])!;
        if (ImGui.BeginTable("Table", acts.Length, ImGuiTableFlag)) {
            foreach (var item in header) {
                if (item == "") ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 24);
                else if (item.Contains("序号")) ImGui.TableSetupColumn(item, ImGuiTableColumnFlags.WidthFixed, 96);
                else ImGui.TableSetupColumn(item, ImGuiTableColumnFlags.WidthStretch);
            }
            ImGui.TableHeadersRow();
            if (filter != null && filterTag != null) {
                var filterdata = new string[acts.Length];
                for (var i = 0; i < filterdata.Length; i++) filterdata[i] = "";
                ImGui.TableNextRow();
                for (var i = 0; i < acts.Length; i++) {
                    ImGui.TableSetColumnIndex(i);
                    if (header[i].IsNullOrEmpty()) continue;
                    if (ImGui.InputText($"##Filter{i}", ref filterdata[i])) {
                        for (var j = 0; j < acts.Length; j++) {
                            if (header[j].IsNullOrEmpty()) continue;
                            datax = datax.Where(x => filter[j](x).Contains(filterdata[j])).ToArray();
                        }
                    }
                }
            }
            foreach (var res in datax) {
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