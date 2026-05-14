using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.Game;
using static SkyEye.Ipcs;
using static SkyEye.Plugin;


namespace SkyEye;

public partial class ConfigWindow {
	private static float tpX, tpZ, tpY;
	private static SetPositionDelegate? setPosition;

	private delegate long SetPositionDelegate(long playerAddress, float x, float y, float z);


	private static void DrawTp() {
		ImGui.Text("自定义Tp为其他插件的潜水tp指令。");
		ImGui.Text("自定义Tp指令的坐标使用<x> <y> <z>(有尖括号)代替(例如/tp <x> <y> <z>)");
		if (ImGui.InputText("自定义Tp指令", ref Configuration.TpCommand)) Configuration.Save();
		if (!CanTp()) return;
		if (ImGui.Checkbox("绿玩在附近也tp", ref Configuration.CoreTpWhenGreenNearby)) Configuration.Save();
		if (ObjectTable.LocalPlayer == null) return;
		if (ImGui.Button("潜水无敌")) CoreDive(true);
		if (ImGui.Button("潜水天灾")) {
			Task.Run(async () => {
				ChatBox.SendMessage("/共通技能 任务指令1");
				await Task.Delay(100);
				CoreDive(true);
			});
		}
		if (ImGui.Button("潜水Tp到flag")) {
			var p = FlagToPoint();
			if (p.HasValue) CoreDiveTp(p.Value, true);
		}
		ImGui.Separator();
		if (ImGui.Checkbox("解除高危tp防护", ref Configuration.PreventTp)) Configuration.Save();
		if (Configuration.PreventTp) {
			if (ImGui.Button("潜水Tp到坐标"))
				CoreDiveTp(new Vector3(tpX, tpY, tpZ), true);
			if (ImGui.Button("到坐标再潜水Tp")) {
				Task.Run(async () => {
					if (setPosition == null) {
						if (SigScanner.TryScanText("E8 ?? ?? ?? ?? 83 4B 70 01", out var x))
							setPosition = Marshal.GetDelegateForFunctionPointer<SetPositionDelegate>(x);
					}
					if (setPosition == null) return;
					setPosition(ObjectTable.LocalPlayer.Address, tpX, tpY, tpZ);
					await Task.Delay(100);
					CoreDive(true);
				});
			}
			ImGui.InputFloat("tpX", ref tpX);
			ImGui.InputFloat("tpY", ref tpY);
			ImGui.InputFloat("tpZ", ref tpZ);
			ImGui.Separator();
		}
		ImGui.Text($"当前坐标: {ObjectTable.LocalPlayer.Position.ToString()}");
		foreach (var p in PartyList) {
			var name = p.Name.ToString();
			if (ImGui.Button(name)) ImGui.SetClipboardText(name);
			ImGui.SameLine();
			ImGui.Text($": {p.Position.ToString()}");
			if (Configuration.PreventTp) {
				ImGui.SameLine();
				if (ImGui.Button($"传送##{name}"))
					CoreDiveTp(p.Position, true);
			}
		}
		ImGui.Separator();
		unsafe {
			var baseaddr = (IntPtr)GameMain.Instance() + SigScanner.GetStaticAddressFromSig("48 8D 8F ?? ?? ?? ?? 40 0F B6 D5 E8 ?? ?? ?? ?? 8B D3") + 1488;
			ImGui.Text("[测试] 岛ID数据: ");
			for (var i = 0; i < 4 * 8 * 4; i += 4) {
				ImGui.Text(Marshal.ReadInt32(baseaddr + i).ToString("x8"));
				if (i % 32 != 28) ImGui.SameLine();
			}
		}
	}
}