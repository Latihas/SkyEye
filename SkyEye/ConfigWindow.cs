using System;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using SkyEye.Data;
using static SkyEye.Ipcs;
using static SkyEye.Plugin;
using static SkyEye.Util;

namespace SkyEye;

public partial class ConfigWindow() : Window("SkyEye") {
	public override void Draw() {
		if (ImGui.Checkbox("开关", ref Configuration.PluginEnabled)) Configuration.Save();
		if (!Configuration.PluginEnabled) {
			SetSpeed(1);
			lastFarmPos = null;
			FarmFull = false;
			Stop();
			return;
		}
		if (ImGui.BeginTabBar("tab")) {
			NewTab("ULK", () => {
				if (ImGui.BeginTabBar("ULK tab")) {
					NewTab("地图/元灵", DrawMap);
					NewTab("宝箱", DrawChest);
					NewTab("史书", DrawHistory);
					NewTab("Fate", DrawFate);
					ImGui.EndTabBar();
				}
			});
			NewTab("月岛", () => {
				if (ImGui.BeginTabBar("月岛tab")) {
					NewTab("萝卜", () => {
						foreach (var p in PData.OccultBunnyPosition
							         .Where(p => ClientState.TerritoryType == p.Key))
							for (var i = 0; i < p.Value.Count; i++) {
								var pos = p.Value[i];
								ImGui.Text($"[{i}] {pos}");
								ImGui.SameLine();
								if (ImGui.Button($"走##{i}")) PathfindAndMoveTo(pos);
							}
					});
					NewTab("罐子", DrawOccultPot);
					if (CanTp()) NewTab("箱子", DrawOccultChest);
					ImGui.EndTabBar();
				}
			});
			NewTab("月球", DrawMoonPack);
			NewTab("加速", DrawSpeed);
			NewTab("农怪", DrawFarm);
			NewTab("目标", DrawTarget);
			NewTab("改名", DrawNameReplacement);
			NewTab("Tp", DrawTp);
			NewTab("显示范围", () => {
				if (ImGui.Checkbox("显示自身位置", ref Configuration.ShowMyPos)) Configuration.Save();
				if (ImGui.Checkbox("显示圆环", ref Configuration.ShowCircle)) Configuration.Save();
				if (ImGui.InputFloat("圆环半径", ref Configuration.CircleR)) Configuration.Save();
				if (ImGui.Checkbox("显示方环", ref Configuration.ShowSquare)) Configuration.Save();
				if (ImGui.InputFloat("方环半径", ref Configuration.SquareR)) Configuration.Save();
				if (ImGui.InputFloat("粗细", ref Configuration.ShowThickness)) Configuration.Save();
				if (ImGui.InputFloat("绘制中心X", ref Configuration.ShowCenterX)) Configuration.Save();
				if (ImGui.InputFloat("绘制中心Y", ref Configuration.ShowCenterY)) Configuration.Save();
				if (ImGui.InputFloat("绘制中心Z", ref Configuration.ShowCenterZ)) Configuration.Save();
			});
			if (HasCore()) NewTab("深宫", DrawPalacePal);
			ImGui.EndTabBar();
		}
	}

	private static QuitInstanceDelegate? QuitInstanceD;
	internal static bool isFindingTreasure;
	internal static bool isFindingMoonPack;

	private delegate IntPtr QuitInstanceDelegate(byte shouldForceQuit);
}