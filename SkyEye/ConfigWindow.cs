using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            Plugin.lastFarmPos = null;
            Plugin.FarmFull = false;
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
                    nmalive.Clear();
                    nmdead.Clear();
                    Plugin.Configuration.Save();
                }

                if (Plugin.Configuration.EnableWss) {
                    if (ImGui.Combo("选择大区", ref Plugin.Configuration.WssRegion, regions)) {
                        Plugin.Configuration.Save();
                        StopWss();
                        _wssCts ??= new CancellationTokenSource();
                        _ = StartWssService(_wssCts!.Token);
                        ImGui.EndCombo();
                    }
                    if (Plugin.Configuration.WssRegion is < 1 or > 4) return;
                    if (!_isWssRunning) {
                        _wssCts ??= new CancellationTokenSource();
                        _ = StartWssService(_wssCts!.Token);
                    }
                    ImGui.Text("活着的");
                    NewTable(["地点", "名称","血量", "触发时间(min)", "击杀时间(min)"],
                        nmalive.OrderBy(a => a.territory_id).ThenBy(a => getDeltaMin(a.defeated_at)).ToArray(), [
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
                            info => ImGui.Text(info.oriname),
                            info => ImGui.Text(info.hp.ToString()),
                            info => ImGui.Text(getDeltaMin(info.appeared_at).ToString()),
                            info => ImGui.Text(getDeltaMin(info.defeated_at).ToString()),
                        ]);

                    ImGui.Text("已死亡");
                    NewTable(["地点", "名称",  "血量", "触发时间(min)", "击杀时间(min)"],
                        nmdead.OrderBy(a => a.territory_id).ThenBy(a => getDeltaMin(a.defeated_at)).ToArray(), [
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
                            info => ImGui.Text(info.oriname),
                            info => ImGui.Text(info.hp.ToString()),
                            info => ImGui.Text(getDeltaMin(info.appeared_at).ToString()),
                            info => ImGui.Text(getDeltaMin(info.defeated_at).ToString())
                        ]);
                }
                else {
                    StopWss();
                }
            });
            NewTab("测试", () => {
                if (ImGui.Button("测试")) {
                }
            });
        }
    }

    private static readonly List<string> regions = [
        "未选择",
        "陆行鸟",
        "莫古力",
        "猫小胖",
        "豆豆柴"
    ];

    private static readonly Vector4 green = new(0, 1, 0, 1);
    private static readonly Vector4 green_alt = new(0, 1, 0, 0.8f);
    private static readonly Vector4 red = new(1, 0, 0, 1);
    private static readonly Vector4 red_alt = new(1, 0, 0, 0.8f);
    private static readonly Vector4 blue = new(0, 0, 1, 1);
    private static readonly Vector4 blue_alt = new(0, 0, 1, 0.8f);
    private static readonly Vector4 cyan = new(0.3f, 0.3f, 1, 1);
    private static readonly Vector4 cyan_alt = new(0.3f, 0.3f, 1, 0.8f);

    private static bool _isWssRunning;
    private static CancellationTokenSource? _wssCts;

    private static async Task StartWssService(CancellationToken cancellationToken) {
        if (_isWssRunning) return;
        _isWssRunning = true;
        nmalive.Clear();
        nmdead.Clear();
        try {
            await RunWebSocketClient(cancellationToken);
        }
        finally {
            _isWssRunning = false;
        }
    }

    private record NmInfo {
        internal readonly string oriname;
        internal readonly string defeated_at;
        internal readonly string appeared_at;
        internal readonly int hp;
        internal readonly int territory_id;
        internal readonly string territory_name_ori;

        public NmInfo(JToken a) {
            var text = a["short_name"].ToString();
            oriname = a["name"].ToString();
            territory_name_ori = a["territory_name"].ToString();
            territory_id = territory_name_ori switch {
                "常风之地" => 1,
                "恒冰之地" => 2,
                "涌火之地" => 3,
                "丰水之地" => 4,
                _ => 0,
            };
            defeated_at = a["defeated_at"].ToString();
            appeared_at = a["appeared_at"].ToString();
            hp = int.Parse(a["hp"].ToString());
        }
    }

    private static long getT(string d) {
        try {
            var dateTime = DateTime.ParseExact(d, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            return DateTime.Now.Ticks;
        }
        catch {
            try {
                var dateTime = DateTime.ParseExact(d, "yyyy-MM-dd H:mm:ss", CultureInfo.InvariantCulture);
                return DateTime.Now.Ticks - dateTime.Ticks;
            }
            catch {
                return -1;
            }
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

    private const ImGuiTableFlags ImGuiTableFlag = ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg;

    private static void NewTable<T>(string[] header, T[] data, Action<T>[] acts) {
        if (ImGui.BeginTable("Table", acts.Length, ImGuiTableFlag)) {
            foreach (var item in header) {
                if (item == "") ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 24);
                else if (item.Contains("序号")) ImGui.TableSetupColumn(item, ImGuiTableColumnFlags.WidthFixed, 96);
                else ImGui.TableSetupColumn(item);
            }
            ImGui.TableHeadersRow();
            foreach (var res in data) {
                ImGui.TableNextRow();
                for (var i = 0; i < acts.Length; i++) {
                    ImGui.TableSetColumnIndex(i);
                    acts[i](res);
                }
            }
            ImGui.EndTable();
        }
    }

    private static readonly string[] pref = [
        "未知", "常风之地", "恒冰之地", "涌火之地", "丰水之地"
    ];
    private static readonly Lock WssLock = new();
    private static readonly List<NmInfo> nmalive = [];
    private static readonly List<NmInfo> nmdead = [];

    private static async Task RunWebSocketClient(CancellationToken cancellationToken) {
        using var client = new ClientWebSocket();
        try {
            Plugin.Log.Info($"wss://eureka-tracker.tunnel.tidebyte.com:8129/v1/{Plugin.Configuration.WssRegion}/ws");
            await client.ConnectAsync(new Uri($"wss://eureka-tracker.tunnel.tidebyte.com:8129/v1/{Plugin.Configuration.WssRegion}/ws"), cancellationToken);
            var buffer = new byte[4096];
            while (client.State == WebSocketState.Open) {
                try {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;
                    do {
                        result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                        if (result.MessageType == WebSocketMessageType.Close) {
                            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
                            break;
                        }
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);
                    if (result.MessageType != 0 || ms.Length == 0L) {
                        continue;
                    }
                    ms.Seek(0L, SeekOrigin.Begin);
                    using var reader = new StreamReader(ms, Encoding.UTF8);
                    var txt = await reader.ReadToEndAsync();
                    Plugin.Log.Info(txt);
                    var val = (JObject)JsonConvert.DeserializeObject(txt);
                    lock (WssLock) {
                        switch (val["type"].ToString()) {
                            case "initial": {
                                var val2 = val["data"];
                                foreach (var i in val2["active"]) {
                                    var x = nmalive.FirstOrDefault(a => i["name"].ToString() == a.oriname);
                                    var ni = new NmInfo(i);
                                    if (x == null) nmalive.Add(ni);
                                    else if (getT(ni.defeated_at) > getT(x.defeated_at)) {
                                        nmalive.Remove(x);
                                        nmalive.Add(ni);
                                    }
                                }
                                foreach (var i in val2["archive"]) {
                                    var x = nmdead.FirstOrDefault(a => i["name"].ToString() == a.oriname);
                                    var ni = new NmInfo(i);
                                    if (x == null) nmdead.Add(ni);
                                    else if (getT(ni.defeated_at) > getT(x.defeated_at)) {
                                        nmdead.Remove(x);
                                        nmdead.Add(ni);
                                    }
                                }
                                break;
                            }
                            case "keepalive":
                                continue;
                            case "active.update": {
                                var datau = val["data"];
                                var info2 = nmalive.FirstOrDefault(a => datau["name"].ToString() == a.oriname);
                                if (info2 != null) nmalive.Remove(info2);
                                nmalive.Add(new NmInfo(datau));
                                break;
                            }
                            case "active.archive": {
                               var dataa = val["data"];
                                var info3 = nmalive.FirstOrDefault(a => dataa["name"].ToString() == a.oriname);
                                if (info3 != null) nmalive.Remove(info3);
                                nmdead.Add(new NmInfo(dataa));
                                break;
                            }
                            case "active.add": {
                                var  dataadd = val["data"];
                                var x = nmalive.FirstOrDefault(a => dataadd["name"].ToString() == a.oriname);
                                var ni = new NmInfo(dataadd);
                                if (x == null) nmalive.Add(ni);
                                else if (getT(ni.defeated_at) > getT(x.defeated_at)) {
                                    nmalive.Remove(x);
                                    nmalive.Add(ni);
                                }
                                break;
                            }
                        }
                        // Detail(nmalive, nmdead);
                    }
                }
                catch (Exception value) {
                    Console.WriteLine(value);
                }
            }
        }
        finally {
            if (client.State == WebSocketState.Open)
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "服务终止", CancellationToken.None);
        }
    }

    internal static void StopWss() {
        try {
            _wssCts?.Cancel();
            _wssCts?.Dispose();
            _wssCts = null;
        }
        catch (Exception) {
            // ignored
        }
        _isWssRunning = false;
    }
}