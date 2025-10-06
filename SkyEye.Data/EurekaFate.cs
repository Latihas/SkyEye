using System.Numerics;

namespace SkyEye.SkyEye.Data;

internal class EurekaFate(ushort fateId, string bossShortName, Vector2 fatePosition, PData.EurekaWeather spawnRequiredWeather, bool spawnByRequiredNight) {
    internal ushort FateId { get; private set; } = fateId;
    internal string BossShortName { get; private set; } = bossShortName;
    internal Vector2 FatePosition { get; } = fatePosition;
    internal PData.EurekaWeather SpawnRequiredWeather { get; private set; } = spawnRequiredWeather;
    internal bool SpawnByRequiredNight { get; private set; } = spawnByRequiredNight;
}