using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using static SkyEye.Plugin;


namespace SkyEye;

public partial class ConfigWindow {
	private static void DrawMap() {
		if (ImGui.Checkbox("稀有天气时间开关", ref Configuration.Overlay2DWeatherMapEnabled)) Configuration.Save();
		if (ImGui.Checkbox("详细信息开关", ref Configuration.Overlay2DDetailEnabled)) Configuration.Save();
		if (ImGui.Checkbox("显示元灵点位", ref Configuration.ShowCurrentElemental)) Configuration.Save();
		if (!InEureka()) return;
		ImGui.Separator();
		ImGui.Text("NM开战时间喊话");
		if (ImGui.InputText("输入NM开战时间", ref Configuration.NmBattleTimeText, 128)) Configuration.Save();
		ImGui.SameLine();
		if (ImGui.Button("发送至喊话频道") && !string.IsNullOrWhiteSpace(Configuration.NmBattleTimeText))
			ChatBox.SendMessage($"/sh <pos>{Configuration.NmBattleTimeText}");
		ImGui.Separator();
		for (var i = 0; i < ElementalPositions.Count; i++) {
			var p = ElementalPositions[i];
			ImGui.Text($"元灵{i}({p.X},{p.Y},{p.Z})");
			ImGui.SameLine();
			if (ImGui.Button($"发送位置{i}")) {
				unsafe {
					AgentMap.Instance()->SetFlagMapMarker(ClientState.TerritoryType, ClientState.MapId, p);
					ChatBox.SendMessage($"/sh 元灵位置{i}: <flag>");
				}
			}
		}
		ImGui.Separator();
		// ImGui.Text("当前Flag地面坐标: " + FlagToPoint());
		if (ImGui.InputFloat("Flag环绕绘制距离", ref Configuration.FlagR)) Configuration.Save();
		if (ImGui.Button("Flag环绕绘制")) {
			Task.Run(async () => {
				if (ObjectTable.LocalPlayer == null) return;
				const int pointCount = 50;
				var originPosition = ObjectTable.LocalPlayer.Position;
				for (var i = 0; i < pointCount; i++) {
					var angle = 2 * MathF.PI * i / pointCount;
					var offsetX = Configuration.FlagR * MathF.Cos(angle);
					var offsetZ = Configuration.FlagR * MathF.Sin(angle);
					var newPoint = new Vector3(originPosition.X + offsetX, originPosition.Y, originPosition.Z + offsetZ);
					unsafe {
						AgentMap.Instance()->SetFlagMapMarker(ClientState.TerritoryType, ClientState.MapId, newPoint);
					}
					await Task.Delay(20);
				}
			});
		}
		foreach (var p in Configuration.AllElementalPositions) {
			ImGui.Text(p.Key.ToString());
			ImGui.Indent();
			foreach (var v in p.Value.OrderBy(i => i.X))
				ImGui.Text(v.ToString());
			ImGui.Unindent();
		}
	}
}