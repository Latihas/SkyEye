using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using SkyEye.Data;
using static SkyEye.Data.TreasureRarity;
using static SkyEye.Ipcs;
using static SkyEye.Plugin;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace SkyEye;

public partial class ConfigWindow {
	private static void DrawOccultChest() {
		ImGui.Text("建议DR开附近箱子距离设置为9，IC下潜7。");
		if (ImGui.InputText("寻宝前指令(竖线|隔开)", ref Configuration.BeforeOccultTreasure)) Configuration.Save();
		if (ImGui.InputText("寻宝后指令(竖线|隔开)", ref Configuration.AfterOccultTreasure)) Configuration.Save();
		if (ImGui.InputInt("时间延迟(看加载速度)(ms)", ref Configuration.OccultTreasureDelay)) Configuration.Save();
		if (!PData.OccultTreasurePosition.TryGetValue((Territory)ClientState.TerritoryType, out var value)) return;
		if (ImGui.Checkbox("满30自动循环", ref Configuration.Auto30OccultTreasure)) Configuration.Save();
		ImGui.Text("开所有箱子");
		if (!isFindingTreasure && ImGui.Button("开始")) {
			StartFindOccultTreasure(() => {
				foreach (var p in Configuration.AfterOccultTreasure.Split("|")) ChatBox.SendMessage(p);
			});
		}
		if (ImGui.CollapsingHeader("银箱子")) {
			foreach (var p in value.Where(p => p.Item2 == Silver)) {
				ImGui.Text($"{p.Item1}");
				ImGui.SameLine();
				if (ImGui.Button($"走##{p.Item1}")) CoreDiveTp(p.Item1, true);
			}
		}
		if (isFindingTreasure && ImGui.Button("强制结束")) {
			QuitInstanceD ??= Marshal.GetDelegateForFunctionPointer<QuitInstanceDelegate>(SigScanner.ScanText("48 83 EC ?? 0F B6 D1 45 33 C9"));
			QuitInstanceD(0);
			isFindingTreasure = false;
			foreach (var p in Configuration.AfterOccultTreasure.Split("|")) ChatBox.SendMessage(p);
		}
	}

	internal static void StartFindOccultTreasure(Action after) {
		var t = ClientState.TerritoryType;
		if (!PData.OccultTreasurePosition.TryGetValue((Territory)ClientState.TerritoryType, out var value)) return;
		isFindingTreasure = true;
		foreach (var p in Configuration.BeforeOccultTreasure.Split("|")) ChatBox.SendMessage(p);
		Task.Run(async () => {
			for (var i = 0; i < value.Count; i++) {
				var p = value[i];
				if (!isFindingTreasure) break;
				ChatBox.SendMessage($"/e 点位 {i + 1}/{value.Count}");
				CoreDiveTp(p.Item1, true);
				var start = DateTime.Now;
				var opened = false;
				while ((DateTime.Now - start).TotalSeconds < 5) {
					if ((DateTime.Now - start).TotalSeconds < 0.5) continue;
					if (opened) continue;
					if (ObjectTable.LocalPlayer == null) continue;
					foreach (var obj in ObjectTable) {
						if (obj.ObjectKind == ObjectKind.Treasure && obj.Name.ToString().Contains("宝箱") && Vector3.DistanceSquared(ObjectTable.LocalPlayer.Position, obj.Position) < 12 * 12) {
							ChatBox.SendMessage("/e 开箱");
							unsafe {
								TargetSystem.Instance()->InteractWithObject((GameObject*)obj.Address);
							}
							opened = true;
						}
					}
				}
				// await Task.Delay(5000);
				if (t != ClientState.TerritoryType) break;
			}
			after();
			isFindingTreasure = false;
		});
	}
}