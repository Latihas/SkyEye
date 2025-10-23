using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static SkyEye.SkyEye.Util;

namespace SkyEye.SkyEye;

internal static class WebSocket {
    internal static bool _isWssRunning;
    private static CancellationTokenSource? _wssCts;

    private static readonly Lock WssLock = new();
    internal static readonly List<NmInfo> nmalive = [];
    internal static readonly List<NmInfo> nmdead = [];

    internal static async Task StartWssService() {
        if (_isWssRunning) return;
        _wssCts ??= new CancellationTokenSource();
        _isWssRunning = true;
        nmalive.Clear();
        nmdead.Clear();
        try {
            await RunWebSocketClient(_wssCts.Token);
        }
        finally {
            _isWssRunning = false;
        }
    }


    internal static void StopWss() {
        _wssCts?.Cancel();
        _wssCts?.Dispose();
        _wssCts = null;
        _isWssRunning = false;
    }

    private static bool InWssNotify(string name) => Plugin.Configuration.WssNotify.Split("|").Any(f => !f.IsNullOrEmpty() && name.Contains(f));

    private static void Notify(string name, bool sound = true) {
        if (!InWssNotify(name)) return;
        Plugin.Framework.RunOnFrameworkThread(() => ChatBox.SendMessage($"/e 史书提醒：{name}"));
        if (sound) UiBuilder.NmFound();
    }

    private static async Task RunWebSocketClient(CancellationToken cancellationToken) {
        using var client = new ClientWebSocket();
        try {
            Plugin.Log.Info($"wss://eureka-tracker.tunnel.tidebyte.com:8129/v1/{Plugin.Configuration.WssRegion}/ws");
            await client.ConnectAsync(new Uri($"wss://eureka-tracker.tunnel.tidebyte.com:8129/v1/{Plugin.Configuration.WssRegion}/ws"), cancellationToken);
            var buffer = new byte[4096];
            while (client.State == WebSocketState.Open) {
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
                if (result.MessageType != 0 || ms.Length == 0) continue;
                ms.Seek(0L, SeekOrigin.Begin);
                using var reader = new StreamReader(ms, Encoding.UTF8);
                var txt = await reader.ReadToEndAsync(cancellationToken);
                Plugin.Log.Info(txt);
                var val = (JObject)JsonConvert.DeserializeObject(txt)!;
                lock (WssLock) {
                    var vt = val["type"]!.ToString();
                    if (vt == "keepalive") continue;
                    var vdata = val["data"]!;
                    switch (vt) {
                        case "initial": {
                            foreach (var i in vdata["active"]!) {
                                var x = nmalive.FirstOrDefault(a => i["name"]!.ToString() == a.oriname);
                                var ni = new NmInfo(i);
                                if (x == null) nmalive.Add(ni);
                                else if (getT(ni.defeated_at) > getT(x.defeated_at)) {
                                    nmalive.Remove(x);
                                    nmalive.Add(ni);
                                }
                                Notify(ni.oriname, false);
                            }
                            foreach (var i in vdata["archive"]!) {
                                var x = nmdead.FirstOrDefault(a => i["name"]!.ToString() == a.oriname);
                                var ni = new NmInfo(i);
                                if (x == null) nmdead.Add(ni);
                                else if (getT(ni.defeated_at) > getT(x.defeated_at)) {
                                    nmdead.Remove(x);
                                    nmdead.Add(ni);
                                }
                            }
                            Plugin.Framework.RunOnFrameworkThread(() => ChatBox.SendMessage("/e 史书初始化完成"));
                            break;
                        }
                        case "active.update": {
                            var info2 = nmalive.FirstOrDefault(a => vdata["name"]!.ToString() == a.oriname);
                            if (info2 != null) nmalive.Remove(info2);
                            var ni = new NmInfo(vdata);
                            nmalive.Add(ni);
                            Notify($"{ni.oriname}({ni.hp}%)", false);
                            break;
                        }
                        case "active.archive": {
                            var info3 = nmalive.FirstOrDefault(a => vdata["name"]!.ToString() == a.oriname);
                            if (info3 != null) nmalive.Remove(info3);
                            var info4 = nmdead.FirstOrDefault(a => vdata["name"]!.ToString() == a.oriname);
                            if (info4 != null) nmalive.Remove(info4);
                            var ni = new NmInfo(vdata);
                            nmdead.Add(new NmInfo(vdata));
                            Notify($"{ni.oriname}已死亡", false);
                            break;
                        }
                        case "active.add": {
                            var x = nmalive.FirstOrDefault(a => vdata["name"]!.ToString() == a.oriname);
                            var ni = new NmInfo(vdata);
                            if (x == null) {
                                nmalive.Add(ni);
                                Notify($"{ni.oriname}已触发");
                            }
                            else if (getT(ni.defeated_at) > getT(x.defeated_at)) {
                                nmalive.Remove(x);
                                nmalive.Add(ni);
                                Notify($"{ni.oriname}已触发");
                            }
                            break;
                        }
                    }
                }
            }
        }
        finally {
            if (client.State == WebSocketState.Open)
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "服务终止", CancellationToken.None);
        }
    }

    internal record NmInfo {
        internal readonly string appeared_at, defeated_at, oriname, territory_name_ori;
        internal readonly int hp, territory_id;

        internal NmInfo(JToken a) {
            oriname = a["name"]!.ToString();
            territory_name_ori = a["territory_name"]!.ToString();
            territory_id = territory_name_ori switch {
                "常风之地" => 1,
                "恒冰之地" => 2,
                "涌火之地" => 3,
                "丰水之地" => 4,
                _ => 0
            };
            defeated_at = a["defeated_at"]!.ToString();
            appeared_at = a["appeared_at"]!.ToString();
            hp = int.Parse(a["hp"]!.ToString());
        }
    }
}