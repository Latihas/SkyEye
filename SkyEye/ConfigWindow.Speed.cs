using System;
using System.Linq;
using System.Text;
using Dalamud.Bindings.ImGui;
using Dalamud.Utility;
using static SkyEye.Plugin;
using static SkyEye.MConfiguration;
using static SkyEye.Util;


namespace SkyEye;

public partial class ConfigWindow {
	private static void ValidateSpeedInfo() {
		SetSpeed(1);
		if (!Configuration.SpeedUp[0].SpeedUpTerritory.Equals(SpeedInfo.Default().SpeedUpTerritory))
			Configuration.SpeedUp[0].SpeedUpTerritory = SpeedInfo.Default().SpeedUpTerritory;
		if (!Configuration.SpeedUp[0].Desc.Equals(SpeedInfo.Default().Desc))
			Configuration.SpeedUp[0].Desc = SpeedInfo.Default().Desc;
		if (Configuration.SpeedUp[^2].SpeedUpTerritory.IsNullOrEmpty())
			Configuration.SpeedUp.Remove(Configuration.SpeedUp[^2]);
		if (!Configuration.SpeedUp[^1].SpeedUpTerritory.IsNullOrEmpty())
			Configuration.SpeedUp.Add(new SpeedInfo());
		CurrentSpeedInfo = null;
		foreach (var s in Configuration.SpeedUp.Where(s => s.Enabled && s.SpeedUpTerritory.Split('|').Contains(ClientState.TerritoryType.ToString()))) {
			CurrentSpeedInfo = s;
			break;
		}
		SetSpeed(1);
		Configuration.Save();
	}

	private static void DrawSpeed() {
		ImGui.Text("提示：");
		ImGui.Text("走路倍率6，坐骑倍率6*2。死亡会掉速，点击重置即可恢复。");
		ImGui.Text("地区id用竖线|隔开。");
		if (ImGui.Checkbox("无人就加速", ref Configuration.SpeedUpEnabled)) Configuration.Save();
		ImGui.SameLine();
		if (ImGui.Button("重置")) {
			foreach (var s in Configuration.SpeedUp.Where(s => s.Enabled && s.SpeedUpTerritory.Split('|').Contains(ClientState.TerritoryType.ToString()))) {
				CurrentSpeedInfo = s;
				break;
			}
			SetSpeed(1);
		}
		ImGui.Text("从下往上删没问题，从中间删会导致null项目，点击即可清除");
		ImGui.SameLine();
		if (ImGui.Button("清空null项目")) {
			Configuration.SpeedUp = Configuration.SpeedUp.Where(i => !i.SpeedUpTerritory.IsNullOrEmpty()).ToList();
			ValidateSpeedInfo();
		}
		if (Configuration.SpeedUpEnabled) {
			string[] header = ["启用", "地区Id", "倍率", "最终速度上限(含乘算倍率)", "备注"];
			if (ImGui.BeginTable("TableSpeedInfo", header.Length, ImGuiTableFlag)) {
				foreach (var item in header) ImGui.TableSetupColumn(item, ImGuiTableColumnFlags.WidthStretch);
				ImGui.TableHeadersRow();
				for (var i = 0; i < Configuration.SpeedUp.Count; i++) {
					ImGui.TableNextRow();
					ImGui.TableSetColumnIndex(0);
					if (ImGui.Checkbox($"##启用{i}", ref Configuration.SpeedUp[i].Enabled)) ValidateSpeedInfo();
					ImGui.TableSetColumnIndex(1);
					ImGui.SetNextItemWidth(-1);
					if (ImGui.InputText($"##地区{i}", ref Configuration.SpeedUp[i].SpeedUpTerritory)) ValidateSpeedInfo();
					if (ImGui.IsItemHovered()) {
						var sb = new StringBuilder();
						foreach (var t in Configuration.SpeedUp[i].SpeedUpTerritory.Split('|'))
							if (!t.IsNullOrEmpty() && MapInfo.TryGetValue(t, out var value))
								sb.Append(t).Append('|').Append(value).Append('\n');
						if (sb.Length != 0) {
							sb.Remove(sb.Length - 1, 1);
							ImGui.SetTooltip(sb.ToString());
						}
					}
					ImGui.TableSetColumnIndex(2);
					ImGui.SetNextItemWidth(-1);
					if (ImGui.InputFloat($"##倍率{i}", ref Configuration.SpeedUp[i].SpeedUpN)) ValidateSpeedInfo();
					ImGui.TableSetColumnIndex(3);
					ImGui.SetNextItemWidth(-1);
					if (ImGui.InputFloat($"##最大{i}", ref Configuration.SpeedUp[i].SpeedUpMax)) ValidateSpeedInfo();
					ImGui.TableSetColumnIndex(4);
					ImGui.SetNextItemWidth(-1);
					if (ImGui.InputText($"##描述{i}", ref Configuration.SpeedUp[i].Desc)) ValidateSpeedInfo();
				}
				ImGui.EndTable();
			}
		}
		ImGui.Text("无视周边的挂壁亲友（用竖线|隔开）");
		if (ImGui.InputText("Friendly names", ref Configuration.SpeedUpFriendly, 114514)) Configuration.Save();
		ImGui.Text($"周围人数：{(InArea() ? OtherPlayer.Count : "不在区域内")};区域id：{ClientState.TerritoryType}");
		ImGui.Separator();
		NewTable(["Id", "名称"], MapInfo.Select(p => (p.Key, p.Value)).ToArray(), [
			i => ImGui.Text(i.Key),
			i => ImGui.Text(i.Value)
		], [
			i => i.Key,
			i => i.Value
		], "Territory");
	}
}