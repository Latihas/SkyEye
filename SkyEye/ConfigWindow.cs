using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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
								if (ImGui.Button($"走##{i}")) PathfindAndMoveTo(pos, false);
							}
					});
					NewTab("罐子", DrawOccultPot);
					if (CanTp()) NewTab("箱子", DrawOccultChest);
					ImGui.EndTabBar();
				}
			});
			NewTab("加速", DrawSpeed);
			NewTab("农怪", DrawFarm);
			NewTab("目标", DrawTarget);
			NewTab("改名", DrawNameReplacement);
			NewTab("Tp", DrawTp);
			if (HasCore()) NewTab("深宫", DrawPalacePal);
			ImGui.EndTabBar();
		}
	}

	private static QuitInstanceDelegate? QuitInstanceD;
	internal static bool isFindingTreasure;
	private readonly Lock isFindingTreasureLock = new();

	private delegate IntPtr QuitInstanceDelegate(byte shouldForceQuit);
}