using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Configuration;
using static SkyEye.Plugin;

namespace SkyEye;

[Serializable]
public class MConfiguration : IPluginConfiguration {
	public float FarmMaxDistance = 100, FarmWaitX, FarmWaitY, FarmWaitZ, FlagR = 100;
	public int FarmTargetMax = 1, WssRegion, FarmDistAlgo, NextWeatherCount = 10;
	public bool PluginEnabled = true, SpeedUpEnabled = true, Overlay2DWeatherMapEnabled = true, Overlay2DDetailEnabled = true, Overlay3DEnabled = true, AutoRabbit = true, AutoRabbitWait = true, AutoFarm, FarmWait, EnableWss,
		ShowCurrentYl, DropMovementPacket, DisableAutoRabbitWhenTerritoryChanged, PreventTp, NameReplacement, EnablePalacePal;
	public List<SpeedInfo> SpeedUp = [];
	public string SpeedUpFriendly = "", NmBattleTimeText = "", FarmTarget = "", FarmStartCommand = "/ac 飞斧", WssNotify = "", FindEntity = "", BeforeFindTreasure = "/bmrai off", AfterFindTreasure = "", BeforeGotoNewRabbit = "/bmrai on";
	public int Version { get; set; }
	public Dictionary<string, int> TotalChest = [];
	public List<(string, string)> NameReplacementDict = [];
	public Dictionary<int, HashSet<Vector3>> AllYlPositions = [];
	public bool CoreTpWhenGreenNearby;

	public void Save() => PluginInterface.SavePluginConfig(this);

	public record SpeedInfo {
		public string Desc = "";
		public bool Enabled;
		public float SpeedUpMax = 20f;
		public float SpeedUpN = 3.5f;
		public string SpeedUpTerritory = "";
		private static readonly SpeedInfo _default = new() {
			Desc = "ULK, 该行地区Id与描述不可修改",
			SpeedUpTerritory = "732|763|795|827",
			Enabled = true
		};

		public static SpeedInfo Default() => _default;
	}
}