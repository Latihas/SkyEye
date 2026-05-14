using System;
using Dalamud.Bindings.ImGui;
using static SkyEye.Plugin;

namespace SkyEye;

public partial class ConfigWindow {
	private void DrawOccultPot() {
		if (ImGui.Checkbox("进入新场景禁用开宝箱", ref Configuration.DisableAutoPotWhenTerritoryChanged)) Configuration.Save();
		if (ImGui.Checkbox("自动开宝箱", ref Configuration.AutoPot)) Configuration.Save();
		if (Configuration.AutoPot) {
			ImGui.Indent();
			if (ImGui.InputText("开始寻找宝箱前指令(|分割)", ref Configuration.BeforeFindPot)) Configuration.Save();
			if (ImGui.InputText("开宝箱后指令(|分割)", ref Configuration.AfterFindPot)) Configuration.Save();
			ImGui.Unindent();
		}
		if (ImGui.Button("立刻寻找")) FindPot(force: true);
		ImGui.Indent();
		if (ImGui.InputText("开始导航到罐子前指令(|分割)", ref Configuration.BeforeGotoNewPot)) Configuration.Save();
		ImGui.Unindent();

		ImGui.Separator();
		ImGui.Text("统计");
		foreach (var p in Configuration.TotalPot)
			ImGui.Text($"{p.Key}:{p.Value}");
		if (ImGui.Button("清空统计")) {
			Configuration.TotalPot = [];
			Configuration.Save();
		}
	}
}