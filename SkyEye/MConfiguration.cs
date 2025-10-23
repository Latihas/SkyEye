using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using static SkyEye.SkyEye.Plugin;

namespace SkyEye.SkyEye;

[Serializable]
public class MConfiguration : IPluginConfiguration {
    public float FarmMaxDistance = 100, RabbitWaitX, RabbitWaitY, RabbitWaitZ, FarmWaitX, FarmWaitY, FarmWaitZ;
    public int FarmTargetMax = 1, WssRegion, FarmDistAlgo;
    public bool PluginEnabled = true, SpeedUpEnabled = true, Overlay2DWeatherMapEnabled = true, Overlay3DEnabled = true, AutoRabbit = true, AutoRabbitWait = true, AutoFarm, FarmWait, EnableWss;
    public List<SpeedInfo> SpeedUp = [];
    public string SpeedUpFriendly = "", NmBattleTimeText = "", FarmTarget = "", FarmStartCommand = "/ac 飞斧", WssNotify = "";
    public int Version { get; set; }

    public void Save() => PluginInterface.SavePluginConfig(this);

    public record SpeedInfo {
        public string Desc = "";
        public bool Enabled;
        public float SpeedUpMax = 20f;
        public float SpeedUpN = 3.5f;
        public string SpeedUpTerritory = "";

        public static SpeedInfo Default() => new() {
            Desc = "ULK",
            SpeedUpTerritory = "732|763|795|827",
            Enabled = true
        };
    }
}