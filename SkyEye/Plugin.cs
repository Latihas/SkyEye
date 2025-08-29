using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using SkyEye.SkyEye.Data;
using static System.StringComparison;

namespace SkyEye.SkyEye;

public sealed class Plugin : IDalamudPlugin {
	private static float _lSpeed = 1f;
	private static SpeedHackPlugin _shp;
	private readonly Lock _speedLock = new();
	public readonly WindowSystem WindowSystem = new("SkyEye");
	internal List<Vector3> DetectedTreasurePositions = [];
	private float _dspeed = 1f;
	internal readonly List<IPlayerCharacter> OtherPlayer = [];

	public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager) {
		PluginInterface = pluginInterface;
		CommandManager = commandManager;
		Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
		Configuration.Initialize(PluginInterface);
		new UiBuilder(this, pluginInterface);
		ConfigWindow = new ConfigWindow(this);
		WindowSystem.AddWindow(ConfigWindow);
		CommandManager.AddHandler("/skyeye", new CommandInfo(OnCommand) {
			HelpMessage = "打开主界面"
		});
		PluginInterface.UiBuilder.Draw += DrawUi;
		Framework.Update += UpdateRoundPlayers;
		PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
		ChatGui.ChatMessageUnhandled += OnChatMessage;
	}

	internal IDalamudPluginInterface PluginInterface { get; init; }

	private ICommandManager CommandManager { get; }

	public static Configuration Configuration { get; private set; }

	[PluginService] public static IClientState ClientState { get; set; }

	[PluginService] internal static IDataManager DataManager { get; set; }

	[PluginService] internal static IPluginLog Log { get; set; }

	[PluginService] internal static ICondition Condition { get; set; }

	[PluginService] internal static IGameGui Gui { get; set; }

	[PluginService] internal static IObjectTable Objects { get; set; }

	[PluginService] internal static IFateTable Fates { get; set; }

	[PluginService] internal static IFramework Framework { get; set; } = null;

	[PluginService] internal static ISigScanner SigScanner { get; set; } = null;

	[PluginService] internal static IChatGui ChatGui { get; set; } = null;


	private ConfigWindow ConfigWindow { get; }

	public void Dispose() {
		WindowSystem.RemoveAllWindows();
		ChatGui.ChatMessageUnhandled -= OnChatMessage;
		ConfigWindow.Dispose();
		Framework.Update -= UpdateRoundPlayers;
		_shp?.Dispose();
		CommandManager.RemoveHandler("/skyeye");
	}

	private void OnCommand(string command, string args) {
		ToggleConfigUi();
	}

	internal static bool InEureka() => ClientState.LocalPlayer != null && ClientState.TerritoryType is 732 or 763 or 795 or 827;
	internal static bool InArea() 
		=>  InEureka() ||  Configuration.Overlay2DSpeedUpTerritory.Split('|').Contains(ClientState.TerritoryType.ToString());
	

	private void OnChatMessage(XivChatType type, int timestamp, SeString sender, SeString message) {
		if (message == null || !InEureka()) return;
		var msg = message.TextValue.Trim();
		if (msg.StartsWith("找到了财宝")) DetectedTreasurePositions = [];
		if (!msg.StartsWith("财宝好像是在")) return;
		var result = Regex.Match(msg, "财宝好像是在(?<direction>正北|东北|正东|东南|正南|西南|正西|西北)方向(?<distance>(很远|稍远|不远|很近))的地方！");
		if (!result.Success) return;
		var direction = result.Groups["direction"].Value;
		int minDistance;
		int maxDistance;
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
		var treasures = from c in PData.RabbitTreasurePositions[ClientState.TerritoryType].Where(delegate(Vector3 c) {
				var num = Vector3.Distance(playerPos, c);
				return num >= minDistance && num <= maxDistance;
			})
			orderby Vector3.Distance(playerPos, c)
			select c;
		if (direction.Equals("正南", OrdinalIgnoreCase)) DetectedTreasurePositions = treasures.Where(c => c.Z > playerPos.Z && Math.Abs(c.X - playerPos.X) <= Math.Abs(c.Z - playerPos.Z)).ToList();
		else if (direction.Equals("正北", OrdinalIgnoreCase)) DetectedTreasurePositions = treasures.Where(c => c.Z < playerPos.Z && Math.Abs(c.X - playerPos.X) <= Math.Abs(c.Z - playerPos.Z)).ToList();
		else if (direction.Equals("正东", OrdinalIgnoreCase)) DetectedTreasurePositions = treasures.Where(c => c.X > playerPos.X && Math.Abs(c.X - playerPos.X) >= Math.Abs(c.Z - playerPos.Z)).ToList();
		else if (direction.Equals("正西", OrdinalIgnoreCase)) DetectedTreasurePositions = treasures.Where(c => c.X < playerPos.X && Math.Abs(c.X - playerPos.X) >= Math.Abs(c.Z - playerPos.Z)).ToList();
		else if (direction.Equals("东南", OrdinalIgnoreCase)) DetectedTreasurePositions = treasures.Where(c => c.Z >= playerPos.Z && c.X >= playerPos.X).ToList();
		else if (direction.Equals("西南", OrdinalIgnoreCase)) DetectedTreasurePositions = treasures.Where(c => c.Z >= playerPos.Z && c.X <= playerPos.X).ToList();
		else if (direction.Equals("东北", OrdinalIgnoreCase)) DetectedTreasurePositions = treasures.Where(c => c.Z <= playerPos.Z && c.X >= playerPos.X).ToList();
		else if (direction.Equals("西北", OrdinalIgnoreCase)) DetectedTreasurePositions = treasures.Where(c => c.Z <= playerPos.Z && c.X <= playerPos.X).ToList();
	}


	private void UpdateRoundPlayers(IFramework framework) {
		if (ClientState.LocalPlayer == null) return;
		if (!InArea()) {
			lock (_speedLock) SetSpeed(1f);
			return;
		}
		lock (OtherPlayer) {
			OtherPlayer.Clear();
			if (Objects == null) return;
			foreach (var obj in Objects)
				try {
					if (obj != null && obj.GameObjectId != ClientState.LocalPlayer.GameObjectId & obj.Address.ToInt64() != 0 && obj is IPlayerCharacter rcTemp) OtherPlayer.Add(rcTemp);
				}
				catch (Exception) {
					Log.Error("error");
				}
		}
		lock (_speedLock) {
			if (Configuration.Overlay2DSpeedUpEnabled) {
				var friends = Configuration.Overlay2DSpeedUpFriendly.Split('|');
				_dspeed = OtherPlayer.Any(i => !friends.Contains(i.Name.ToString()) && Vector3.Distance(i.Position, ClientState.LocalPlayer.Position) < (110 ^ 2)) ? 1f : Configuration.Overlay2DSpeedUpN;
			}
			else _dspeed = 1f;
			SetSpeed(_dspeed);
		}
	}

	public static void SetSpeed(float speedBase) {
		if (_lSpeed == speedBase) return;
		_lSpeed = speedBase;
		_shp ??= new SpeedHackPlugin();
		_shp.SetSpeedMultiplier(speedBase);
	}

	private void DrawUi() {
		WindowSystem.Draw();
	}

	private void ToggleConfigUi() {
		ConfigWindow.Toggle();
	}
}