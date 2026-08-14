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
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
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
using static SkyEye.MConfiguration;
using static SkyEye.Util;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;
using Timer = System.Timers.Timer;

namespace SkyEye;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed partial class Plugin : IDalamudPlugin {
	private const uint LuckyCarrotItemId = 2002482;
	private const uint LuckyPotItemId = 2003296;
	private const int CurrentConfigurationVersion = 1;
	private const string BaseMovementSpeedSignature = "48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 84 C0 75 4F";
	private const int BaseMovementSpeedOffset = 0x58;
	private const float SpeedComparisonTolerance = 0.001f;
	private const long SpeedRetryDelayMilliseconds = 1000;
	internal const int FarmTimeout = 50;
	private static float _lSpeed = float.NaN;
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
	private static IntPtr? BaseMovementSpeedPtr;
	private static IntPtr _overriddenSpeedAddress;
	private static float _originalSpeed, _restoreWalkingBaseSpeed, _restoreMountedBaseSpeed;
	private static long _speedRetryAfter;
	private static bool _speedOverridden, _speedScanAttempted, _speedFailureLogged, _overrideMounted;
	internal static SpeedInfo? CurrentSpeedInfo;
	internal static Dictionary<string, string> MapInfo = new();
	private static Timer _carrotTimer = null!, _potTimer = null!;
	private readonly ConfigWindow _configWindow;
	private readonly UiBuilder _uiBuilder;
	private bool mountState;
	// ReSharper disable once MemberCanBePrivate.Global
	public readonly WindowSystem WindowSystem = new("SkyEye");

	public Plugin() {
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
		_potTimer = new Timer(7000) {
			AutoReset = true
		};
		_carrotTimer.Elapsed += (_, _) => {
			if (Configuration.AutoRabbit) UseCarrot();
			else StopCarrotTimer();
		};
		_potTimer.Elapsed += (_, _) => {
			if (Configuration.AutoPot) UsePotCarrot();
			else StopPotTimer();
		};
		mountState = Condition[ConditionFlag.Mounted];
		Framework.Update += CheckState;
		Framework.Update += UpdateRoundPlayers;
		Framework.Update += Farm;
		Framework.Update += FindElemental;
		PluginInterface.UiBuilder.OpenConfigUi += OnCommand;
		PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
		ChatGui.ChatMessageUnhandled += ChatRabbit;
		ChatGui.ChatMessageUnhandled += ChatPot;
		ChatGui.ChatMessageUnhandled += ChatMoonPack;
		ChatGui.ChatMessageUnhandled += Chat30OccultTreasure;
		var configChanged = MigrateConfiguration();
		configChanged |= EnsureDefaultSpeedInfo();
		configChanged |= DisableAutoTreasureOnTerritoryEnter(ClientState.TerritoryType);
		if (configChanged) Configuration.Save();
		MapInfo = DataManager.GetExcelSheet<TerritoryType>().Where(i => !i.PlaceNameRegion.Value.Name.IsEmpty)
			.ToDictionary(i => i.RowId.ToString(), i => $"{i.PlaceNameRegion.Value.Name}|{i.PlaceName.Value.Name}");
		RefreshCurrentSpeedInfo(ClientState.TerritoryType);
		if (Configuration.NameReplacement) EnableNameplate();
	}

	private void CheckState(IFramework _) {
		if (Condition[ConditionFlag.Mounted] == mountState) return;
		mountState = Condition[ConditionFlag.Mounted];
		RestoreSpeed(true);
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
		isFindingTreasure = false;
		isFindingMoonPack = false;
		PluginInterface.UiBuilder.OpenConfigUi -= OnCommand;
		PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
		WindowSystem.RemoveAllWindows();
		ChatGui.ChatMessageUnhandled -= ChatRabbit;
		ChatGui.ChatMessageUnhandled -= ChatPot;
		ChatGui.ChatMessageUnhandled -= ChatMoonPack;
		ChatGui.ChatMessageUnhandled -= Chat30OccultTreasure;
		Framework.Update -= UpdateRoundPlayers;
		Framework.Update -= Farm;
		Framework.Update -= FindElemental;
		Framework.Update -= CheckState;
		DisableNameplate();
		RestoreSpeed(true);
		_uiBuilder.Dispose();
		CommandManager.RemoveHandler("/skyeye");
		_carrotTimer.Stop();
		_potTimer.Stop();
		_carrotTimer.Dispose();
		_potTimer.Dispose();
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
		if (!force && (!Configuration.AutoForwardNewRabbit || _carrotTimer is { Enabled: true } || Condition[ConditionFlag.InCombat] || FateManager.Instance()->SyncedFateId != 0 || wait4chest)) return;
		if (!string.IsNullOrEmpty(Configuration.BeforeGotoNewRabbit))
			foreach (var cmd in Configuration.BeforeGotoNewRabbit.Split('|'))
				ChatBox.SendMessage(cmd);
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

	internal static void FindPot(bool force = false) {
		if (!Configuration.EnableOccultPotNavigation) return;
		if (!force && (_potTimer is { Enabled: true } || Condition[ConditionFlag.InCombat] || wait4chest)) return;
		if (!string.IsNullOrEmpty(Configuration.BeforeGotoNewPot))
			foreach (var cmd in Configuration.BeforeGotoNewPot.Split('|'))
				ChatBox.SendMessage(cmd);
		foreach (var ret in Fates.Where(fate => fate.FateId is 1976 or 1977)) {
			Ipcs.PathfindAndMoveTo(ret.Position);
			return;
		}
	}

	private static unsafe void Farm(IFramework _) {
		if (!Configuration.PluginEnabled) return;
		if (ObjectTable.LocalPlayer is null || !Configuration.AutoFarm) return;
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
				if (!Ipcs.IsRunning()) Ipcs.PathfindAndMoveTo(obj.Position);
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
			foreach (var cmd in Configuration.BeforeFindTreasure.Split('|'))
				ChatBox.SendMessage(cmd);
		_carrotTimer.Start();
	}

	private static void StartPotTimer() {
		if (_potTimer.Enabled || !Configuration.AutoPot) return;
		UsePotCarrot();
		if (!string.IsNullOrEmpty(Configuration.BeforeFindPot))
			foreach (var cmd in Configuration.BeforeFindPot.Split('|'))
				ChatBox.SendMessage(cmd);
		_potTimer.Start();
	}

	private static void StopCarrotTimer() {
		if (_carrotTimer is not { Enabled: true }) return;
		_carrotTimer.Stop();
	}

	private static void StopPotTimer() {
		if (_potTimer is not { Enabled: true }) return;
		_potTimer.Stop();
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

	private static unsafe void UsePotCarrot() {
		if (!Configuration.AutoPot) return;
		if (!InOccult()) {
			StopPotTimer();
			return;
		}
		if (InventoryManager.Instance()->GetInventoryItemCount(LuckyPotItemId) > 0)
			ActionManager.Instance()->UseAction(ActionType.EventItem, LuckyPotItemId, mode: ActionManager.UseActionMode.Queue);
		else {
			Log.Warning("没有幸运胡萝卜可用，停止自动使用");
			StopCarrotTimer();
		}
	}

	private void OnCommand(string? command, string? args) => _configWindow.Toggle();

	internal static bool InEureka() => ObjectTable.LocalPlayer != null && InEureka(ClientState.TerritoryType);
	internal static bool InOccult() => ObjectTable.LocalPlayer != null && InOccult(ClientState.TerritoryType);
	internal static bool InEureka(uint id) => (Territory)id is Territory.Anemos or Territory.Pagos or Territory.Pyros or Territory.Hydatos;
	internal static bool InOccult(uint id) => id == 1252;

	internal static bool InArea() => InEureka() || CurrentSpeedInfo != null;
	internal static Vector3 Pos2Map(Vector2 pos) => ToVector3(MapToWorld(pos, 200, 11f, (Territory)ClientState.TerritoryType == Territory.Hydatos ? 20.25f : 11.25f));

	internal static unsafe void SetFlagAndMove(Vector2 pos) {
		AgentMap.Instance()->SetFlagMapMarker(ClientState.TerritoryType, ClientState.MapId, Pos2Map(pos));
		var p = Ipcs.FlagToPoint();
		if (p.HasValue) Ipcs.PathfindAndMoveTo(p.Value);
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
				if (!Configuration.AutoForwardNewRabbit) continue;
				ChatBox.SendMessage("/e 等待7s后寻找下一个兔子");
				if (!string.IsNullOrEmpty(Configuration.AfterFindTreasure))
					foreach (var cmd in Configuration.AfterFindTreasure.Split('|'))
						ChatBox.SendMessage(cmd);
				Task.Run(async () => {
					await Task.Delay(7000);
					wait4chest = false;
					FindRabbit(force: true);
				});
			}
			return;
		}
		var result = DirectionRegex().Match(msg);
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
		if (Configuration.AutoRabbit) Ipcs.PathfindAndMoveTo(pos);
	}

	private static void ChatPot(IChatMessage chatMessage) {
		if (!InOccult()) return;
		var msg = chatMessage.Message.TextValue.Trim();
		if (msg.StartsWith("谢谢你的圣灵药！")) {
			DetectedTreasurePositions = [];
			wait4chest = true;
			StopPotTimer();
			foreach (var obj in ObjectTable) {
				if (obj is not { ObjectKind: ObjectKind.EventObj } || !obj.Name.ToString().Contains("财宝箱")) continue;
				unsafe {
					TargetSystem.Instance()->InteractWithObject((GameObject*)obj.Address);
				}
				var name = obj.Name.ToString();
				Configuration.TotalPot.TryAdd(name, 0);
				Configuration.TotalPot[name]++;
				Configuration.Save();
			}
			return;
		}
		if (msg.StartsWith("给我更多的圣灵药，我就再帮你找一次财宝！")) {
			DetectedTreasurePositions = [];
			wait4chest = true;
			StopPotTimer();
			foreach (var obj in ObjectTable) {
				if (obj is not { ObjectKind: ObjectKind.EventObj } || !obj.Name.ToString().Contains("财宝箱")) continue;
				unsafe {
					TargetSystem.Instance()->InteractWithObject((GameObject*)obj.Address);
				}
				var name = obj.Name.ToString();
				Configuration.TotalPot.TryAdd(name, 0);
				Configuration.TotalPot[name]++;
				Configuration.Save();
				ChatBox.SendMessage("/e 等待7s后寻找下一个点位");
				Task.Run(async () => {
					await Task.Delay(7000);
					wait4chest = false;
					StartPotTimer();
				});
			}
			return;
		}
		var result = DirectionRegex().Match(msg);
		if (!(result.Success || msg.StartsWith("撒娇罐很想要圣灵药"))) return;
		StartPotTimer();
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
		DetectedTreasurePositions = OccultPotPosition[ClientState.TerritoryType]
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
		if (Configuration.AutoPot) Ipcs.PathfindAndMoveTo(pos);
	}

	internal static uint MoonPackId => ClientState.TerritoryType switch { 1319 => 50415, 1310 => 50414, _ => 0 };

	private static void ChatMoonPack(IChatMessage message) {
		if (!isFindingMoonPack || MoonPackId == 0) return;
		if (message.Message.TextValue == "探索无人机发现了星球遗物！") {
			var p = ObjectTable.ReactionEventObjects.FirstOrDefault(i => i.Name.TextValue == "星球遗物");
			if (p == null) return;
			var pos = p.Position;
			Ipcs.PathfindAndMoveTo(pos);
			Task.Run(async () => {
				while (true) {
					await Task.Delay(1000);
					if (!isFindingMoonPack || MoonPackId == 0) return;
					if (Vector3.DistanceSquared(ObjectTable.LocalPlayer.Position, pos) < 10
					    && !Condition[ConditionFlag.BetweenAreas]
					    && !Condition[ConditionFlag.BetweenAreas51]
					    && !Ipcs.IsRunning()) {
						unsafe {
							Framework.RunOnTick(() => TargetSystem.Instance()->InteractWithObject((GameObject*)p.Address));
						}
					}
				}
			});
		}
		if (message.Message.TextValue == "“星球遗物”解析完毕！") {
			Task.Run(async () => {
				var p = ObjectTable.ReactionEventObjects.FirstOrDefault(i => i.Name.TextValue == "星球遗物");
				if (p == null) return;
				ChatBox.SendMessage("/e 等待5s后开启下一个");
				await Task.Delay(5000);
				if (!isFindingMoonPack || MoonPackId == 0) return;
				unsafe {
					Framework.RunOnTick(() => AgentInventoryContext.Instance()->UseItem(MoonPackId));
				}
			});
		}
	}

	private static void Chat30OccultTreasure(IChatMessage chatMessage) {
		if (!InOccult() || !Configuration.Auto30OccultTreasure) return;
		var msg = chatMessage.Message.TextValue.Trim();
		if (Chat30OccultTreasureRegex().IsMatch(msg) && OccultTreasurePosition.TryGetValue(ClientState.TerritoryType, out var value)) {
			foreach (var p in Configuration.BeforeAuto30OccultTreasure.Split("|")) ChatBox.SendMessage(p);
			StartFindOccultTreasure(() => {
				foreach (var p in Configuration.AfterAuto30OccultTreasure.Split("|")) ChatBox.SendMessage(p);
			});
		}
	}

	internal static bool GreenNearby() {
		var localPlayer = ObjectTable.LocalPlayer;
		if (localPlayer == null) return false;
		var friends = Configuration.SpeedUpFriendly.Split('|');
		return OtherPlayer.Any(i => !friends.Contains(i.Name.ToString()) && Vector3.DistanceSquared(i.Position, localPlayer.Position) < 110f * 110f);
	}

	private static void UpdateRoundPlayers(IFramework _) {
		if (!Configuration.PluginEnabled || !Configuration.SpeedUpEnabled || ObjectTable.LocalPlayer == null ||
		    Condition[ConditionFlag.BetweenAreas] || Condition[ConditionFlag.BetweenAreas51] || !InArea() || CurrentSpeedInfo == null) {
			RestoreSpeed();
			return;
		}
		OtherPlayer.Clear();
		foreach (var obj in ObjectTable)
			if (obj.GameObjectId != ObjectTable.LocalPlayer.GameObjectId & obj.Address.ToInt64() != 0 && obj is IPlayerCharacter rcTemp)
				OtherPlayer.Add(rcTemp);
		if (GreenNearby()) RestoreSpeed();
		else ApplyBaseMovementSpeedOverride();
	}

	internal static SpeedInfo? FindSpeedInfo(uint territoryType) {
		var territory = territoryType.ToString();
		return Configuration.SpeedUp.FirstOrDefault(s =>
			s != null &&
			s.Enabled &&
			!string.IsNullOrWhiteSpace(s.SpeedUpTerritory ?? string.Empty) &&
			(s.SpeedUpTerritory ?? string.Empty).Split('|').Contains(territory) &&
			IsValidSpeedValue(s.BaseMovementSpeed) &&
			IsValidSpeedValue(s.MountBaseMovementSpeed) &&
			IsValidSpeedValue(s.SpeedUpN) &&
			IsValidSpeedValue(s.SpeedUpMax));
	}

	internal static bool IsValidSpeedValue(float value) => float.IsFinite(value) && value > 0f;

	internal static bool DisableAutoTreasureOnTerritoryEnter(uint territoryId) {
		var changed = false;
		if (Configuration.DisableAutoRabbitWhenTerritoryChanged && InEureka(territoryId)) {
			changed |= Configuration.AutoRabbit;
			changed |= Configuration.AutoForwardNewRabbit;
			Configuration.AutoRabbit = false;
			Configuration.AutoForwardNewRabbit = false;
		}
		if (Configuration.DisableAutoPotWhenTerritoryChanged && InOccult(territoryId)) {
			changed |= Configuration.AutoPot;
			changed |= Configuration.EnableOccultPotNavigation;
			Configuration.AutoPot = false;
			Configuration.EnableOccultPotNavigation = false;
		}
		return changed;
	}

	private static bool MigrateConfiguration() {
		if (Configuration.Version >= CurrentConfigurationVersion) return false;
		if (Configuration.Version < 1) {
			Configuration.DisableAutoRabbitWhenTerritoryChanged = true;
			Configuration.DisableAutoPotWhenTerritoryChanged = true;
		}
		Configuration.Version = CurrentConfigurationVersion;
		return true;
	}

	private static bool EnsureDefaultSpeedInfo() {
		var changed = false;
		Configuration.SpeedUp ??= [];
		if (Configuration.SpeedUp.Count == 0) {
			Configuration.SpeedUp.Add(SpeedInfo.Default());
			return true;
		}
		if (Configuration.SpeedUp.Any(speedInfo => speedInfo?.IsDefault == true)) return changed;
		var legacyDefault = Configuration.SpeedUp.FirstOrDefault(speedInfo =>
			speedInfo != null && SpeedInfo.HasLegacyDefaultCharacteristics(speedInfo));
		if (legacyDefault != null) {
			legacyDefault.IsDefault = true;
			changed = true;
		}
		return changed;
	}

	internal static void RefreshCurrentSpeedInfo(uint? territoryType = null, bool resetFailures = false) {
		if (resetFailures) ResetSpeedFailureState();
		RestoreSpeed(true);
		CurrentSpeedInfo = FindSpeedInfo(territoryType ?? ClientState.TerritoryType);
	}

	internal static void ResetSpeedFailureState() {
		_speedRetryAfter = 0;
		_speedScanAttempted = false;
		_speedFailureLogged = false;
	}

	// https://github.com/Jaksuhn/ffxiv-bundleoftweaks
	// https://github.com/MnFeN/Triggernometry
	[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
	internal static void ApplyBaseMovementSpeedOverride() {
		var speedInfo = CurrentSpeedInfo;
		if (SpeedRetryPending() || speedInfo == null || !Configuration.PluginEnabled || !Configuration.SpeedUpEnabled ||
		    !IsValidSpeedValue(speedInfo.BaseMovementSpeed) || !IsValidSpeedValue(speedInfo.MountBaseMovementSpeed) ||
		    !IsValidSpeedValue(speedInfo.SpeedUpN) || !IsValidSpeedValue(speedInfo.SpeedUpMax)) return;
		var mounted = Condition[ConditionFlag.Mounted];
		var baseSpeed = mounted ? speedInfo.MountBaseMovementSpeed : speedInfo.BaseMovementSpeed;
		var requestedSpeed = Math.Min(speedInfo.SpeedUpMax, baseSpeed * speedInfo.SpeedUpN);
		if (_speedOverridden) {
			try {
				var activeValues = SafeMemory.Read<float>(_overriddenSpeedAddress, 1);
				if (activeValues == null || activeValues.Length == 0 || !IsValidSpeedValue(activeValues[0])) {
					ScheduleSpeedRetry("无法校验当前基础移速覆盖值");
					return;
				}
				if (float.IsFinite(_lSpeed) && SpeedApproximatelyEquals(activeValues[0], _lSpeed)) {
					if (SpeedApproximatelyEquals(_lSpeed, requestedSpeed)) return;
				} else {
					Log.Information($"[BaseMovementSpeed] 检测到游戏重置基础移速为 {activeValues[0]}，重新建立覆盖状态");
					ClearSpeedOverrideState();
				}
			} catch (Exception ex) {
				ScheduleSpeedRetry("校验当前基础移速覆盖值失败", ex);
				return;
			}
		}
		if (!TryResolveBaseMovementSpeedAddress(out var address)) return;

		float previousSpeed, finalSpeed;
		try {
			var values = SafeMemory.Read<float>(address, 1);
			if (values == null || values.Length == 0 || !IsValidSpeedValue(values[0])) {
				ScheduleSpeedRetry("读取当前基础移速失败");
				if (!_speedOverridden) {
					BaseMovementSpeedPtr = null;
					_speedScanAttempted = false;
				}
				return;
			}
			previousSpeed = values[0];
			finalSpeed = requestedSpeed;
			if (!IsValidSpeedValue(finalSpeed)) {
				ScheduleSpeedRetry("计算得到的基础移速覆盖值无效");
				return;
			}
			SafeMemory.Write(address, finalSpeed);
		} catch (Exception ex) {
			ScheduleSpeedRetry("读写基础移速内存失败", ex);
			if (!_speedOverridden) {
				BaseMovementSpeedPtr = null;
				_speedScanAttempted = false;
			}
			return;
		}

		if (!_speedOverridden) {
			_originalSpeed = previousSpeed;
			_restoreWalkingBaseSpeed = speedInfo.BaseMovementSpeed;
			_restoreMountedBaseSpeed = speedInfo.MountBaseMovementSpeed;
			_overrideMounted = mounted;
			_overriddenSpeedAddress = address;
			_speedOverridden = true;
		}
		_lSpeed = finalSpeed;
		ClearSpeedRetry();
		if (Configuration.SpeedDebugOutput) {
			try {
				ChatBox.SendMessage($"/e SetBaseSpeed: mounted={mounted} base={baseSpeed}*rate={speedInfo.SpeedUpN} capped={speedInfo.SpeedUpMax} {previousSpeed}->{finalSpeed}");
			} catch (Exception ex) {
				LogSpeedFailure("基础移速调试输出失败", ex);
			}
		}
	}

	internal static void RestoreSpeed(bool force = false) {
		if (!_speedOverridden) {
			_lSpeed = float.NaN;
			return;
		}
		if (!force && SpeedRetryPending()) return;
		var addressValue = _overriddenSpeedAddress.ToInt64();
		if (_overriddenSpeedAddress == IntPtr.Zero || addressValue < 0x10000 || addressValue > 0x00007FFFFFFFFFFF) {
			ScheduleSpeedRetry("缓存的基础移速地址不合理，暂缓恢复写入");
			return;
		}
		try {
			var currentValues = SafeMemory.Read<float>(_overriddenSpeedAddress, 1);
			if (currentValues == null || currentValues.Length == 0 || !float.IsFinite(currentValues[0])) {
				ScheduleSpeedRetry("恢复前无法确认基础移速地址可读，暂缓恢复写入");
				return;
			}
			var mounted = Condition[ConditionFlag.Mounted];
			var restoreSpeed = mounted == _overrideMounted
				? _originalSpeed
				: mounted ? _restoreMountedBaseSpeed : _restoreWalkingBaseSpeed;
			if (!IsValidSpeedValue(restoreSpeed)) {
				ScheduleSpeedRetry("没有可用的基础移速恢复值");
				return;
			}
			if (SpeedApproximatelyEquals(currentValues[0], restoreSpeed)) {
				ClearSpeedOverrideState();
				return;
			}
			if (!float.IsFinite(_lSpeed) || !SpeedApproximatelyEquals(currentValues[0], _lSpeed)) {
				Log.Warning($"[BaseMovementSpeed] 当前值已被游戏或其他插件改为 {currentValues[0]}，放弃旧覆盖状态，不写入过期恢复值");
				ClearSpeedOverrideState();
				return;
			}
			SafeMemory.Write(_overriddenSpeedAddress, restoreSpeed);
			var restoredValues = SafeMemory.Read<float>(_overriddenSpeedAddress, 1);
			if (restoredValues == null || restoredValues.Length == 0 || !float.IsFinite(restoredValues[0]) || !SpeedApproximatelyEquals(restoredValues[0], restoreSpeed)) {
				ScheduleSpeedRetry("恢复后基础移速值校验失败");
				return;
			}
		} catch (Exception ex) {
			ScheduleSpeedRetry("恢复原始基础移速失败", ex);
			return;
		}
		ClearSpeedOverrideState();
	}

	private static void ClearSpeedOverrideState() {
		_speedOverridden = false;
		_overriddenSpeedAddress = IntPtr.Zero;
		_originalSpeed = 0f;
		_restoreWalkingBaseSpeed = 0f;
		_restoreMountedBaseSpeed = 0f;
		_overrideMounted = false;
		_lSpeed = float.NaN;
		ClearSpeedRetry();
	}

	private static bool SpeedApproximatelyEquals(float left, float right) => Math.Abs(left - right) <= SpeedComparisonTolerance;

	private static bool SpeedRetryPending() => Environment.TickCount64 < _speedRetryAfter;

	private static void ScheduleSpeedRetry(string message, Exception? exception = null) {
		_speedRetryAfter = Environment.TickCount64 + SpeedRetryDelayMilliseconds;
		LogSpeedFailure(message, exception);
	}

	private static void ClearSpeedRetry() {
		_speedRetryAfter = 0;
		_speedFailureLogged = false;
	}

	private static bool TryResolveBaseMovementSpeedAddress(out IntPtr address) {
		if (BaseMovementSpeedPtr.HasValue) {
			address = BaseMovementSpeedPtr.Value;
			return true;
		}
		address = IntPtr.Zero;
		if (_speedScanAttempted) return false;
		_speedScanAttempted = true;
		try {
			if (!SigScanner.TryScanText(BaseMovementSpeedSignature, out var signature) || signature == IntPtr.Zero) {
				_speedScanAttempted = false;
				ScheduleSpeedRetry("未找到基础移速签名");
				return false;
			}
			var displacementAddress = signature + 3;
			address = displacementAddress + Marshal.ReadInt32(displacementAddress) + 4 + BaseMovementSpeedOffset;
			if (address == IntPtr.Zero) {
				_speedScanAttempted = false;
				ScheduleSpeedRetry("基础移速地址无效");
				return false;
			}
			BaseMovementSpeedPtr = address;
			return true;
		} catch (Exception ex) {
			_speedScanAttempted = false;
			ScheduleSpeedRetry("解析基础移速地址失败", ex);
			address = IntPtr.Zero;
			return false;
		}
	}

	private static void LogSpeedFailure(string message, Exception? exception = null) {
		if (_speedFailureLogged) return;
		_speedFailureLogged = true;
		if (exception == null) Log.Error($"[BaseMovementSpeed] {message}");
		else Log.Error(exception, $"[BaseMovementSpeed] {message}");
	}

	[GeneratedRegex("^财宝好像是在(?<direction>正北|东北|正东|东南|正南|西南|正西|西北)方向(?<distance>(很远|稍远|不远|很近))的地方！")]
	private static partial Regex DirectionRegex();

	[GeneratedRegex("^在当前区域中感知到了.个银宝箱、30个铜宝箱……！")]
	private static partial Regex Chat30OccultTreasureRegex();
}
