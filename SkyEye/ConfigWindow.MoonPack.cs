using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace SkyEye;

public partial class ConfigWindow {
	private static unsafe void DrawMoonPack() {
		try {
			ImGui.Text("CE进度");
			var addon = Plugin.Gui.GetAddonByName<AtkUnitBase>("WKSAnnounce");
			var uld = addon->UldManager;
			for (var i = 0; i < uld.NodeListSize; i++) {
				var node = uld.NodeList[i];
				if (!node->IsVisible() || (int)node->Type != 1002) continue;
				var comp = ((AtkComponentNode*)node)->Component;
				if (comp->GetComponentType() != ComponentType.GaugeBar) continue;
				var jg = (AtkComponentGaugeBar*)comp;
				ImGui.Text($"{jg->Values[0].ValueFloat}/{jg->MaxValue}");
			}
		} catch {
			//
		}
		if (Plugin.MoonPackId != 0) {
			if (!isFindingMoonPack && ImGui.Button("开始自动寻宝")) {
				isFindingMoonPack = true;
				Plugin.Framework.RunOnTick(() => AgentInventoryContext.Instance()->UseItem(Plugin.MoonPackId));
			}
			if (isFindingMoonPack && ImGui.Button("停止自动寻宝"))
				isFindingMoonPack = false;
		}
	}
}