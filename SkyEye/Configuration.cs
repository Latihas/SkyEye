using System;
using Dalamud.Configuration;

namespace SkyEye.SkyEye;

[Serializable]
public class Configuration : IPluginConfiguration {
    public int FarmTargetMax = 1, WssRegion;
    public bool PluginEnabled = true, SpeedUpEnabled = true, Overlay2DWeatherMapEnabled = true, Overlay3DEnabled = true, AutoRabbit = true, AutoRabbitWait = true, AutoFarm = false, FarmWait = false, EnableWss = false;
    public string SpeedUpFriendly = "", SpeedUpTerritory = "", NmBattleTimeText = "", FarmTarget = "", FarmStartCommand = "/ac 飞斧", WssNotify = "";
    public float SpeedUpN = 3.5f, RabbitWaitX = 0, RabbitWaitY = 0, RabbitWaitZ = 0, FarmMaxDistance = 100;

    public int Version { get; set; }

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}