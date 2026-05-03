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
using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Application.Network;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using SkyEye.Data;
using static System.StringComparison;
using static SkyEye.ConfigWindow;
using static SkyEye.Data.PData;
using static SkyEye.Util;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;
using Timer = System.Timers.Timer;

namespace SkyEye;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed partial class Plugin : IDalamudPlugin {
	private const uint LuckyCarrotItemId = 2002482;
	internal const int FarmTimeout = 50;
	private static float _lSpeed = 1f;
	internal static List<Vector3> DetectedTreasurePositions = [];
	internal static readonly List<IPlayerCharacter> OtherPlayer = [];
	internal static readonly List<Vector3> ElementalPositions = [];
	internal static readonly HashSet<uint> ElementalSet = [];
	private static IGameObject? _farmGameObject;
	internal static DateTime LastKill = DateTime.Now;
	private static readonly uint[] Loc = [21, 22];
	private static bool _locIter, _killing;
	private static readonly Lock KillingLock = new();
	internal static Vector3? lastFarmPos;
	internal static bool FarmFull;
	private static IntPtr? SpeedPtr;
	internal static MConfiguration.SpeedInfo? CurrentSpeedInfo;
	internal static Dictionary<string, string> MapInfo = new();
	private static Timer _carrotTimer = null!;
	private readonly ConfigWindow _configWindow;
	private readonly UiBuilder _uiBuilder;
	private bool mountState;
	// ReSharper disable once MemberCanBePrivate.Global
	public readonly WindowSystem WindowSystem = new("SkyEye");

	public unsafe Plugin() {
		Configuration = PluginInterface.GetPluginConfig() as MConfiguration ?? new MConfiguration();
		_uiBuilder = new UiBuilder();
		_configWindow = new ConfigWindow();
		WindowSystem.AddWindow(_configWindow);
		CommandManager.AddHandler("/skyeye", new CommandInfo(OnCommand) {
			HelpMessage = "打开主界面"
		});
		_carrotTimer = new Timer(7000) {
			AutoReset = true
		};
		_carrotTimer.Elapsed += (_, _) => {
			if (Configuration.AutoRabbit) UseCarrot();
			else StopCarrotTimer();
		};
		Ipcs.Init();
		Framework.Update += UpdateRoundPlayers;
		Framework.Update += Farm;
		Framework.Update += FindElemental;
		Framework.Update += CheckState;
		PluginInterface.UiBuilder.OpenConfigUi += OnCommand;
		PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
		ChatGui.ChatMessageUnhandled += ChatRabbit;
		if (Configuration.SpeedUp.Count == 0) {
			Configuration.SpeedUp.Add(MConfiguration.SpeedInfo.Default());
			Configuration.SpeedUp.Add(new MConfiguration.SpeedInfo());
		}
		MapInfo = DataManager.GetExcelSheet<TerritoryType>().Where(i => !i.PlaceNameRegion.Value.Name.IsEmpty)
			.ToDictionary(i => i.RowId.ToString(), i => $"{i.PlaceNameRegion.Value.Name}|{i.PlaceName.Value.Name}");
		foreach (var s in Configuration.SpeedUp.Where(s => s.Enabled && s.SpeedUpTerritory.Split('|').Contains(ClientState.TerritoryType.ToString()))) {
			CurrentSpeedInfo = s;
			break;
		}
		if (Configuration.NameReplacement) EnableNameplate();
		SetSpeed(1);
		SendPacketInternalHook ??= GameInteropProvider.HookFromSignature<SendPacketInternalDelegate>("48 83 EC ?? 48 8B 89 ?? ?? ?? ?? 48 85 C9 74 ?? 44 89 44 24 ?? 4C 8D 44 24 ?? 44 89 4C 24 ?? 44 0F B6 4C 24", SendPacketInternalDetour);
		SendPacketInternalHook.Enable();
	}

	internal unsafe delegate bool SendPacketInternalDelegate(ZoneClient* zoneClient, IntPtr packet, uint a3, uint a4, bool a5);

	internal static Hook<SendPacketInternalDelegate>? SendPacketInternalHook;

	private void CheckState(IFramework _) {
		if (!InArea() || Condition[ConditionFlag.Mounted] == mountState) return;
		mountState = Condition[ConditionFlag.Mounted];
		SetSpeed(1);
	}

	internal static unsafe bool SendPacketInternalDetour(ZoneClient* zoneClient, IntPtr packet, uint a1, uint a2, bool b) {
		if (Configuration.DropMovementPacket && Marshal.ReadByte(packet) == 0x09 && Marshal.ReadByte(packet + 1) == 0x01)
			return true;
		return SendPacketInternalHook!.Original(zoneClient, packet, a1, a2, b);
	}

	private void OnCommand() => OnCommand(null, null);
	public static MConfiguration Configuration { get; private set; } = null!;
	[PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
	[PluginService] internal static IClientState ClientState { get; private set; } = null!;
	[PluginService] internal static INotificationManager NotificationManager { get; private set; } = null!;
	[PluginService] private static IDataManager DataManager { get; set; } = null!;
	[PluginService] internal static IPluginLog Log { get; private set; } = null!;
	[PluginService] internal static ICondition Condition { get; private set; } = null!;
	[PluginService] internal static IGameGui Gui { get; private set; } = null!;
	[PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
	[PluginService] internal static IPartyList PartyList { get; private set; } = null!;
	[PluginService] internal static IFateTable Fates { get; private set; } = null!;
	[PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
	[PluginService] private static IChatGui ChatGui { get; set; } = null!;
	[PluginService] internal static IFramework Framework { get; private set; } = null!;
	[PluginService] internal static INamePlateGui NamePlate { get; private set; } = null!;
	[PluginService] private static ICommandManager CommandManager { get; set; } = null!;
	[PluginService] internal static IGameInteropProvider GameInteropProvider { get; set; } = null!;

	public void Dispose() {
		PluginInterface.UiBuilder.OpenConfigUi -= OnCommand;
		PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
		WindowSystem.RemoveAllWindows();
		ChatGui.ChatMessageUnhandled -= ChatRabbit;
		Framework.Update -= UpdateRoundPlayers;
		Framework.Update -= Farm;
		Framework.Update -= FindElemental;
		Framework.Update -= CheckState;
		DisableNameplate();
		SetSpeed(1);
		_uiBuilder.Dispose();
		CommandManager.RemoveHandler("/skyeye");
		_carrotTimer.Stop();
		_carrotTimer.Dispose();
		SendPacketInternalHook?.Disable();
		SendPacketInternalHook?.Dispose();
		WebSocket.StopWss();
	}

	private static unsafe void FindElemental(IFramework _) {
		if (!Configuration.PluginEnabled) return;
		if (ObjectTable.LocalPlayer is null || !InEureka()) return;
		IGameObject es;
		try {
			es = ObjectTable.First(obj => {
				if (obj.ObjectKind == ObjectKind.Pc) return false;
				var s = obj.Name.ToString();
				return s.Contains("风元灵") || s.Contains("冰元灵") || s.Contains("火元灵") || s.Contains("水元灵");
			});
		} catch (Exception) {
			return;
		}
		if (!ElementalSet.Add(es.EntityId)) return;
		var p = es.Position;
		ElementalPositions.Add(p);
		if (!PData.ElementalPositions[(Territory)ClientState.TerritoryType].Contains(p)) {
			if (!Configuration.AllElementalPositions.ContainsKey(ClientState.TerritoryType)) Configuration.AllElementalPositions[ClientState.TerritoryType] = [];
			Configuration.AllElementalPositions[ClientState.TerritoryType].Add(p);
			Configuration.Save();
		}
		AgentMap.Instance()->SetFlagMapMarker(ClientState.TerritoryType, ClientState.MapId, p);
		ChatBox.SendMessage("/e 找到元灵<se.1>");
	}

	internal static unsafe void FindRabbit(int fateidx = -1, bool force = false) {
		if (!force && (!Configuration.AutoRabbitWait || _carrotTimer is { Enabled: true } || Condition[ConditionFlag.InCombat] || FateManager.Instance()->SyncedFateId != 0 || wait4chest)) return;
		if (!string.IsNullOrEmpty(Configuration.BeforeGotoNewRabbit))
			ChatBox.SendMessage(Configuration.BeforeGotoNewRabbit);
		var territory = (Territory)ClientState.TerritoryType;
		if (fateidx != -1) {
			if (territory == Territory.Pagos && fateidx is 1367 or 1368 || territory == Territory.Pyros && fateidx is 1407 or 1408 || territory == Territory.Hydatos && fateidx is 1425) {
				var ret = XFates[territory].FirstOrDefault(i => i.FateId == fateidx);
				if (ret != null) {
					SetFlagAndMove(ret.FatePosition);
					return;
				}
			}
		}
		foreach (var ret in UiBuilder._eurekaLiveIdList2DOld
			         .Where(fateid => territory == Territory.Pagos && fateid is 1367 or 1368 || territory == Territory.Pyros && fateid is 1407 or 1408 || territory == Territory.Hydatos && fateid is 1425)
			         .Select(fateid => XFates[territory].FirstOrDefault(i => i.FateId == fateid))) {
			if (ret == null) continue;
			SetFlagAndMove(ret.FatePosition);
			return;
		}
	}


	private static unsafe void Farm(IFramework _) {
		if (!Configuration.PluginEnabled) return;
		if (ObjectTable.LocalPlayer is null || !Configuration.AutoFarm) return;
		if (!Ipcs.IsReady()) Ipcs.Init();
		var playerPos = ObjectTable.LocalPlayer.Position;
		var playerName = ObjectTable.LocalPlayer.Name.ToString();
		var allObjs = ObjectTable.Where(obj =>
			obj is { ObjectKind: ObjectKind.BattleNpc, IsDead: false } && obj.Name.ToString().Contains(Configuration.FarmTarget)).ToList();
		var validObjs = allObjs.Where(obj => lastFarmPos is null || Vector3.Distance(lastFarmPos.Value, obj.Position) < Configuration.FarmMaxDistance).ToList();
		var attracted = allObjs.Where(obj => obj.TargetObject != null && obj.TargetObject.Name.ToString().Contains(playerName)).ToArray();
		if (attracted.Length >= Configuration.FarmTargetMax) {
			FarmFull = true;
			Ipcs.Stop();
			return;
		}
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
			Ipcs.Stop();
			ChatBox.SendMessage($"/e 检测超时，正在尝试移动到{targ}");
			Telepo.Instance()->Teleport(targ, 0);
			LastKill = DateTime.Now;
		}
		var ieu = InEureka();
		foreach (var obj in validObjs.OrderBy(c => Vector3.Distance(playerPos, c.Position))) {
			if (obj.TargetObject != null) continue;
			if (ieu) {
				if (Vector3.Distance(playerPos, obj.Position) < 16) {
					TargetSystem.Instance()->SetHardTarget((GameObject*)obj.Address);
					ChatBox.SendMessage(Configuration.FarmStartCommand);
					if (attracted.Length == 0 || lastFarmPos == null)
						lastFarmPos = Configuration.FarmDistAlgo == 0 ? obj.Position : new Vector3(Configuration.FarmWaitX, Configuration.FarmWaitY, Configuration.FarmWaitZ);
					Ipcs.Stop();
					break;
				}
				if (!Ipcs.IsRunning()) Ipcs.PathfindAndMoveTo(obj.Position, false);
			} else {
				if (Ipcs.IsRunning()) {
					if ((DateTime.Now - LastKill).Seconds % 15 == 14) {
						Ipcs.Stop();
						Ipcs.PathfindAndMoveTo(obj.Position, true);
						if (!ObjectTable.LocalPlayer!.CurrentMount.HasValue) ChatBox.SendMessage("/ac 随机坐骑");
						LastKill = DateTime.Now;
					}
				}
				bool nk;
				lock (KillingLock) nk = _killing;
				if (nk) break;
				if (Vector3.Distance(playerPos, obj.Position) < 2) {
					lock (KillingLock) _killing = true;
					_farmGameObject = obj;
					TargetSystem.Instance()->SetHardTarget((GameObject*)obj.Address);
					new Task(Startkill).Start();
					break;
				}
				if (!ObjectTable.LocalPlayer!.CurrentMount.HasValue) ChatBox.SendMessage("/ac 随机坐骑");
				if (!Ipcs.IsRunning()) {
					Ipcs.PathfindAndMoveTo(obj.Position, true);
					LastKill = DateTime.Now;
				}
			}
		}
	}

	private static async void Startkill() {
		try {
			Ipcs.Stop();
			ChatBox.SendMessage("/e NewTask");
			if (ObjectTable.LocalPlayer!.CurrentMount.HasValue) {
				ChatBox.SendMessage("/ac 随机坐骑");
				await Task.Delay(1000);
			}
			ChatBox.SendMessage(Configuration.FarmStartCommand);
			await Task.Delay(500);
		} catch (Exception e) {
			Log.Error(e.ToString());
		} finally {
			lock (KillingLock) _killing = false;
		}
	}

	private static void StartCarrotTimer() {
		if (_carrotTimer.Enabled || !Configuration.AutoRabbit) return;
		UseCarrot();
		if (!string.IsNullOrEmpty(Configuration.BeforeFindTreasure))
			ChatBox.SendMessage(Configuration.BeforeFindTreasure);
		_carrotTimer.Start();
	}

	private static void StopCarrotTimer() {
		if (_carrotTimer is not { Enabled: true }) return;
		_carrotTimer.Stop();
	}

	private static unsafe void UseCarrot() {
		if (!Configuration.AutoRabbit) return;
		if (!InEureka()) {
			StopCarrotTimer();
			return;
		}
		if (InventoryManager.Instance()->GetInventoryItemCount(LuckyCarrotItemId) > 0)
			ActionManager.Instance()->UseAction(ActionType.EventItem, LuckyCarrotItemId, mode: ActionManager.UseActionMode.Queue);
		else {
			Log.Warning("没有幸运胡萝卜可用，停止自动使用");
			StopCarrotTimer();
		}
	}

	private void OnCommand(string? command, string? args) => _configWindow.Toggle();

	internal static bool InEureka() => ObjectTable.LocalPlayer != null && InEureka(ClientState.TerritoryType);
	internal static bool InEureka(uint id) => (Territory)id is Territory.Anemos or Territory.Pagos or Territory.Pyros or Territory.Hydatos;

	internal static bool InArea() => InEureka() || CurrentSpeedInfo != null;
	internal static Vector3 Pos2Map(Vector2 pos) => ToVector3(MapToWorld(pos, 200, 11f, (Territory)ClientState.TerritoryType == Territory.Hydatos ? 20.25f : 11.25f));

	internal static unsafe void SetFlagAndMove(Vector2 pos) {
		AgentMap.Instance()->SetFlagMapMarker(ClientState.TerritoryType, ClientState.MapId, Pos2Map(pos));
		var p = Ipcs.FlagToPoint();
		if (p.HasValue) Ipcs.PathfindAndMoveTo(p.Value, false);
	}

	private static bool wait4chest;

	private static void ChatRabbit(IChatMessage chatMessage) {
		if (!InEureka()) return;
		var msg = chatMessage.Message.TextValue.Trim();
		if (msg.StartsWith("找到了财宝，幸福兔心满意足地离去了。")) {
			DetectedTreasurePositions = [];
			wait4chest = true;
			StopCarrotTimer();
			foreach (var obj in ObjectTable) {
				if (obj is not { ObjectKind: ObjectKind.EventObj } || !obj.Name.ToString().Contains("财宝箱")) continue;
				unsafe {
					TargetSystem.Instance()->InteractWithObject((GameObject*)obj.Address);
				}
				var name = obj.Name.ToString();
				Configuration.TotalChest.TryAdd(name, 0);
				Configuration.TotalChest[name]++;
				Configuration.Save();
				if (!Configuration.AutoRabbitWait) continue;
				ChatBox.SendMessage("/e 等待7s后寻找下一个兔子");
				if (!string.IsNullOrEmpty(Configuration.AfterFindTreasure))
					ChatBox.SendMessage(Configuration.AfterFindTreasure);
				Task.Run(async () => {
					await Task.Delay(7000);
					wait4chest = false;
					FindRabbit(force: true);
				});
			}
			return;
		}
		var result = MyRegex().Match(msg);
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
		var playerPos = ObjectTable.LocalPlayer!.Position;
		var playerPos2D = new Vector2(playerPos.X, playerPos.Z);
		DetectedTreasurePositions = RabbitTreasurePositions[(Territory)ClientState.TerritoryType]
			.Select(i => (i, Vector2.Distance(playerPos2D, new Vector2(i.X, i.Z))))
			.OrderBy(c => c.Item2).Where(c => c.Item2 >= minDistance && c.Item2 <= maxDistance).Select(i => i.i).ToList();
		if (direction.Equals("正南", OrdinalIgnoreCase)) DetectedTreasurePositions = DetectedTreasurePositions.Where(c => c.Z > playerPos.Z && Math.Abs(c.X - playerPos.X) <= Math.Abs(c.Z - playerPos.Z)).ToList();
		else if (direction.Equals("正北", OrdinalIgnoreCase)) DetectedTreasurePositions = DetectedTreasurePositions.Where(c => c.Z < playerPos.Z && Math.Abs(c.X - playerPos.X) <= Math.Abs(c.Z - playerPos.Z)).ToList();
		else if (direction.Equals("正东", OrdinalIgnoreCase)) DetectedTreasurePositions = DetectedTreasurePositions.Where(c => c.X > playerPos.X && Math.Abs(c.X - playerPos.X) >= Math.Abs(c.Z - playerPos.Z)).ToList();
		else if (direction.Equals("正西", OrdinalIgnoreCase)) DetectedTreasurePositions = DetectedTreasurePositions.Where(c => c.X < playerPos.X && Math.Abs(c.X - playerPos.X) >= Math.Abs(c.Z - playerPos.Z)).ToList();
		else if (direction.Equals("东南", OrdinalIgnoreCase)) DetectedTreasurePositions = DetectedTreasurePositions.Where(c => c.Z >= playerPos.Z && c.X >= playerPos.X).ToList();
		else if (direction.Equals("西南", OrdinalIgnoreCase)) DetectedTreasurePositions = DetectedTreasurePositions.Where(c => c.Z >= playerPos.Z && c.X <= playerPos.X).ToList();
		else if (direction.Equals("东北", OrdinalIgnoreCase)) DetectedTreasurePositions = DetectedTreasurePositions.Where(c => c.Z <= playerPos.Z && c.X >= playerPos.X).ToList();
		else if (direction.Equals("西北", OrdinalIgnoreCase)) DetectedTreasurePositions = DetectedTreasurePositions.Where(c => c.Z <= playerPos.Z && c.X <= playerPos.X).ToList();
		var pos = DetectedTreasurePositions.FirstOrDefault();
		if (pos == default) {
			Log.Warning("无可用点位");
			return;
		}
		if (Configuration.AutoRabbit) Ipcs.PathfindAndMoveTo(pos, false);
	}

	internal static bool GreenNearby() {
		var friends = Configuration.SpeedUpFriendly.Split('|');
		return OtherPlayer.Any(i => !friends.Contains(i.Name.ToString()) && Vector3.Distance(i.Position, ObjectTable.LocalPlayer!.Position) < (110 ^ 2));
	}

	private static void UpdateRoundPlayers(IFramework _) {
		if (!Configuration.PluginEnabled || ObjectTable.LocalPlayer == null || !InArea() || CurrentSpeedInfo == null) return;
		OtherPlayer.Clear();
		foreach (var obj in ObjectTable)
			if (obj.GameObjectId != ObjectTable.LocalPlayer.GameObjectId & obj.Address.ToInt64() != 0 && obj is IPlayerCharacter rcTemp)
				OtherPlayer.Add(rcTemp);
		SetSpeed(!Configuration.SpeedUpEnabled || GreenNearby() ? 1f : CurrentSpeedInfo.SpeedUpN);
	}

	// https://github.com/Jaksuhn/ffxiv-bundleoftweaks
	// https://github.com/MnFeN/Triggernometry
	[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
	internal static void SetSpeed(float speedBase) {
		if (CurrentSpeedInfo == null || !Configuration.SpeedUpEnabled) return;
		var mounted = Condition[ConditionFlag.Mounted];
		if (mounted) speedBase *= InEureka() ? 20f / 12 : 2;
		if (_lSpeed == speedBase) return;
		_lSpeed = speedBase;
		if (SpeedPtr == null) {
			var ba = SigScanner.ScanText("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 84 C0 75 4F") + 3;
			SpeedPtr = ba + Marshal.ReadInt32(ba) + 4 + 0x58;
		}
		var finalspeed = Math.Min(CurrentSpeedInfo.SpeedUpMax, speedBase * 6);
		ChatBox.SendMessage($"/e SetSpeed({(mounted ? InEureka() ? 10 : 12 : 6)}x): {SafeMemory.Read<float>(SpeedPtr.Value, 1)![0]}->{finalspeed}");
		SafeMemory.Write(SpeedPtr.Value, finalspeed);
	}

	[GeneratedRegex("^财宝好像是在(?<direction>正北|东北|正东|东南|正南|西南|正西|西北)方向(?<distance>(很远|稍远|不远|很近))的地方！")]
	private static partial Regex MyRegex();
}