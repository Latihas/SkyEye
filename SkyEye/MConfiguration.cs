using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Configuration;
using static SkyEye.Plugin;

namespace SkyEye;

[Serializable]
public class MConfiguration : IPluginConfiguration {
	public float FarmMaxDistance = 100, FarmWaitX, FarmWaitY, FarmWaitZ, FlagR = 100;
	public int FarmTargetMax = 1, WssRegion, FarmDistAlgo, NextWeatherCount = 10,
		OccultTreasureDelay = 2000;
	public bool PluginEnabled = true, SpeedUpEnabled = true, SpeedDebugOutput, Overlay2DWeatherMapEnabled = true, Overlay2DDetailEnabled = true, Overlay3DEnabled = true,
		AutoRabbit = true, AutoForwardNewRabbit = true,
		AutoPot, Auto30OccultTreasure, EnableOccultBunnyNavigation, EnableOccultPotNavigation,
		AutoFarm, FarmWait,
		EnableWss,
		ShowCurrentElemental,
		DisableAutoRabbitWhenTerritoryChanged = true,
		DisableAutoPotWhenTerritoryChanged = true,
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
		public const float DefaultSpeedMultiplierMax = 3.5f;
		public string Desc = "";
		public bool Enabled, IsDefault;
		public float SpeedUpN = 3.5f;
		public float SpeedMultiplierMax = DefaultSpeedMultiplierMax;
		public float BaseMovementSpeed = 6f;
		public float MountBaseMovementSpeed = 9.6f;
		public float SpeedUpMax = 20f;
		public float SpeedUpMountX = 2f;
		public string SpeedUpTerritory = "";
		private static readonly SpeedInfo _default = new() {
			Desc = "ULK, 该行地区Id与描述不可修改",
			SpeedUpTerritory = "732|763|795|827",
			Enabled = true,
			IsDefault = true
		};

		public static SpeedInfo Default() => new() {
			Desc = _default.Desc,
			SpeedUpTerritory = _default.SpeedUpTerritory,
			Enabled = _default.Enabled,
			IsDefault = true
		};

		internal static bool HasLegacyDefaultCharacteristics(SpeedInfo speedInfo) {
			var expectedTerritories = _default.SpeedUpTerritory.Split('|', StringSplitOptions.RemoveEmptyEntries);
			var actualTerritories = (speedInfo.SpeedUpTerritory ?? string.Empty).Split('|', StringSplitOptions.RemoveEmptyEntries);
			return actualTerritories.Length == expectedTerritories.Length &&
			       actualTerritories.Distinct().Count() == expectedTerritories.Length &&
			       expectedTerritories.All(actualTerritories.Contains) &&
			       string.Equals(speedInfo.Desc ?? string.Empty, _default.Desc, StringComparison.Ordinal);
		}
	}
}
