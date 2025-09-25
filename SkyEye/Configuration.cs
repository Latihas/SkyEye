using System;
using Dalamud.Configuration;

namespace SkyEye.SkyEye;

[Serializable]
public class Configuration : IPluginConfiguration {
	public const float Overlay2DDotStroke = 1f;
	public bool Overlay2DEnabled = true, Overlay2DSpeedUpEnabled = true, Overlay2DWeatherMapEnabled = true, Overlay3DEnabled = true;
	public string Overlay2DSpeedUpFriendly = "", Overlay2DSpeedUpTerritory = "", NmBattleTimeText = "";
	public float Overlay2DSpeedUpN = 3.5f;


	public int Version { get; set; }

	public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}