using System.Numerics;

namespace SkyEye.SkyEye.Data;

internal class EurekaFate(ushort fateId, int lv, string name, string bossShortName, string trigger, int triggerLv, Vector2 fatePosition, PData.EurekaWeather spawnRequiredWeather = PData.EurekaWeather.None, bool spawnByRequiredNight = false) {
    internal ushort FateId { get; private set; } = fateId;
    internal string Lv { get; private set; } = lv.ToString();
    internal string Name { get; private set; } = name;
    internal string Trigger { get; private set; } = trigger;
    internal string TriggerLv { get; private set; } = triggerLv > 0 ? triggerLv.ToString() : "";
    internal string BossShortName { get; private set; } = bossShortName;
    internal Vector2 FatePosition { get; } = fatePosition;
    internal PData.EurekaWeather SpawnRequiredWeather { get; private set; } = spawnRequiredWeather;
    internal bool SpawnByRequiredNight { get; private set; } = spawnByRequiredNight;
}