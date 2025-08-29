using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace SkyEye.SkyEye;

[Serializable]
public class Configuration : IPluginConfiguration {
	public const float Overlay2DDotStroke = 1f;
	public bool Overlay2DEnabled = true,
		Overlay2DSpeedUpEnabled = true,
		Overlay2DWeatherMapEnabled = true,
		Overlay3DEnabled = true;
	public string Overlay2DSpeedUpFriendly = "", Overlay2DSpeedUpTerritory = "", NmBattleTimeText = "";
	public float Overlay2DSpeedUpN = 3.5f;

	[NonSerialized] private IDalamudPluginInterface _pluginInterface;

	public int Version { get; set; }

	public void Initialize(IDalamudPluginInterface pluginInterface) {
		_pluginInterface = pluginInterface;
	}

	public void Save() {
		_pluginInterface.SavePluginConfig(this);
	}
}