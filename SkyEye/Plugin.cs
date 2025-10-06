using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
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
using SkyEye.SkyEye.Data;
using static System.StringComparison;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;
using Timer = System.Timers.Timer;

namespace SkyEye.SkyEye;

public sealed class Plugin : IDalamudPlugin {
    private const uint LuckyCarrotItemId = 2002482;
    private static float _lSpeed = 1f;
    private static ChatBox _chatBox;
    internal static List<Vector3> DetectedTreasurePositions = [];
    internal static readonly List<IPlayerCharacter> OtherPlayer = [];
    private readonly Timer _carrotTimer;
    private readonly ConfigWindow _configWindow;
    private readonly Lock _speedLock = new();
    private readonly UiBuilder _uiBuilder;
    public readonly WindowSystem WindowSystem = new("SkyEye");
    private float _dspeed = 1f;

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
        Framework.Update += _ => {
            if (ClientState.LocalPlayer is null) return;
            var playerPos = ClientState.LocalPlayer.Position;
            var playerName = ClientState.LocalPlayer.Name.ToString();
            if (Configuration.AutoFarm) {
                var validObjs = Objects.Where(obj => !obj.IsDead && obj.Name.ToString().Contains(Configuration.FarmTarget)).ToList();
                var attracted = validObjs.Count(obj => obj.TargetObject != null && obj.TargetObject.Name.ToString().Contains(playerName));
                if (attracted < Configuration.FarmTargetMax) {
                    foreach (var obj in validObjs.OrderBy(c => Vector3.Distance(playerPos, c.Position))) {
                        if (obj.TargetObject == null) {
                            if (!NavmeshIpc.IsRunning()) NavmeshIpc.PathfindAndMoveTo(obj.Position, false);
                            if (Vector3.Distance(playerPos, obj.Position) < 5) {
                                unsafe {
                                    TargetSystem.Instance()->SetHardTarget((GameObject*)obj.Address);
                                    ChatBox.SendMessage(Configuration.FarmStartCommand);
                                }
                            }
                            break;
                        }
                    }
                }
            }
        };
        PluginInterface.UiBuilder.OpenConfigUi += () => OnCommand(null, null);
        ChatGui.ChatMessageUnhandled += OnChatMessage;
    }

    internal static ChatBox ChatBox => _chatBox ??= new ChatBox();
    public static Configuration Configuration { get; private set; }
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    [PluginService] public static IClientState ClientState { get; private set; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    [PluginService] internal static IDataManager DataManager { get; private set; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    [PluginService] internal static IPluginLog Log { get; private set; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    [PluginService] internal static ICondition Condition { get; private set; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    [PluginService] internal static IGameGui Gui { get; private set; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    [PluginService] internal static IObjectTable Objects { get; private set; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    [PluginService] internal static IFateTable Fates { get; private set; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    [PluginService] internal static ISigScanner SigScanner { get; private set; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    [PluginService] private static IChatGui ChatGui { get; set; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    [PluginService] private static IFramework Framework { get; set; }
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
    [PluginService] private ICommandManager CommandManager { get; set; }

    public void Dispose() {
        WindowSystem.RemoveAllWindows();
        ChatGui.ChatMessageUnhandled -= OnChatMessage;
        Framework.Update -= UpdateRoundPlayers;
        _uiBuilder.Dispose();
        CommandManager.RemoveHandler("/skyeye");
        _carrotTimer.Stop();
        _carrotTimer.Dispose();
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

    private void OnCommand(string command, string args) => _configWindow.Toggle();


    internal static bool InEureka() => ClientState.LocalPlayer != null && ClientState.TerritoryType is 732 or 763 or 795 or 827;

    internal static bool InArea()
        => InEureka() || Configuration.SpeedUpTerritory.Split('|').Contains(ClientState.TerritoryType.ToString());


    private void OnChatMessage(XivChatType type, int timestamp, SeString sender, SeString message) {
        if (message == null || !InEureka()) return;
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
                if (d < maxd) {
                    maxd = d;
                    point = treasure;
                }
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
        if (ClientState.LocalPlayer == null) return;
        if (!InArea()) {
            lock (_speedLock) SetSpeed(1f);
            return;
        }
        lock (OtherPlayer) {
            OtherPlayer.Clear();
            foreach (var obj in Objects)
                try {
                    if (obj != null && obj.GameObjectId != ClientState.LocalPlayer.GameObjectId & obj.Address.ToInt64() != 0 && obj is IPlayerCharacter rcTemp) OtherPlayer.Add(rcTemp);
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

    public static void SetSpeed(float speedBase) {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (_lSpeed == speedBase) return;
        _lSpeed = speedBase;
        ChatBox.SendMessage($"/pdrspeed {_lSpeed}");
    }
}