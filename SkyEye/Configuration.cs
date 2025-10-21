using System;
using Dalamud.Configuration;

namespace SkyEye.SkyEye;

[Serializable]
public class Configuration : IPluginConfiguration {
    public int FarmTargetMax = 1, WssRegion, FarmDistAlgo;
    public bool PluginEnabled = true, SpeedUpEnabled = true, Overlay2DWeatherMapEnabled = true, Overlay3DEnabled = true, AutoRabbit = true, AutoRabbitWait = true, AutoFarm, FarmWait, EnableWss;
    public string SpeedUpFriendly = "", SpeedUpTerritory = "", NmBattleTimeText = "", FarmTarget = "", FarmStartCommand = "/ac 飞斧", WssNotify = "";
    public float SpeedUpN = 3.5f, FarmMaxDistance = 100, RabbitWaitX, RabbitWaitY, RabbitWaitZ, FarmWaitX, FarmWaitY, FarmWaitZ;

    public int Version { get; set; }

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}