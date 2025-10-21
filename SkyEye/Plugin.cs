using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dalamud;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using SkyEye.SkyEye.Data;
using static System.StringComparison;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;
using Timer = System.Timers.Timer;

namespace SkyEye.SkyEye;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class Plugin : IDalamudPlugin {
    private const uint LuckyCarrotItemId = 2002482;
    internal const int FarmTimeout = 50;
    private static float _lSpeed = 1f, _dspeed = 1f;
    internal static List<Vector3> DetectedTreasurePositions = [];
    internal static readonly List<IPlayerCharacter> OtherPlayer = [];
    internal static readonly List<Vector3> YlPositions = [];
    internal static readonly HashSet<uint> Yl = [];
    private static IGameObject? _farmGameObject;
    internal static DateTime LastKill = DateTime.Now;
    private static readonly uint[] Loc = [21, 22];
    private static bool _locIter, _killing;
    private static readonly Lock KillingLock = new();
    internal static Vector3? lastFarmPos;
    internal static bool FarmFull;

    private static IntPtr? SpeedPtr;
    private readonly Timer _carrotTimer;
    private readonly ConfigWindow _configWindow;
    private readonly Lock _speedLock = new();
    private readonly UiBuilder _uiBuilder;
    // ReSharper disable once MemberCanBePrivate.Global
    public readonly WindowSystem WindowSystem = new("SkyEye");

    public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager) {
        PluginInterface = pluginInterface;
        CommandManager = commandManager;
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        _uiBuilder = new UiBuilder();
        _configWindow = new ConfigWindow();
        WindowSystem.AddWindow(_configWindow);
        CommandManager.AddHandler("/skyeye", new CommandInfo(OnCommand) {
            HelpMessage = "打开主界面"
        });
        _carrotTimer = new Timer(7000);
        _carrotTimer.AutoReset = true;
        _carrotTimer.Elapsed += (_, _) => {
            if (Configuration.AutoRabbit) UseCarrot();
            else StopCarrotTimer();
        };
        NavmeshIpc.Init();
        PluginInterface.UiBuilder.Draw += () => WindowSystem.Draw();
        Framework.Update += UpdateRoundPlayers;
        Framework.Update += Farm;
        Framework.Update += FindYl;
        PluginInterface.UiBuilder.OpenConfigUi += () => OnCommand(null, null);
        ChatGui.ChatMessageUnhandled += ChatRabbit;
    }

    public static Configuration Configuration { get; private set; } = null!;
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static IClientState ClientState { get; private set; } = null!;

    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;

    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    [PluginService] internal static ICondition Condition { get; private set; } = null!;

    [PluginService] internal static IGameGui Gui { get; private set; } = null!;

    [PluginService] private static IObjectTable Objects { get; set; } = null!;

    [PluginService] internal static IFateTable Fates { get; private set; } = null!;

    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;

    [PluginService] private static IChatGui ChatGui { get; set; } = null!;

    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] private static ICommandManager CommandManager { get; set; } = null!;

    public void Dispose() {
        WindowSystem.RemoveAllWindows();
        ChatGui.ChatMessageUnhandled -= ChatRabbit;
        Framework.Update -= UpdateRoundPlayers;
        Framework.Update -= Farm;
        Framework.Update -= FindYl;
        _uiBuilder.Dispose();
        CommandManager.RemoveHandler("/skyeye");
        _carrotTimer.Stop();
        _carrotTimer.Dispose();
        WebSocket.StopWss();
    }

    private static void FindYl(IFramework framework) {
        if (!Configuration.PluginEnabled) return;
        if (ClientState.LocalPlayer is null || !InEureka()) return;
        IGameObject yls;
        try {
            yls = Objects.First(obj => obj.Name.ToString().Contains("元灵") && obj.ObjectKind != ObjectKind.Player);
        }
        catch (Exception) {
            return;
        }
        if (!Yl.Add(yls.EntityId)) return;
        var p = yls.Position;
        YlPositions.Add(p);
        unsafe {
            AgentMap.Instance()->SetFlagMapMarker(ClientState.TerritoryType, ClientState.MapId, p);
        }
        ChatBox.SendMessage("/e 找到元灵<se.1>");
    }


    private static void Farm(IFramework framework) {
        if (!Configuration.PluginEnabled) return;
        if (ClientState.LocalPlayer is null || !Configuration.AutoFarm) return;
        if (!NavmeshIpc.IsReady()) NavmeshIpc.Init();
        var playerPos = ClientState.LocalPlayer.Position;
        var playerName = ClientState.LocalPlayer.Name.ToString();
        var validObjs = Objects.Where(obj =>
            obj is { ObjectKind: ObjectKind.BattleNpc, IsDead: false } && obj.Name.ToString().Contains(Configuration.FarmTarget) && (lastFarmPos is null || Vector3.Distance(lastFarmPos.Value, obj.Position) < Configuration.FarmMaxDistance)).ToList();
        var attracted = validObjs.Where(obj => obj.TargetObject != null && obj.TargetObject.Name.ToString().Contains(playerName)).ToArray();
        if (attracted.Length >= Configuration.FarmTargetMax) {
            FarmFull = true;
            NavmeshIpc.Stop();
            return;
        }
        if (validObjs.Count == 0 && NavmeshIpc.IsRunning()) NavmeshIpc.Stop();
        if (attracted.Length == 0) {
            lastFarmPos = null;
            FarmFull = false;
        }
        if (Configuration.FarmWait && FarmFull) return;
        if (_farmGameObject != null) {
            if (!_farmGameObject.IsValid()) _farmGameObject = null;
            else if (_farmGameObject.IsDead) {
                LastKill = DateTime.Now;
                _farmGameObject = null;
            }
        }
        if (ClientState.TerritoryType == 147 && (DateTime.Now - LastKill).Seconds > FarmTimeout) {
            _locIter = !_locIter;
            var targ = Loc[_locIter ? 1 : 0];
            NavmeshIpc.Stop();
            ChatBox.SendMessage($"/e 检测超时，正在尝试移动到{targ}");
            unsafe {
                Telepo.Instance()->Teleport(targ, 0);
            }
            LastKill = DateTime.Now;
        }
        var ieu = InEureka();
        foreach (var obj in validObjs.OrderBy(c => Vector3.Distance(playerPos, c.Position))) {
            if (obj.TargetObject != null) continue;
            if (ieu) {
                if (Vector3.Distance(playerPos, obj.Position) < 15) {
                    unsafe {
                        TargetSystem.Instance()->SetHardTarget((GameObject*)obj.Address);
                    }
                    ChatBox.SendMessage(Configuration.FarmStartCommand);
                    if (attracted.Length == 0)
                        lastFarmPos = Configuration.FarmDistAlgo == 0 ? obj.Position : new Vector3(Configuration.FarmWaitX, Configuration.FarmWaitY, Configuration.FarmWaitZ);
                    NavmeshIpc.Stop();
                    break;
                }
                if (!NavmeshIpc.IsRunning()) NavmeshIpc.PathfindAndMoveTo(obj.Position, false);
            }
            else {
                if (NavmeshIpc.IsRunning()) {
                    if ((DateTime.Now - LastKill).Seconds % 15 == 14) {
                        NavmeshIpc.Stop();
                        NavmeshIpc.PathfindAndMoveTo(obj.Position, true);
                        if (!ClientState.LocalPlayer!.CurrentMount.HasValue) ChatBox.SendMessage("/ac 随机坐骑");
                        LastKill = DateTime.Now;
                    }
                }
                bool nk;
                lock (KillingLock) nk = _killing;
                if (nk) break;
                if (Vector3.Distance(playerPos, obj.Position) < 2) {
                    lock (KillingLock) _killing = true;
                    _farmGameObject = obj;
                    unsafe {
                        TargetSystem.Instance()->SetHardTarget((GameObject*)obj.Address);
                    }
                    new Task(Startkill).Start();
                    break;
                }
                if (!ClientState.LocalPlayer!.CurrentMount.HasValue) ChatBox.SendMessage("/ac 随机坐骑");
                if (!NavmeshIpc.IsRunning()) {
                    NavmeshIpc.PathfindAndMoveTo(obj.Position, true);
                    LastKill = DateTime.Now;
                }
            }
        }
    }

    private static async void Startkill() {
        try {
            NavmeshIpc.Stop();
            await Framework.RunOnFrameworkThread(() => ChatBox.SendMessage("/e NewTask"));
            if (ClientState.LocalPlayer!.CurrentMount.HasValue) {
                await Framework.RunOnFrameworkThread(() => ChatBox.SendMessage("/ac 随机坐骑"));
                await Task.Delay(1000);
            }
            await Framework.RunOnFrameworkThread(() => ChatBox.SendMessage(Configuration.FarmStartCommand));
            await Task.Delay(500);
        }
        catch (Exception e) {
            Log.Error(e.ToString());
        }
        finally {
            lock (KillingLock) _killing = false;
        }
    }

    private void StartCarrotTimer() {
        if (_carrotTimer.Enabled || !Configuration.AutoRabbit) return;
        UseCarrot();
        _carrotTimer.Start();
    }

    private void StopCarrotTimer() {
        if (_carrotTimer is not { Enabled: true }) return;
        _carrotTimer.Stop();
    }

    private void UseCarrot() {
        if (!Configuration.AutoRabbit) return;
        if (!InEureka()) {
            StopCarrotTimer();
            return;
        }
        unsafe {
            var inventoryManager = InventoryManager.Instance();
            var itemCount = inventoryManager->GetInventoryItemCount(LuckyCarrotItemId);
            if (itemCount > 0) ActionManager.Instance()->UseAction(ActionType.KeyItem, LuckyCarrotItemId, mode: ActionManager.UseActionMode.Queue);
            else {
                Log.Warning("没有幸运胡萝卜可用，停止自动使用");
                StopCarrotTimer();
            }
        }
    }

    private void OnCommand(string? command, string? args) => _configWindow.Toggle();


    internal static bool InEureka() => ClientState is { LocalPlayer: not null, TerritoryType: 732 or 763 or 795 or 827 };

    internal static bool InArea()
        => InEureka() || Configuration.SpeedUpTerritory.Split('|').Contains(ClientState.TerritoryType.ToString());


    private void ChatRabbit(XivChatType type, int timestamp, SeString sender, SeString message) {
        if (!InEureka()) return;
        var msg = message.TextValue.Trim();
        if (msg.StartsWith("找到了财宝，幸福兔心满意足地离去了。")) {
            DetectedTreasurePositions = [];
            StopCarrotTimer();
            foreach (var obj in Objects)
                try {
                    if (obj is not { ObjectKind: ObjectKind.EventObj } || !obj.Name.ToString().Contains("财宝箱")) continue;
                    unsafe {
                        TargetSystem.Instance()->InteractWithObject((GameObject*)obj.Address);
                    }
                    if (!Configuration.AutoRabbitWait) continue;
                    ChatBox.SendMessage("/e 等待7s后返回");
                    Task.Run(async () => {
                        await Task.Delay(7000);
                        NavmeshIpc.PathfindAndMoveTo(new Vector3(Configuration.RabbitWaitX, Configuration.RabbitWaitY, Configuration.RabbitWaitZ), false);
                    });
                }
                catch (Exception) {
                    Log.Error("error");
                }
            return;
        }
        var result = Regex.Match(msg, "^财宝好像是在(?<direction>正北|东北|正东|东南|正南|西南|正西|西北)方向(?<distance>(很远|稍远|不远|很近))的地方！");
        if (!(result.Success || msg.StartsWith("幸福兔看起来很喜欢你。"))) return;
        StartCarrotTimer();
        var direction = result.Groups["direction"].Value;
        int minDistance, maxDistance;
        switch (result.Groups["distance"].Value) {
            case "很远":
                minDistance = 200;
                maxDistance = int.MaxValue;
                break;
            case "稍远":
                minDistance = 100;
                maxDistance = 200;
                break;
            case "不远":
                minDistance = 25;
                maxDistance = 100;
                break;
            default:
                minDistance = 0;
                maxDistance = 25;
                break;
        }
        var playerPos = ClientState.LocalPlayer!.Position;
        if (DetectedTreasurePositions.Count == 0)
            DetectedTreasurePositions = PData.RabbitTreasurePositions[ClientState.TerritoryType]
                .Where(delegate(Vector3 c) {
                    var num = Vector3.Distance(playerPos, c);
                    return num >= minDistance && num <= maxDistance;
                })
                .OrderBy(c => Vector3.Distance(playerPos, c)).ToList();
        if (direction.Equals("正南", OrdinalIgnoreCase)) DetectedTreasurePositions = DetectedTreasurePositions.Where(c => c.Z > playerPos.Z && Math.Abs(c.X - playerPos.X) <= Math.Abs(c.Z - playerPos.Z)).ToList();
        else if (direction.Equals("正北", OrdinalIgnoreCase)) DetectedTreasurePositions = DetectedTreasurePositions.Where(c => c.Z < playerPos.Z && Math.Abs(c.X - playerPos.X) <= Math.Abs(c.Z - playerPos.Z)).ToList();
        else if (direction.Equals("正东", OrdinalIgnoreCase)) DetectedTreasurePositions = DetectedTreasurePositions.Where(c => c.X > playerPos.X && Math.Abs(c.X - playerPos.X) >= Math.Abs(c.Z - playerPos.Z)).ToList();
        else if (direction.Equals("正西", OrdinalIgnoreCase)) DetectedTreasurePositions = DetectedTreasurePositions.Where(c => c.X < playerPos.X && Math.Abs(c.X - playerPos.X) >= Math.Abs(c.Z - playerPos.Z)).ToList();
        else if (direction.Equals("东南", OrdinalIgnoreCase)) DetectedTreasurePositions = DetectedTreasurePositions.Where(c => c.Z >= playerPos.Z && c.X >= playerPos.X).ToList();
        else if (direction.Equals("西南", OrdinalIgnoreCase)) DetectedTreasurePositions = DetectedTreasurePositions.Where(c => c.Z >= playerPos.Z && c.X <= playerPos.X).ToList();
        else if (direction.Equals("东北", OrdinalIgnoreCase)) DetectedTreasurePositions = DetectedTreasurePositions.Where(c => c.Z <= playerPos.Z && c.X >= playerPos.X).ToList();
        else if (direction.Equals("西北", OrdinalIgnoreCase)) DetectedTreasurePositions = DetectedTreasurePositions.Where(c => c.Z <= playerPos.Z && c.X <= playerPos.X).ToList();

        if (Configuration.AutoRabbit) {
            float maxd = 114514;
            Vector3? point = null;
            foreach (var treasure in DetectedTreasurePositions) {
                var d = Vector3.Distance(ClientState.LocalPlayer.Position, treasure);
                if (d > maxd) continue;
                maxd = d;
                point = treasure;
            }
            if (point != null) {
                if (!NavmeshIpc.IsEnabled || !NavmeshIpc.IsReady()) {
                    Log.Error("vnavmesh插件异常，尝试重新初始化");
                    NavmeshIpc.Init();
                    return;
                }
                NavmeshIpc.PathfindAndMoveTo(point.Value, false);
            }
        }
    }


    private void UpdateRoundPlayers(IFramework framework) {
        if (!Configuration.PluginEnabled) return;
        if (ClientState.LocalPlayer == null) return;
        if (!InArea()) {
            lock (_speedLock) SetSpeed(1);
            return;
        }
        lock (OtherPlayer) {
            OtherPlayer.Clear();
            foreach (var obj in Objects)
                try {
                    if (obj.GameObjectId != ClientState.LocalPlayer.GameObjectId & obj.Address.ToInt64() != 0 && obj is IPlayerCharacter rcTemp) OtherPlayer.Add(rcTemp);
                }
                catch (Exception) {
                    Log.Error("error");
                }
        }
        lock (_speedLock) {
            if (Configuration.SpeedUpEnabled) {
                var friends = Configuration.SpeedUpFriendly.Split('|');
                _dspeed = OtherPlayer.Any(i => !friends.Contains(i.Name.ToString()) && Vector3.Distance(i.Position, ClientState.LocalPlayer.Position) < (110 ^ 2)) ? 1f : Configuration.SpeedUpN;
            }
            else _dspeed = 1f;
            SetSpeed(_dspeed);
        }
    }

    // https://github.com/Jaksuhn/ffxiv-bundleoftweaks
    // https://github.com/MnFeN/Triggernometry
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    internal static void SetSpeed(float speedBase) {
        if (_lSpeed == speedBase) return;
        _lSpeed = speedBase;
        if (SpeedPtr == null) {
            var ba = SigScanner.ScanText("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 84 C0 75 4F") + 3;
            SpeedPtr = ba + Marshal.ReadInt32(ba) + 4 + 0x58;
        }
        var finalspeed = speedBase * 6;
        SafeMemory.Write(SpeedPtr.Value, finalspeed);
        ChatBox.SendMessage($"/e SetSpeed(6x): {SafeMemory.Read<float>(SpeedPtr.Value, 1)![0]}->{finalspeed}");
    }
}