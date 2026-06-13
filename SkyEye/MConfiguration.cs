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
		AutoRabbit = true, AutoForwardNewRabbit = true,
		AutoPot, Auto30OccultTreasure,
		AutoFarm, FarmWait,
		EnableWss,
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
		BeforeFindPot = "/bocchiillegal off|/bmrai off|/rotation off", AfterFindPot = "/bocchiillegal on|/rotation manual|/ac 返回", BeforeGotoNewPot = "/bmrai on",
		BeforeAuto30OccultTreasure = "/bocchiillegal off|/bmrai off|/rotation off|/i-ching-commander y_adjust -7 false", AfterAuto30OccultTreasure = "/bocchiillegal on|/rotation manual|/ac 返回|/i-ching-commander y_adjust 0 false",
		BeforeOccultTreasure = "/bocchiillegal off|/bmrai off|/rotation off|/i-ching-commander y_adjust -7 false", AfterOccultTreasure = "/bocchiillegal on|/rotation manual|/ac 返回|/i-ching-commander y_adjust 0 false", TpCommand = "";
	public int Version { get; set; }
	public Dictionary<string, int> TotalChest = [], TotalPot = [];
	public List<(string, string)> NameReplacementDict = [];
	public Dictionary<uint, HashSet<Vector3>> AllElementalPositions = [];
	public bool CoreTpWhenGreenNearby, ShowMyPos, ShowCircle, ShowSquare;
	public float CircleR = 20, SquareR = 20, ShowThickness =6,ShowCenterX=100,ShowCenterY,ShowCenterZ=100;

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