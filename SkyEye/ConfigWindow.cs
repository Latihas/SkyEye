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
			NewTab("地图/元灵", DrawMap);
			NewTab("加速", DrawSpeed);
			NewTab("宝箱", DrawChest);
			NewTab("农怪", DrawFarm);
			NewTab("目标", DrawTarget);
			NewTab("史书", DrawHistory);
			NewTab("Fate", DrawFate);

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
					if (HasCore())
						NewTab("箱子", () => {
							ImGui.Text("建议DR开附近箱子距离设置为7.5，IC下潜7。");
							if (ImGui.InputText("寻宝前指令", ref Configuration.BeforeOccultTreasure)) Configuration.Save();
							if (ImGui.InputText("寻宝后指令", ref Configuration.AfterOccultTreasure)) Configuration.Save();
							if (ImGui.InputInt("时间延迟(看加载速度)(ms)", ref Configuration.OccultTreasureDelay)) Configuration.Save();
							if (!PData.OccultTreasurePosition.TryGetValue(ClientState.TerritoryType, out var value)) return;
							ImGui.Text("银箱子");
							foreach (var p in value.Where(p => p.Item2 == 1597)) {
								ImGui.Text($"{p.Item1}");
								ImGui.SameLine();
								if (ImGui.Button($"走##{p.Item1}")) CoreDiveTp(p.Item1, true);
							}
							ImGui.Text("开所有箱子");
							if (!isFindingTreasure && ImGui.Button("开始")) {
								lock (isFindingTreasureLock) isFindingTreasure = true;
								ChatBox.SendMessage(Configuration.BeforeOccultTreasure);
								Task.Run(async () => {
									for (var i = 0; i < value.Count; i++) {
										var p = value[i];
										if (!isFindingTreasure) break;
										ChatBox.SendMessage($"/e 点位 {i}/{value.Count}");
										CoreDiveTp(p.Item1, true);
										await Task.Delay(5000);
									}
									ChatBox.SendMessage(Configuration.AfterOccultTreasure);
									lock (isFindingTreasureLock) isFindingTreasure = false;
								});
							}
							if (isFindingTreasure && ImGui.Button("强制结束")) {
								QuitInstanceD ??= Marshal.GetDelegateForFunctionPointer<QuitInstanceDelegate>(SigScanner.ScanText("48 83 EC ?? 0F B6 D1 45 33 C9"));
								QuitInstanceD(0);
								lock (isFindingTreasureLock) isFindingTreasure = false;
							}
						});
					ImGui.EndTabBar();
				}
			});
			NewTab("改名", DrawNameReplacement);
			if (HasCore()) {
				NewTab("Tp", DrawTp);
				NewTab("深宫", DrawPalacePal);
			}
			ImGui.EndTabBar();
		}
	}

	private static QuitInstanceDelegate? QuitInstanceD;
	internal static bool isFindingTreasure;
	private readonly Lock isFindingTreasureLock = new();

	private delegate IntPtr QuitInstanceDelegate(byte shouldForceQuit);
}