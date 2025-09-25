using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace SkyEye.SkyEye;

public class ConfigWindow(Plugin plugin) : Window("SkyEye") {
	private readonly Configuration _configuration = Plugin.Configuration;

	public override void Draw() {
		if (ImGui.Checkbox("开关", ref _configuration.Overlay2DEnabled)) _configuration.Save();
		if (!_configuration.Overlay2DEnabled) return;
		ImGui.Separator();
		if (ImGui.Checkbox("稀有天气时间开关", ref _configuration.Overlay2DWeatherMapEnabled)) _configuration.Save();
		if (ImGui.Checkbox("宝箱位置绘制开关", ref _configuration.Overlay3DEnabled)) _configuration.Save();
		ImGui.Separator();
		if (ImGui.Checkbox("无人就加速", ref _configuration.Overlay2DSpeedUpEnabled)) _configuration.Save();
		ImGui.SameLine();
		if (ImGui.InputFloat("倍率", ref _configuration.Overlay2DSpeedUpN)) _configuration.Save();
		ImGui.SameLine();
		if (ImGui.Button("重置")) Plugin.SetSpeed(1f);
		ImGui.Text(@"加速区域id（用竖线|隔开，默认支持732,763,795,827），可在ACT.DieMoe\Plugins\Data\FFXIV_ACT_Plugin\Chinese\Resource\FFXIV_ACT_Plugin.Resource.Generated.TerritoryList_English.txt中查看");
		if (ImGui.InputText("Territory ids", ref _configuration.Overlay2DSpeedUpTerritory, 114514)) _configuration.Save();
		ImGui.Text("无视周边的挂壁亲友（用竖线|隔开）");
		if (ImGui.InputText("Friendly names", ref _configuration.Overlay2DSpeedUpFriendly, 114514)) _configuration.Save();
		ImGui.Text($"周围人数：{(Plugin.InArea() ? plugin.OtherPlayer.Count : "不在区域内")};区域id：{Plugin.ClientState.TerritoryType}");
		if (Plugin.InEureka()) {
			ImGui.Separator();
			ImGui.Text("NM开战时间喊话");
			if (ImGui.InputText("输入NM开战时间", ref _configuration.NmBattleTimeText, 128)) _configuration.Save();
			ImGui.SameLine();
			if (ImGui.Button("发送至喊话频道") && !string.IsNullOrWhiteSpace(_configuration.NmBattleTimeText)) {
				Plugin.ChatBox.SendMessage($"/sh <pos>{_configuration.NmBattleTimeText}");
			}
		}
	}
}