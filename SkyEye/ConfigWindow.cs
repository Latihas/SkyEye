using System;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace SkyEye.SkyEye;

public class ConfigWindow() : Window("SkyEye") {
    private static void NewTab(string tabname, Action act) {
        if (!ImGui.BeginTabItem(tabname)) return;
        act();
        ImGui.EndTabItem();
    }

    public override void Draw() {
        if (ImGui.Checkbox("开关", ref Plugin.Configuration.PluginEnabled)) Plugin.Configuration.Save();
        if (!Plugin.Configuration.PluginEnabled) {
            Plugin.SetSpeed(1);
            return;
        }
        if (ImGui.BeginTabBar("tab")) {
            NewTab("基础", () => {
                if (ImGui.Checkbox("稀有天气时间开关", ref Plugin.Configuration.Overlay2DWeatherMapEnabled)) Plugin.Configuration.Save();
                if (ImGui.Checkbox("元灵位置绘制开关", ref Plugin.Configuration.Overlay3DEnabled)) Plugin.Configuration.Save();
                if (Plugin.InEureka()) {
                    ImGui.Separator();
                    ImGui.Text("NM开战时间喊话");
                    if (ImGui.InputText("输入NM开战时间", ref Plugin.Configuration.NmBattleTimeText, 128)) Plugin.Configuration.Save();
                    ImGui.SameLine();
                    if (ImGui.Button("发送至喊话频道") && !string.IsNullOrWhiteSpace(Plugin.Configuration.NmBattleTimeText))
                        ChatBox.SendMessage($"/sh <pos>{Plugin.Configuration.NmBattleTimeText}");
                    ImGui.Separator();
                    for (var i = 0; i < Plugin.YlPositions.Count; i++) {
                        var p = Plugin.YlPositions[i];
                        ImGui.Text($"元灵{i}({p.X},{p.Y},{p.Z})");
                        ImGui.SameLine();
                        if (ImGui.Button($"发送位置{i}")) {
                            unsafe {
                                AgentMap.Instance()->SetFlagMapMarker(Plugin.ClientState.TerritoryType, Plugin.ClientState.MapId, p);
                            }
                            ChatBox.SendMessage($"/sh 元灵位置{i}: <flag>");
                        }
                    }
                }
            });
            NewTab("加速", () => {
                if (ImGui.Checkbox("无人就加速", ref Plugin.Configuration.SpeedUpEnabled)) Plugin.Configuration.Save();
                ImGui.SameLine();
                if (ImGui.InputFloat("倍率", ref Plugin.Configuration.SpeedUpN)) Plugin.Configuration.Save();
                ImGui.SameLine();
                if (ImGui.Button("重置")) Plugin.SetSpeed(1);
                ImGui.Text(@"加速区域id（用竖线|隔开，默认支持732,763,795,827），可在ACT.DieMoe\Plugins\Data\FFXIV_ACT_Plugin\Chinese\Resource\FFXIV_ACT_Plugin.Resource.Generated.TerritoryList_English.txt中查看");
                if (ImGui.InputText("Territory ids", ref Plugin.Configuration.SpeedUpTerritory, 114514)) Plugin.Configuration.Save();
                ImGui.Text("无视周边的挂壁亲友（用竖线|隔开）");
                if (ImGui.InputText("Friendly names", ref Plugin.Configuration.SpeedUpFriendly, 114514)) Plugin.Configuration.Save();
                ImGui.Text($"周围人数：{(Plugin.InArea() ? Plugin.OtherPlayer.Count : "不在区域内")};区域id：{Plugin.ClientState.TerritoryType}");
            });
            NewTab("宝箱", () => {
                if (ImGui.Checkbox("宝箱位置绘制开关", ref Plugin.Configuration.Overlay3DEnabled)) Plugin.Configuration.Save();
                if (ImGui.Checkbox("自动开宝箱", ref Plugin.Configuration.AutoRabbit)) Plugin.Configuration.Save();
                if (Plugin.Configuration.AutoRabbit) {
                    if (ImGui.Checkbox("自动开宝箱后回点位", ref Plugin.Configuration.AutoRabbitWait)) Plugin.Configuration.Save();
                    if (Plugin.Configuration.AutoRabbitWait) {
                        if (ImGui.InputFloat("X", ref Plugin.Configuration.RabbitWaitX)) Plugin.Configuration.Save();
                        if (ImGui.InputFloat("Y", ref Plugin.Configuration.RabbitWaitY)) Plugin.Configuration.Save();
                        if (ImGui.InputFloat("Z", ref Plugin.Configuration.RabbitWaitZ)) Plugin.Configuration.Save();
                        if (ImGui.Button("设置为当前坐标")) {
                            if (Plugin.ClientState.LocalPlayer != null) {
                                Plugin.Configuration.RabbitWaitX = Plugin.ClientState.LocalPlayer.Position.X;
                                Plugin.Configuration.RabbitWaitY = Plugin.ClientState.LocalPlayer.Position.Y;
                                Plugin.Configuration.RabbitWaitZ = Plugin.ClientState.LocalPlayer.Position.Z;
                                Plugin.Configuration.Save();
                            }
                        }
                    }
                }
            });
            NewTab("农怪", () => {
                if (ImGui.Checkbox("自动农怪(移动到北萨那兰会自动切换为刷B怪,名称为永恒不灭的菲兰德副耀士)", ref Plugin.Configuration.AutoFarm)) {
                    Plugin.LastKill = DateTime.Now;
                    Plugin.Configuration.Save();
                }
                ImGui.SameLine();
                if (ImGui.Button("永恒不灭的菲兰德副耀士")) ImGui.SetClipboardText("永恒不灭的菲兰德副耀士");

                if (ImGui.InputText("怪名称", ref Plugin.Configuration.FarmTarget, 114514)) Plugin.Configuration.Save();
                if (Plugin.Configuration.AutoFarm) {
                    if (ImGui.InputText("开怪指令", ref Plugin.Configuration.FarmStartCommand, 114514)) Plugin.Configuration.Save();
                    if (ImGui.InputInt("最大引仇目标", ref Plugin.Configuration.FarmTargetMax, 1)) Plugin.Configuration.Save();
                    if (ImGui.InputFloat("最大引仇距离(从第一个怪位置开始计算)", ref Plugin.Configuration.FarmMaxDistance, 1)) Plugin.Configuration.Save();
                    if (ImGui.Checkbox("打完一波再拉下一波", ref Plugin.Configuration.FarmWait)) Plugin.Configuration.Save();
                }
                else NavmeshIpc.Stop();
                ImGui.Text("自动农怪可能在第一次开启时无反应，还没找到bug在哪，/xivplugins关闭打开一次SkeEye即可。");
                if (Plugin.ClientState.TerritoryType == 147 && Plugin.Configuration.AutoFarm) ImGui.Text($"超时：{(DateTime.Now - Plugin.LastKill).Seconds}/{Plugin.FarmTimeout}");
            });
            NewTab("测试", () => {
                if (ImGui.Button("测试")) {
                }
            });
        }
    }
}