using System.Linq;
using System.Text;
using Dalamud.Bindings.ImGui;
using static SkyEye.Plugin;
using static SkyEye.Util;

namespace SkyEye;

public partial class ConfigWindow {
	private static void SpeedConfigurationChanged() {
		RefreshCurrentSpeedInfo();
		Configuration.Save();
	}

	private static bool InputPositiveSpeed(string label, ref float value) {
		var previous = value;
		if (!ImGui.InputFloat(label, ref value)) return false;
		if (!IsValidSpeedValue(value)) {
			value = previous;
			return false;
		}
		return true;
	}

	private static void DrawSpeed() {
		ImGui.Text("提示：");
		ImGui.Text("移速算法已重置，目前是hook游戏基础移速×配置倍率。");
		ImGui.Text("特殊场景上限基本都是3.5左右，高了基本掉线，视网络情况而定");
		ImGui.Text("保护公式：最终倍率=min(游戏原始倍率×配置倍率, 每行上限)，技能加速也会被截断。");
		ImGui.Text("地区id用竖线|隔开。");
		if (ImGui.Checkbox("无人就加速", ref Configuration.SpeedUpEnabled)) {
			if (!Configuration.SpeedUpEnabled) RestoreSpeed();
			else RefreshCurrentSpeedInfo(resetFailures: true);
			Configuration.Save();
		}
		ImGui.SameLine();
		if (ImGui.Checkbox("输出移速倍率调试信息", ref Configuration.SpeedDebugOutput)) Configuration.Save();
		ImGui.SameLine();
		if (ImGui.Button("重新应用")) {
			RefreshCurrentSpeedInfo(resetFailures: true);
		}
		if (ImGui.Button("添加配置")) {
			RestoreSpeed();
			Configuration.SpeedUp.Add(new());
			SpeedConfigurationChanged();
		}
		if (Configuration.SpeedUpEnabled) {
			string[] header = ["启用", "地区Id", "倍率", "最终倍率上限", "备注", "操作"];
			var deleteIndex = -1;
			if (ImGui.BeginTable("TableSpeedInfo", header.Length, ImGuiTableFlag)) {
				foreach (var item in header) ImGui.TableSetupColumn(item, ImGuiTableColumnFlags.WidthStretch);
				ImGui.TableHeadersRow();
				for (var i = 0; i < Configuration.SpeedUp.Count; i++) {
					var speedInfo = Configuration.SpeedUp[i];
					ImGui.TableNextRow();
					var isDefault = speedInfo.IsDefault;
					var territoryText = speedInfo.SpeedUpTerritory;
					var descText = speedInfo.Desc;
					ImGui.TableSetColumnIndex(0);
					if (ImGui.Checkbox($"##启用{i}", ref speedInfo.Enabled)) SpeedConfigurationChanged();
					ImGui.TableSetColumnIndex(1);
					ImGui.SetNextItemWidth(-1);
					if (isDefault) ImGui.Text(territoryText);
					else if (ImGui.InputText($"##地区{i}", ref territoryText)) {
						speedInfo.SpeedUpTerritory = territoryText;
						SpeedConfigurationChanged();
					}
					if (ImGui.IsItemHovered()) {
						var sb = new StringBuilder();
						foreach (var territory in territoryText.Split('|'))
							if (!string.IsNullOrEmpty(territory) && MapInfo.TryGetValue(territory, out var value))
								sb.Append(territory).Append('|').Append(value).Append('\n');
						if (sb.Length != 0) ImGui.SetTooltip(sb.ToString().TrimEnd());
					}
					ImGui.TableSetColumnIndex(2);
					ImGui.SetNextItemWidth(-1);
					if (InputPositiveSpeed($"##倍率{i}", ref speedInfo.SpeedUpN)) SpeedConfigurationChanged();
					ImGui.TableSetColumnIndex(3);
					ImGui.SetNextItemWidth(-1);
					if (InputPositiveSpeed($"##倍率上限{i}", ref speedInfo.SpeedMultiplierMax)) SpeedConfigurationChanged();
					ImGui.TableSetColumnIndex(4);
					ImGui.SetNextItemWidth(-1);
					if (isDefault) ImGui.Text(descText);
					else if (ImGui.InputText($"##描述{i}", ref descText)) {
						speedInfo.Desc = descText;
						SpeedConfigurationChanged();
					}
					ImGui.TableSetColumnIndex(5);
					if (isDefault) ImGui.Text("默认配置");
					else if (ImGui.Button($"删除##速度{i}")) deleteIndex = i;
				}
				ImGui.EndTable();
			}
			if (deleteIndex >= 0) {
				RestoreSpeed();
				Configuration.SpeedUp.RemoveAt(deleteIndex);
				SpeedConfigurationChanged();
			}
		}
		ImGui.Text("无视周边的挂壁亲友（用竖线|隔开）");
		if (ImGui.InputText("亲友", ref Configuration.SpeedUpFriendly, 114514)) Configuration.Save();
		ImGui.Text($"周围人数：{(InArea() ? OtherPlayer.Count : "不在区域内")};区域id：{ClientState.TerritoryType}");
		ImGui.Separator();
		NewTable(["Id", "名称"], [.. MapInfo.Select(p => (p.Key, p.Value))], [
			i => ImGui.Text(i.Key),
			i => ImGui.Text(i.Value)
		], [
			i => i.Key,
			i => i.Value
		], "Territory");
	}
}