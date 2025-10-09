using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace SkyEye.SkyEye;

public class ConfigWindow() : Window("SkyEye") {
    private readonly Configuration _configuration = Plugin.Configuration;

    public override void Draw() {
        if (ImGui.Checkbox("开关", ref _configuration.Overlay2DEnabled)) _configuration.Save();
        if (!_configuration.Overlay2DEnabled) return;
        ImGui.Separator();
        if (ImGui.Checkbox("稀有天气时间开关", ref _configuration.Overlay2DWeatherMapEnabled)) _configuration.Save();
        if (ImGui.Checkbox("宝箱位置绘制开关", ref _configuration.Overlay3DEnabled)) _configuration.Save();
        if (ImGui.Checkbox("自动开宝箱", ref _configuration.AutoRabbit)) _configuration.Save();
        if (_configuration.AutoRabbit) {
            if (ImGui.Checkbox("自动开宝箱后回点位", ref _configuration.AutoRabbitWait)) _configuration.Save();
            if (_configuration.AutoRabbitWait) {
                if (ImGui.InputFloat("X", ref _configuration.RabbitWaitX)) _configuration.Save();
                if (ImGui.InputFloat("Y", ref _configuration.RabbitWaitY)) _configuration.Save();
                if (ImGui.InputFloat("Z", ref _configuration.RabbitWaitZ)) _configuration.Save();
                if (ImGui.Button("设置为当前坐标")) {
                    if (Plugin.ClientState.LocalPlayer != null) {
                        _configuration.RabbitWaitX = Plugin.ClientState.LocalPlayer.Position.X;
                        _configuration.RabbitWaitY = Plugin.ClientState.LocalPlayer.Position.Y;
                        _configuration.RabbitWaitZ = Plugin.ClientState.LocalPlayer.Position.Z;
                        _configuration.Save();
                    }
                }
            }
        }
        if (ImGui.Checkbox("自动农怪", ref _configuration.AutoFarm)) _configuration.Save();
        if (_configuration.AutoFarm) {
            if (ImGui.InputText("怪名称", ref _configuration.FarmTarget, 114514)) _configuration.Save();
            if (ImGui.InputText("开怪指令", ref _configuration.FarmStartCommand, 114514)) _configuration.Save();
            if (ImGui.InputInt("最大引仇目标", ref _configuration.FarmTargetMax, 114514)) _configuration.Save();
        }
        ImGui.Separator();
        if (ImGui.Checkbox("无人就加速", ref _configuration.SpeedUpEnabled)) _configuration.Save();
        ImGui.SameLine();
        if (ImGui.InputFloat("倍率", ref _configuration.SpeedUpN)) _configuration.Save();
        ImGui.SameLine();
        if (ImGui.Button("重置")) Plugin.SetSpeed(1f);
        ImGui.Text(@"加速区域id（用竖线|隔开，默认支持732,763,795,827），可在ACT.DieMoe\Plugins\Data\FFXIV_ACT_Plugin\Chinese\Resource\FFXIV_ACT_Plugin.Resource.Generated.TerritoryList_English.txt中查看");
        if (ImGui.InputText("Territory ids", ref _configuration.SpeedUpTerritory, 114514)) _configuration.Save();
        ImGui.Text("无视周边的挂壁亲友（用竖线|隔开）");
        if (ImGui.InputText("Friendly names", ref _configuration.SpeedUpFriendly, 114514)) _configuration.Save();
        ImGui.Text($"周围人数：{(Plugin.InArea() ? Plugin.OtherPlayer.Count : "不在区域内")};区域id：{Plugin.ClientState.TerritoryType}");
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