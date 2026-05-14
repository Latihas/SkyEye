using Dalamud.Bindings.ImGui;
using static SkyEye.Plugin;


namespace SkyEye;

public partial class ConfigWindow {
	private static void DrawChest() {
		if (ImGui.Checkbox("宝箱位置绘制开关", ref Configuration.Overlay3DEnabled)) Configuration.Save();
		if (ImGui.Checkbox("进入新场景禁用开宝箱", ref Configuration.DisableAutoRabbitWhenTerritoryChanged)) Configuration.Save();
		if (ImGui.Checkbox("自动开宝箱", ref Configuration.AutoRabbit)) Configuration.Save();
		if (Configuration.AutoRabbit) {
			ImGui.Indent();
			if (ImGui.InputText("开始寻找宝箱前指令(|分割)", ref Configuration.BeforeFindTreasure)) Configuration.Save();
			if (ImGui.InputText("开宝箱后指令(|分割)", ref Configuration.AfterFindTreasure)) Configuration.Save();
			ImGui.Unindent();
		}
		if (ImGui.Checkbox("自动导航到新兔子", ref Configuration.AutoForwardNewRabbit)) Configuration.Save();
		ImGui.SameLine();
		if(ImGui.Button("立刻寻找"))FindRabbit(force: true);
		if (Configuration.AutoForwardNewRabbit) {
			ImGui.Indent();
			if (ImGui.InputText("开始导航到兔子前指令(|分割)", ref Configuration.BeforeGotoNewRabbit)) Configuration.Save();
			ImGui.Unindent();
		}
		ImGui.Separator();
		ImGui.Text("统计");
		foreach (var p in Configuration.TotalChest)
			ImGui.Text($"{p.Key}:{p.Value}");
		if (ImGui.Button("清空统计")) {
			Configuration.TotalChest = [];
			Configuration.Save();
		}
	}
}