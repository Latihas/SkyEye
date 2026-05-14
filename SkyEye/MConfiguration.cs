using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Configuration;
using static SkyEye.Plugin;

namespace SkyEye;

[Serializable]
public class MConfiguration : IPluginConfiguration {
	public float FarmMaxDistance = 100, FarmWaitX, FarmWaitY, FarmWaitZ, FlagR = 100;
	public int FarmTargetMax = 1, WssRegion, FarmDistAlgo, NextWeatherCount = 10,
		OccultTreasureDelay = 2000;
	public bool PluginEnabled = true, SpeedUpEnabled = true, Overlay2DWeatherMapEnabled = true, Overlay2DDetailEnabled = true, Overlay3DEnabled = true,
		AutoRabbit = true, AutoForwardNewRabbit = true, AutoPot, 
		AutoFarm, FarmWait, EnableWss,
		ShowCurrentElemental, 
		DisableAutoRabbitWhenTerritoryChanged,
		DisableAutoPotWhenTerritoryChanged,
		PreventTp, NameReplacement, EnablePalacePal,
		FindCharaNiao, FindCharaMao, FindCharaGou, FindCharaZhu,
		FindRaceRenM, FindRaceRenF,
		FindRaceJingLingM, FindRaceJingLingF,
		FindRaceLaLaFeiErM, FindRaceLaLaFeiErF,
		FindRaceMaoMeiM, FindRaceMaoMeiF,
		FindRaceLuJiaM, FindRaceLuJiaF,
		FindRaceAoLongM, FindRaceAoLongF,
		FindRaceGeShiM, FindRaceGeShiF,
		FindRaceWeiAiLaM, FindRaceWeiAiLaF;
	public List<SpeedInfo> SpeedUp = [];
	public string SpeedUpFriendly = "", NmBattleTimeText = "", FarmTarget = "", FarmStartCommand = "/ac 飞斧", WssNotify = "", FindEntity = "", 
		BeforeFindTreasure = "/bmrai off", AfterFindTreasure = "", BeforeGotoNewRabbit = "/bmrai on",
		BeforeFindPot = "/bmrai off", AfterFindPot = "", BeforeGotoNewPot = "/bmrai on",
		
		BeforeOccultTreasure = "/i-ching-commander y_adjust -7 false", AfterOccultTreasure = "/i-ching-commander y_adjust 0 false", TpCommand = "";
	public int Version { get; set; }
	public Dictionary<string, int> TotalChest = [],TotalPot = [];
	public List<(string, string)> NameReplacementDict = [];
	public Dictionary<uint, HashSet<Vector3>> AllElementalPositions = [];
	public bool CoreTpWhenGreenNearby;

	public void Save() => PluginInterface.SavePluginConfig(this);

	public record SpeedInfo {
		public string Desc = "";
		public bool Enabled;
		public float SpeedUpMax = 20f;
		public float SpeedUpMountX = 2f;
		public float SpeedUpN = 3.5f;
		public string SpeedUpTerritory = "";
		private static readonly SpeedInfo _default = new() {
			Desc = "ULK, 该行地区Id与描述不可修改",
			SpeedUpTerritory = "732|763|795|827",
			SpeedUpMountX = 1.6f,
			Enabled = true
		};

		public static SpeedInfo Default() => _default;
	}
}