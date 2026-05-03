using System;
using System.Linq;
using Dalamud.Bindings.ImGui;
using static SkyEye.Plugin;
using static SkyEye.Util;

namespace SkyEye;

public partial class ConfigWindow {
	private static readonly string[] regions = ["未选择", "陆行鸟", "莫古力", "猫小胖", "豆豆柴"];

	private static int getDeltaMin(string d) {
		try {
			return (int)new TimeSpan(getT(d)).TotalMinutes;
		} catch {
			return 0;
		}
	}

	private static void DrawHistory() {
		if (ImGui.Checkbox("连接史书", ref Configuration.EnableWss)) {
			WebSocket.nmalive.Clear();
			WebSocket.nmdead.Clear();
			Configuration.Save();
		}
		if (ImGui.InputText("提醒(Fate原名，包含即可，用竖线|隔开)", ref Configuration.WssNotify, 114514)) Configuration.Save();
		if (Configuration.EnableWss) {
			try {
				if (ImGui.Combo("选择大区", ref Configuration.WssRegion, regions)) {
					Configuration.Save();
					if (Configuration.WssRegion is >= 1 and <= 4) WebSocket.StartWssService();
					ImGui.EndCombo();
				}
				if (Configuration.WssRegion is < 1 or > 4) return;
				if (!WebSocket.inited) {
					WebSocket.StartWssService();
					WebSocket.inited = true;
				}
				var acts = new Action<WebSocket.NmInfo>[] {
					info => {
						switch (info.territory_id) {
							case 1:
								ImGui.PushStyleColor(ImGuiCol.TableRowBg, green);
								ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, green_alt);
								break;
							case 2:
								ImGui.PushStyleColor(ImGuiCol.TableRowBg, cyan);
								ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, cyan_alt);
								break;
							case 3:
								ImGui.PushStyleColor(ImGuiCol.TableRowBg, red);
								ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, red_alt);
								break;
							case 4:
								ImGui.PushStyleColor(ImGuiCol.TableRowBg, blue);
								ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, blue_alt);
								break;
						}
						ImGui.Text(info.territory_name_ori);
					},
					info => ImGui.Text(info.oriname), info => ImGui.Text(info.hp.ToString()), info => ImGui.Text(getDeltaMin(info.appeared_at).ToString()), info => { ImGui.Text(getDeltaMin(info.defeated_at).ToString()); }
				};
				ImGui.Text("活着的");
				var data = WebSocket.nmalive.OrderBy(a => a.territory_id).ThenBy(a => getDeltaMin(a.defeated_at)).ToArray();
				NewTable(["地点", "名称", "血量", "触发时间(min)", "击杀时间(min)"], data, acts);
				ImGui.PopStyleColor(2 * data.Length);
				ImGui.Text("已死亡");
				var data2 = WebSocket.nmdead.OrderBy(a => a.territory_id).ThenBy(a => getDeltaMin(a.defeated_at)).ToArray();
				NewTable(["地点", "名称", "血量", "触发时间(min)", "击杀时间(min)"], data2, acts);
				ImGui.PopStyleColor(2 * data2.Length);
			} catch (Exception e) {
				Log.Error(e.ToString());
			}
		} else WebSocket.StopWss();
	}
}