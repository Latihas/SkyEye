using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiNotification;
using static SkyEye.Plugin;


namespace SkyEye;

public partial class ConfigWindow {
	private static void DrawFarm() {
		ImGui.Text("提示：");
		ImGui.Text("该功能会占用Vnav寻路功能");
		ImGui.Text("移动到北萨那兰会自动切换为刷B怪,名称为");
		ImGui.SameLine();
		if (ImGui.Button("永恒不灭的菲兰德副耀士")) {
			ImGui.SetClipboardText("永恒不灭的菲兰德副耀士");
			NotificationManager.AddNotification(new Notification {
				Title = "已复制",
				Content = "永恒不灭的菲兰德副耀士"
			});
		}
		ImGui.Text("自动农怪可能在第一次开启时无反应，/xivplugins关闭打开一次SkeEye即可。");
		if (ImGui.Checkbox("自动农怪", ref Configuration.AutoFarm)) {
			LastKill = DateTime.Now;
			Configuration.Save();
		}
		if (ImGui.InputText("怪名称", ref Configuration.FarmTarget, 114514)) Configuration.Save();
		string[] distAlgo = ["从第一个怪位置开始计算", "从指定点位开始计算"];
		if (ImGui.Combo("引仇距离计算方式", ref Configuration.FarmDistAlgo, distAlgo)) {
			Configuration.Save();
			ImGui.EndCombo();
		}
		if (Configuration.AutoFarm) {
			if (ImGui.InputText("开怪指令", ref Configuration.FarmStartCommand, 114514)) Configuration.Save();
			if (ImGui.InputInt("最大引仇目标", ref Configuration.FarmTargetMax, 1)) Configuration.Save();
			if (Configuration.FarmDistAlgo == 1) {
				if (ImGui.InputFloat("FarmWaitX", ref Configuration.FarmWaitX)) Configuration.Save();
				if (ImGui.InputFloat("FarmWaitY", ref Configuration.FarmWaitY)) Configuration.Save();
				if (ImGui.InputFloat("FarmWaitZ", ref Configuration.FarmWaitZ)) Configuration.Save();
				if (ImGui.Button("设置农怪返回点为当前坐标") && ObjectTable.LocalPlayer != null) {
					Configuration.FarmWaitX = ObjectTable.LocalPlayer.Position.X;
					Configuration.FarmWaitY = ObjectTable.LocalPlayer.Position.Y;
					Configuration.FarmWaitZ = ObjectTable.LocalPlayer.Position.Z;
					Configuration.Save();
					if (lastFarmPos != null) lastFarmPos = new Vector3(Configuration.FarmWaitX, Configuration.FarmWaitY, Configuration.FarmWaitZ);
				}
			}
			if (ImGui.InputFloat("最大引仇距离", ref Configuration.FarmMaxDistance, 1)) Configuration.Save();
			if (ImGui.Checkbox("打完一波再拉下一波", ref Configuration.FarmWait)) Configuration.Save();
		}
		if (ClientState.TerritoryType == 147 && Configuration.AutoFarm) ImGui.Text($"超时：{(DateTime.Now - LastKill).Seconds}/{FarmTimeout}");
	}
}