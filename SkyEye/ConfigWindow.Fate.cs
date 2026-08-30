using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Utility;
using SkyEye.Data;
using static SkyEye.Data.EorzeaWeather;
using static SkyEye.Data.PData;
using static SkyEye.Data.PData.EurekaWeather;
using static SkyEye.Data.Territory;
using static SkyEye.Plugin;
using static SkyEye.Util;

namespace SkyEye;

public partial class ConfigWindow {
	private static void DrawFate() {
		if (ImGui.SliderInt("下几个天气", ref Configuration.NextWeatherCount, 10, 50)) Configuration.Save();
		var data = new Dictionary<Territory, Dictionary<string, EurekaWeather>> {
			[Anemos] = GetAllWeathers([.. Weathers[Anemos]])
				.ToDictionary(item => item.Time.ToString(UiBuilder.timeFormat), item => item.Weather),
			[Pagos] = GetAllWeathers(Weathers[Pagos])
				.ToDictionary(item => item.Time.ToString(UiBuilder.timeFormat), item => item.Weather),
			[Pyros] = GetAllWeathers(Weathers[Pyros])
				.ToDictionary(item => item.Time.ToString(UiBuilder.timeFormat), item => item.Weather),
			[Hydatos] = GetAllWeathers(Weathers[Hydatos])
				.ToDictionary(item => item.Time.ToString(UiBuilder.timeFormat), item => item.Weather)
		};
		var allUniqueTimes = data.Values.Aggregate(new List<string>(), (current, x) => [.. current, .. x.Keys])
			.Distinct().ToList();
		var tablen = Math.Min(allUniqueTimes.Count, Configuration.NextWeatherCount);
		if (ImGui.BeginTable("EurekaWeatherTable", 1 + tablen, ImGuiTableFlags.Borders)) {
			ImGui.TableSetupColumn("区域", ImGuiTableColumnFlags.WidthFixed);
			for (var index = 0; index < tablen; index++)
				ImGui.TableSetupColumn(index == 0 ? "当前" : allUniqueTimes[index], ImGuiTableColumnFlags.WidthFixed);
			ImGui.TableHeadersRow();
			foreach (var t in data) {
				ImGui.TableNextRow();
				ImGui.TableSetColumnIndex(0);
				ImGui.Text(t.Key.ToFriendlyString());
				var iter = 0;
				foreach (var time in allUniqueTimes) {
					ImGui.TableNextColumn();
					if (t.Value.TryGetValue(time, out var weather)) {
						ImGui.PushStyleColor(ImGuiCol.Text, (Vector4)(weather switch {
							Gales => new(0, 1, 0, 1),
							Blizzards => new(0, 1, 1, 1),
							Fog => new(1, 1, 1, 1),
							HeatWaves when t.Key == Pyros => new(1, 0, 0, 1),
							Thunder when t.Key == Pyros => new(1, 0, 1, 1),
							Snow when t.Key == Hydatos => new(.5f, .5f, 1, 1),
							_ => new(1, 1, 1, .2f)
						}));
						ImGui.Text(weather.ToFriendlyString());
						ImGui.PopStyleColor();
					}
					if (++iter == Configuration.NextWeatherCount) break;
				}
			}
			ImGui.EndTable();
		}
		var acts = new Action<EurekaFate>[] {
			i => ImGui.Text(i.Lv), i => {
				if (ImGui.Button(i.Name)) {
					ImGui.SetClipboardText(i.Name);
					NotificationManager.AddNotification(new() {
						Title = "已复制",
						Content = i.Name
					});
					if (InEureka()) {
						SetFlagAndMove(i.FatePosition);
					}
				}
			},
			i => {
				if (i.Trigger.IsNullOrEmpty()) ImGui.Text("");
				else if (ImGui.Button(i.Trigger)) {
					ImGui.SetClipboardText(i.Trigger);
					NotificationManager.AddNotification(new() {
						Title = "已复制",
						Content = i.Trigger
					});
				}
			},
			i => ImGui.Text(i.TriggerLv), i => ImGui.Text(i.SpawnRequiredWeather.ToFriendlyString()), i => ImGui.Text(i.SpawnByRequiredNight ? "是" : "")
		};
		var orderact = new[] {
			() => {
				ImGui.Text("常风之地");
				NewTable(["等级", "任务名", "触发怪", "触发怪等级", "天气", "夜晚"], XFates[Anemos], acts);
			},
			() => {
				ImGui.Text("恒冰之地");
				NewTable(["等级", "任务名", "触发怪", "触发怪等级", "天气", "夜晚"], XFates[Pagos], acts);
			},
			() => {
				ImGui.Text("涌火之地");
				NewTable(["等级", "任务名", "触发怪", "触发怪等级", "天气", "夜晚"], XFates[Pyros], acts);
			},
			() => {
				ImGui.Text("丰水之地");
				NewTable(["等级", "任务名", "触发怪", "触发怪等级", "天气", "夜晚"], XFates[Hydatos], acts);
			}
		};
		switch ((Territory)ClientState.TerritoryType) {
			case Anemos:
				ImGui.PushStyleColor(ImGuiCol.TableRowBg, white);
				ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, white_alt);
				orderact[0]();
				ImGui.PopStyleColor(2);
				ImGui.PushStyleColor(ImGuiCol.TableRowBg, black);
				ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, black_alt);
				orderact[1]();
				orderact[2]();
				orderact[3]();
				break;
			case Pagos:
				ImGui.PushStyleColor(ImGuiCol.TableRowBg, white);
				ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, white_alt);
				orderact[1]();
				ImGui.PopStyleColor(2);
				ImGui.PushStyleColor(ImGuiCol.TableRowBg, black);
				ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, black_alt);
				orderact[0]();
				orderact[2]();
				orderact[3]();
				break;
			case Pyros:
				ImGui.PushStyleColor(ImGuiCol.TableRowBg, white);
				ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, white_alt);
				orderact[2]();
				ImGui.PopStyleColor(2);
				ImGui.PushStyleColor(ImGuiCol.TableRowBg, black);
				ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, black_alt);
				orderact[0]();
				orderact[1]();
				orderact[3]();
				break;
			case Hydatos:
				ImGui.PushStyleColor(ImGuiCol.TableRowBg, white);
				ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, white_alt);
				orderact[3]();
				ImGui.PopStyleColor(2);
				ImGui.PushStyleColor(ImGuiCol.TableRowBg, black);
				ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, black_alt);
				orderact[0]();
				orderact[1]();
				orderact[2]();
				break;
			default:
				ImGui.PushStyleColor(ImGuiCol.TableRowBg, black);
				ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, black_alt);
				orderact[0]();
				orderact[1]();
				orderact[2]();
				orderact[3]();
				break;
		}
		ImGui.PopStyleColor(2);
	}
}