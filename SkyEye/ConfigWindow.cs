using System;
using System.Globalization;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using SkyEye.SkyEye.Data;
using static SkyEye.SkyEye.Data.PData;
using static SkyEye.SkyEye.Util;
using Action = System.Action;

namespace SkyEye.SkyEye;

public class ConfigWindow() : Window("SkyEye") {
    private static readonly string[] regions = [
        "未选择",
        "陆行鸟",
        "莫古力",
        "猫小胖",
        "豆豆柴"
    ];

    private static void NewTab(string tabname, Action act) {
        if (!ImGui.BeginTabItem(tabname)) return;
        act();
        ImGui.EndTabItem();
    }

    public override void Draw() {
        if (ImGui.Checkbox("开关", ref Plugin.Configuration.PluginEnabled)) Plugin.Configuration.Save();
        if (!Plugin.Configuration.PluginEnabled) {
            Plugin.SetSpeed(1);
            Plugin.lastFarmPos = null;
            Plugin.FarmFull = false;
            NavmeshIpc.Stop();
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
                ImGui.Text("如果登录时就在ULK里可能会自动加速失败，关开一下就行了。");
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
                ImGui.Text("提示：该功能会占用Vnav寻路功能");
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
                ImGui.Text("自动农怪可能在第一次开启时无反应，还没找到bug在哪，/xivplugins关闭打开一次SkeEye即可。");
                if (Plugin.ClientState.TerritoryType == 147 && Plugin.Configuration.AutoFarm) ImGui.Text($"超时：{(DateTime.Now - Plugin.LastKill).Seconds}/{Plugin.FarmTimeout}");
            });
            NewTab("史书", () => {
                if (ImGui.Checkbox("连接史书", ref Plugin.Configuration.EnableWss)) {
                    WebSocket.nmalive.Clear();
                    WebSocket.nmdead.Clear();
                    Plugin.Configuration.Save();
                }
                if (ImGui.InputText("提醒(Fate原名，包含即可，用竖线|隔开)", ref Plugin.Configuration.WssNotify, 114514)) Plugin.Configuration.Save();
                if (Plugin.Configuration.EnableWss) {
                    if (ImGui.Combo("选择大区", ref Plugin.Configuration.WssRegion, regions)) {
                        Plugin.Configuration.Save();
                        WebSocket.StopWss();
                        _ = WebSocket.StartWssService();
                        ImGui.EndCombo();
                    }
                    if (Plugin.Configuration.WssRegion is < 1 or > 4) return;
                    if (!WebSocket._isWssRunning) _ = WebSocket.StartWssService();
                    ImGui.Text("活着的");
                    var acts = new Action<WebSocket.NmInfo>[] {
                        info => {
                            switch (info.territory_id) {
                                case 1:
                                    ImGui.PushStyleColor(ImGuiCol.TableRowBg, green);
                                    ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, green_alt);
                                    break;
                                case 2:
                                    ImGui.PushStyleColor(ImGuiCol.TableRowBg, cyan);
                                    ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, cyan_alt);
                                    break;
                                case 3:
                                    ImGui.PushStyleColor(ImGuiCol.TableRowBg, red);
                                    ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, red_alt);
                                    break;
                                case 4:
                                    ImGui.PushStyleColor(ImGuiCol.TableRowBg, blue);
                                    ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, blue_alt);
                                    break;
                            }
                            ImGui.Text(info.territory_name_ori);
                        },
                        info => ImGui.Text(info.oriname), info => ImGui.Text(info.hp.ToString()), info => ImGui.Text(getDeltaMin(info.appeared_at).ToString()), info => { ImGui.Text(getDeltaMin(info.defeated_at).ToString()); }
                    };
                    NewTable(["地点", "名称", "血量", "触发时间(min)", "击杀时间(min)"],
                        WebSocket.nmalive.OrderBy(a => a.territory_id).ThenBy(a => getDeltaMin(a.defeated_at)).ToArray(), acts);

                    ImGui.Text("已死亡");
                    NewTable(["地点", "名称", "血量", "触发时间(min)", "击杀时间(min)"],
                        WebSocket.nmdead.OrderBy(a => a.territory_id).ThenBy(a => getDeltaMin(a.defeated_at)).ToArray(), acts);
                }
                else WebSocket.StopWss();
            });
            NewTab("Fate", () => {
                var acts = new Action<EurekaFate>[] {
                    i => ImGui.Text(i.Lv), i => {
                        if (ImGui.Button(i.Name)) {
                            ImGui.SetClipboardText(i.Name);
                            if (Plugin.InEureka()) {
                                unsafe {
                                    AgentMap.Instance()->SetFlagMapMarker(Plugin.ClientState.TerritoryType, Plugin.ClientState.MapId,
                                        ToVector3(MapToWorld(i.FatePosition, MapInfo[Plugin.ClientState.TerritoryType].Item1, MapInfo[Plugin.ClientState.TerritoryType].Item2, MapInfo[Plugin.ClientState.TerritoryType].Item3)));
                                }
                                ChatBox.SendMessage("/vnav moveflag");
                            }
                        }
                    },
                    i => {
                        if (i.Trigger.IsNullOrEmpty()) ImGui.Text("");
                        else if (ImGui.Button(i.Trigger)) ImGui.SetClipboardText(i.Trigger);
                    },
                    i => ImGui.Text(i.TriggerLv), i => ImGui.Text(i.SpawnRequiredWeather.ToFriendlyString()), i => ImGui.Text(i.SpawnByRequiredNight ? "是" : "")
                };
                var orderact = new[] {
                    () => {
                        ImGui.Text("常风之地");
                        NewTable(["等级", "任务名", "触发怪", "触发怪等级", "天气", "需要夜晚"], EurekaAnemos.AnemosFates, acts);
                    },
                    () => {
                        ImGui.Text("恒冰之地");
                        NewTable(["等级", "任务名", "触发怪", "触发怪等级", "天气", "需要夜晚"], EurekaPagos.PagosFates, acts);
                    },
                    () => {
                        ImGui.Text("涌火之地");
                        NewTable(["等级", "任务名", "触发怪", "触发怪等级", "天气", "需要夜晚"], EurekaPyros.PyrosFates, acts);
                    },
                    () => {
                        ImGui.Text("丰水之地");
                        NewTable(["等级", "任务名", "触发怪", "触发怪等级", "天气", "需要夜晚"], EurekaHydatos.HydatosFates, acts);
                    }
                };
                switch (Plugin.ClientState.TerritoryType) {
                    case 732:
                        ImGui.PushStyleColor(ImGuiCol.TableRowBg, white);
                        ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, white_alt);
                        orderact[0]();
                        ImGui.PushStyleColor(ImGuiCol.TableRowBg, black);
                        ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, black_alt);
                        orderact[1]();
                        orderact[2]();
                        orderact[3]();
                        break;
                    case 763:
                        ImGui.PushStyleColor(ImGuiCol.TableRowBg, white);
                        ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, white_alt);
                        orderact[1]();
                        ImGui.PushStyleColor(ImGuiCol.TableRowBg, black);
                        ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, black_alt);
                        orderact[0]();
                        orderact[2]();
                        orderact[3]();
                        break;
                    case 795:
                        ImGui.PushStyleColor(ImGuiCol.TableRowBg, white);
                        ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, white_alt);
                        orderact[2]();
                        ImGui.PushStyleColor(ImGuiCol.TableRowBg, black);
                        ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, black_alt);
                        orderact[0]();
                        orderact[1]();
                        orderact[3]();
                        break;
                    case 827:
                        ImGui.PushStyleColor(ImGuiCol.TableRowBg, white);
                        ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, white_alt);
                        orderact[3]();
                        ImGui.PushStyleColor(ImGuiCol.TableRowBg, black);
                        ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, black_alt);
                        orderact[0]();
                        orderact[1]();
                        orderact[2]();
                        break;
                    default:
                        ImGui.PushStyleColor(ImGuiCol.TableRowBg, black);
                        ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, black_alt);
                        orderact[0]();
                        orderact[1]();
                        orderact[2]();
                        orderact[3]();
                        break;
                }
            });
            // NewTab("测试", () => {
            //     if (ImGui.Button("测试")) {
            //     }
            // });
        }
    }

    private static int getDeltaMin(string d) {
        try {
            var dateTime = DateTime.ParseExact(d, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            return (int)new TimeSpan(DateTime.Now.Ticks - dateTime.Ticks).TotalMinutes;
        }
        catch {
            try {
                var dateTime = DateTime.ParseExact(d, "yyyy-MM-dd H:mm:ss", CultureInfo.InvariantCulture);
                return (int)new TimeSpan(DateTime.Now.Ticks - dateTime.Ticks).TotalMinutes;
            }
            catch {
                return -1;
            }
        }
    }
}