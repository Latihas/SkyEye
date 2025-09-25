using System;
using Dalamud.Configuration;

namespace SkyEye.SkyEye;

[Serializable]
public class Configuration : IPluginConfiguration {
	public const float Overlay2DDotStroke = 1f;
	public bool Overlay2DEnabled = true, SpeedUpEnabled = true, Overlay2DWeatherMapEnabled = true, Overlay3DEnabled = true, AutoRabbit = true,AutoRabbitWait = true;
	public string SpeedUpFriendly = "", SpeedUpTerritory = "", NmBattleTimeText = "";
	public float SpeedUpN = 3.5f, RabbitWaitX = 0,RabbitWaitY = 0,RabbitWaitZ = 0;

	public int Version { get; set; }

	public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}