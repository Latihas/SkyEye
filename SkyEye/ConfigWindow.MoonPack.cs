using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace SkyEye;

public partial class ConfigWindow {
	private static unsafe void DrawMoonPack() {
		if (Plugin.MoonPackId == 0) return;
		if (!isFindingMoonPack && ImGui.Button("开始自动寻宝")) {
			isFindingMoonPack = true;
			AgentInventoryContext.Instance()->UseItem(Plugin.MoonPackId);
		}
		if (isFindingMoonPack && ImGui.Button("停止自动寻宝"))
			isFindingMoonPack = false;
	}
}