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
			NewTab("地图/元灵", DrawMap);
			NewTab("加速", DrawSpeed);
			NewTab("宝箱", DrawChest);
			NewTab("农怪", DrawFarm);
			NewTab("目标", DrawTarget);
			NewTab("史书", DrawHistory);
			NewTab("Fate", DrawFate);

			NewTab("月岛", () => {
				foreach (var p in PData.OccultBunnyPosition
					         .Where(p => ClientState.TerritoryType == p.Key))
					for (var i = 0; i < p.Value.Count; i++) {
						var pos = p.Value[i];
						ImGui.Text($"[{i}] {pos}");
						ImGui.SameLine();
						if (ImGui.Button($"走##{i}")) PathfindAndMoveTo(pos, false);
					}
			});
			NewTab("改名", DrawNameReplacement);
			if (HasCore()) {
				NewTab("Tp", DrawTp);
				NewTab("深宫", DrawPalacePal);
			}
		}
	}
}