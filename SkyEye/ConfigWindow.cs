using System;
using System.Runtime.InteropServices;
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
        if (ImGui.Checkbox("自动农怪(移动到北萨那兰会自动切换为刷B怪,名称为永恒不灭的菲兰德副耀士)", ref _configuration.AutoFarm)) _configuration.Save();
        ImGui.SameLine();
        if (ImGui.Button("永恒不灭的菲兰德副耀士")) ImGui.SetClipboardText("永恒不灭的菲兰德副耀士");
        if (_configuration.AutoFarm) {
            if (ImGui.InputText("怪名称", ref _configuration.FarmTarget, 114514)) _configuration.Save();
            if (ImGui.InputText("开怪指令", ref _configuration.FarmStartCommand, 114514)) _configuration.Save();
            if (ImGui.InputInt("最大引仇目标", ref _configuration.FarmTargetMax, 1)) _configuration.Save();
            if (ImGui.InputFloat("最大引仇距离", ref _configuration.FarmMaxDistance, 1)) _configuration.Save();
        }
        else NavmeshIpc.Stop();
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
        if (Plugin.ClientState.TerritoryType == 147) ImGui.Text($"超时：{(DateTime.Now - Plugin.LastKill).Seconds }/{Plugin.FarmTimeout}");
        if (ImGui.Button("测试")) {
            StaticVfx.StaticVfxCreate = Marshal.GetDelegateForFunctionPointer<StaticVfx.StaticVfxCreateDelegate>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? F3 0F 10 35 ?? ?? ?? ?? 48 89 43 08"));
            StaticVfx.StaticVfxRun = Marshal.GetDelegateForFunctionPointer<StaticVfx.StaticVfxRunDelegate>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? B0 02 EB 02"));
            var playerObject = Plugin.ClientState.LocalPlayer!;
            var path = "vfx/omen/eff/mdl_general_02x.avfx";
            StaticVfx.Vfxs.Add(new StaticVfx(path, playerObject.Position, playerObject.Rotation), new VfxSpawnItem(path, SpawnType.Ground, false));
        }
    }
}