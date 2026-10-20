using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Numerics;
using System.Text;
using System.Threading;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using SkyEye.Data;
using static System.Globalization.CultureInfo;
using static SkyEye.ConfigWindow;
using static SkyEye.Data.PData;
using static SkyEye.Plugin;
using static SkyEye.Util;

namespace SkyEye;

internal class UiBuilder : IDisposable {
	internal const string timeFormat = @"hh\:mm\:ss";
	private static readonly SoundPlayer Player1 = new(), Player2 = new();
	private static uint lastTerritoryId;
	private static ushort[] _eurekaPreviousLiveIdsSnapshot = [];
	private readonly Vector2[] _mapPosSize = new Vector2[2];
	private HashSet<ushort> _eurekaLiveIds = [];
	private HashSet<ushort> _eurekaPreviousLiveIds = [];
	private readonly Dictionary<ushort, int> _fateProgress = [];
	private readonly Dictionary<int, TimeSpan> _fateCooldowns = [];
	private readonly Dictionary<EurekaWeather, (string Start, string Left)> _weatherDic = [];
	private readonly HashSet<uint> _huntIds = [];
	private string _entityPatternSource = "";
	private string[] _entityPatterns = [];
	private IReadOnlyList<PalacePalMarker> _palacePalCache = [];
	private uint _palacePalCacheTerritory;
	private bool _palacePalCacheReady;
	private ImDrawListPtr _bdl;
	private float _globalUiScale = 1f;
	private float _mapScale = 1f;
	private Vector2? _mapOrigin = Vector2.Zero;
	private OverlayCache _cache = OverlayCache.Empty;
	private readonly Vector2[] _highResCircleOffsets = new Vector2[HighResCircleSegments + 1];
	private float _circleRadius = float.NaN;

	private readonly record struct EurekaMarker(Vector3 WorldPosition, string Text, uint Color);

	private readonly record struct PalacePalMarker(int Type, Vector3 Position);

	private sealed record OverlayCache(bool HasLocalPlayer, bool IsEureka, uint TerritoryType, Vector3 LocalPlayerPosition, Vector3? FarmPosition, IReadOnlyList<Vector3> TreasurePositions, IReadOnlyList<Vector3> TargetPositions, IReadOnlyList<Vector3> ElementalPositions, IReadOnlyList<EurekaMarker> EurekaMarkers, IReadOnlyList<PalacePalMarker> PalacePalMarkers, string? WeatherNotice) {
		internal static readonly OverlayCache Empty = new(false, false, 0, Vector3.Zero, null, [], [], [], [], [], null);
	}

	public UiBuilder() {
		ClientState.TerritoryChanged += TerritoryChanged;
		PluginInterface.UiBuilder.Draw += OnBuildUi;
	}

	public void Dispose() {
		PluginInterface.UiBuilder.Draw -= OnBuildUi;
		ClientState.TerritoryChanged -= TerritoryChanged;
	}

	private static List<Hunt> CurrentTerritoryHunts = [];
	private static uint CurrentHuntTerritory;
	private static bool HasCurrentHuntTerritory;

	internal static ushort[] EurekaPreviousLiveIds => Volatile.Read(ref _eurekaPreviousLiveIdsSnapshot);

	private void TerritoryChanged(uint u) {
		if (DisableAutoTreasureOnTerritoryEnter(u)) Configuration.Save();
		if (InEureka(lastTerritoryId) || InEureka(ClientState.TerritoryType)) {
			Plugin.ElementalPositions.Clear();
			ElementalSet.Clear();
			lastFarmPos = null;
			FarmFull = false;
			foreach (var p in DeadFateDic)
			foreach (var k in p.Value.Keys)
				DeadFateDic[p.Key][k] = "-1";
		}
		lastTerritoryId = ClientState.TerritoryType;
		RefreshCurrentSpeedInfo(u, true);
		UpdateHuntData(ClientState.TerritoryType);
		_eurekaLiveIds.Clear();
		_eurekaPreviousLiveIds.Clear();
		Volatile.Write(ref _eurekaPreviousLiveIdsSnapshot, []);
		Volatile.Write(ref _cache, OverlayCache.Empty);
	}

	private static uint LastPotId;

	internal void Update(IFramework _) {
		var localPlayer = ObjectTable.LocalPlayer;
		if (!Configuration.PluginEnabled || localPlayer == null) {
			Volatile.Write(ref _cache, OverlayCache.Empty);
			return;
		}

		var territory = ClientState.TerritoryType;
		UpdateHuntData(territory);
		var isBetweenAreas = Condition[ConditionFlag.BetweenAreas] || Condition[ConditionFlag.BetweenAreas51];
		var isEureka = InEureka(territory) && !isBetweenAreas;
		EurekaUpdateResult? eurekaUpdate = isEureka ? UpdateEureka(territory) : null;
		if (!isBetweenAreas && InOccult(territory)) {
			foreach (var fate in Fates) {
				if (fate.FateId is not (1976 or 1977 or 2072 or 2073)) continue;
				if (fate.FateId != LastPotId) {
					LastPotId = fate.FateId;
					FindPot();
				}
				break;
			}
		}

		var cache = new OverlayCache(
			true,
			isEureka,
			territory,
			localPlayer.Position,
			lastFarmPos,
			[.. DetectedTreasurePositions],
			CollectTargetPositions(),
			[.. Plugin.ElementalPositions],
			eurekaUpdate?.Markers ?? [],
			CollectPalacePalMarkers(territory),
			eurekaUpdate?.WeatherNotice);
		Volatile.Write(ref _cache, cache);
	}

	private void OnBuildUi() {
		var cache = Volatile.Read(ref _cache);
		if (!cache.HasLocalPlayer || !Configuration.PluginEnabled) return;
		_bdl = ImGui.GetBackgroundDrawList(ImGui.GetMainViewport());
		if (cache.IsEureka) {
			RefreshMapOrigin();
			DrawMapOverlay(cache);
			if (Configuration.Overlay3DEnabled)
				foreach (var pos in cache.TreasurePositions)
					if (Gui.WorldToScreen(pos, out var v))
						_bdl.DrawMapDot(v, 0xFF00FFFF, 0xFF00FFFF);
		}
		if (cache.FarmPosition is { } farmPosition)
			if (Gui.WorldToScreen(farmPosition, out var v))
				_bdl.DrawMapDot(v, 0xFFFF0000, 0xFF00FF00, 10f);
		DrawPalacePal(cache);
		foreach (var en in cache.TargetPositions)
			if (Gui.WorldToScreen(cache.LocalPlayerPosition, out var v) && Gui.WorldToScreen(en, out var v2))
				_bdl.DrawLine(v, v2, 0x7F0000FF);
		if (Configuration.ShowMyPos) {
			Gui.WorldToScreen(cache.LocalPlayerPosition, out var p);
			_bdl.DrawMapDot(p, 0xFFFFFFFF, 0xFFFFFFFF, Configuration.ShowThickness);
		}
		const uint color = 0xFF00FF00;
		if (Configuration.ShowCircle) {
			var center = new Vector3(Configuration.ShowCenterX, Configuration.ShowCenterY, Configuration.ShowCenterZ);
			EnsureCircleOffsets(Configuration.CircleR);
			Vector2 last = new();
			for (var i = 0; i <= HighResCircleSegments; i++) {
				var p = _highResCircleOffsets[i];
				if (i == 0) {
					Gui.WorldToScreen(new(center.X + p.X, center.Y, center.Z + p.Y), out last);
					continue;
				}
				Gui.WorldToScreen(new(center.X + p.X, center.Y, center.Z + p.Y), out var now);
				_bdl.DrawLine(last, now, color, Configuration.ShowThickness);
				last = now;
			}
		}
		if (Configuration.ShowSquare) {
			var center = new Vector3(Configuration.ShowCenterX, Configuration.ShowCenterY, Configuration.ShowCenterZ);
			Gui.WorldToScreen(new(center.X - Configuration.SquareR, center.Y, center.Z + Configuration.SquareR), out var LT2);
			Gui.WorldToScreen(new(center.X + Configuration.SquareR, center.Y, center.Z + Configuration.SquareR), out var RT2);
			Gui.WorldToScreen(new(center.X - Configuration.SquareR, center.Y, center.Z - Configuration.SquareR), out var LB2);
			Gui.WorldToScreen(new(center.X + Configuration.SquareR, center.Y, center.Z - Configuration.SquareR), out var RB2);
			_bdl.DrawLine(LT2, RT2, color, Configuration.ShowThickness);
			_bdl.DrawLine(LT2, LB2, color, Configuration.ShowThickness);
			_bdl.DrawLine(RT2, RB2, color, Configuration.ShowThickness);
			_bdl.DrawLine(LB2, RB2, color, Configuration.ShowThickness);
		}
	}

	private (float, float)[]? _paramsPalacePal;
	private const int DefaultCircleSegments = 16, HighResCircleSegments = 50;
	private const float DefaultCircleSegmentFullRotation = 2 * MathF.PI / DefaultCircleSegments,
		HighResCircleSegmentFullRotation = 2 * MathF.PI / HighResCircleSegments;

	private void EnsureCircleOffsets(float radius) {
		if (_circleRadius == radius) return;
		_circleRadius = radius;
		for (var i = 0; i <= HighResCircleSegments; i++) {
			var currentRotation = i * HighResCircleSegmentFullRotation;
			_highResCircleOffsets[i] = new(radius * MathF.Sin(currentRotation), radius * MathF.Cos(currentRotation));
		}
	}

	private static void UpdateHuntData(uint territory) {
		if (HasCurrentHuntTerritory && CurrentHuntTerritory == territory && HuntData.Count != 0) return;
		if (HuntData.Count == 0)
			HuntData = JsonConvert.DeserializeObject<Dictionary<uint, List<Hunt>>>(
				File.ReadAllText(Path.Combine(PluginInterface.AssemblyLocation.Directory!.FullName, "hunt.json")))!;
		CurrentTerritoryHunts = HuntData.TryGetValue(territory, out var hunts) ? hunts : [];
		CurrentHuntTerritory = territory;
		HasCurrentHuntTerritory = true;
	}

	private IReadOnlyList<Vector3> CollectTargetPositions() {
		if (!string.Equals(_entityPatternSource, Configuration.FindEntity, StringComparison.Ordinal)) {
			_entityPatternSource = Configuration.FindEntity;
			_entityPatterns = string.IsNullOrEmpty(_entityPatternSource)
				? []
				: _entityPatternSource.Split('|');
		}
		var entityPatterns = _entityPatterns;
		var findPlayers = Configuration.FindCharaNiao || Configuration.FindCharaMao ||
		                  Configuration.FindCharaZhu || Configuration.FindCharaGou ||
		                  Configuration.FindRaceRenM || Configuration.FindRaceRenF ||
		                  Configuration.FindRaceJingLingM || Configuration.FindRaceJingLingF ||
		                  Configuration.FindRaceLaLaFeiErM || Configuration.FindRaceLaLaFeiErF ||
		                  Configuration.FindRaceMaoMeiM || Configuration.FindRaceMaoMeiF ||
		                  Configuration.FindRaceLuJiaM || Configuration.FindRaceLuJiaF ||
		                  Configuration.FindRaceAoLongM || Configuration.FindRaceAoLongF ||
		                  Configuration.FindRaceGeShiM || Configuration.FindRaceGeShiF ||
		                  Configuration.FindRaceWeiAiLaM || Configuration.FindRaceWeiAiLaF;
		var findHunts = Configuration.FindHunt &&
		                (Configuration.FindHuntB || Configuration.FindHuntA || Configuration.FindHuntS ||
		                 Configuration.FindHuntSS || Configuration.FindHuntSSMinion);
		if (entityPatterns.Length == 0 && !findPlayers && !findHunts) return Array.Empty<Vector3>();

		if (findHunts) {
			_huntIds.Clear();
			foreach (var hunt in CurrentTerritoryHunts)
				if (Configuration.FindHuntB && hunt.rank == "B" ||
				    Configuration.FindHuntA && hunt.rank == "A" ||
				    Configuration.FindHuntS && hunt.rank == "S" ||
				    Configuration.FindHuntSS && hunt.rank == "SS" ||
				    Configuration.FindHuntSSMinion && hunt.rank == "SSMinion")
					_huntIds.Add(hunt.id);
			if (_huntIds.Count == 0) findHunts = false;
		}

		var poses = new HashSet<Vector3>();
		foreach (var obj in ObjectTable) {
			if (entityPatterns.Length > 0) {
				var name = obj.Name.ToString();
				foreach (var pattern in entityPatterns)
					if (name.Contains(pattern)) {
						poses.Add(obj.Position);
						break;
					}
			}
			if (findPlayers && obj is IPlayerCharacter player) {
				var homeWorld = player.HomeWorld.Value.DataCenter.Value.Name.ToString();
				if (Configuration.FindCharaNiao && homeWorld == "陆行鸟" ||
				    Configuration.FindCharaMao && homeWorld == "猫小胖" ||
				    Configuration.FindCharaZhu && homeWorld == "莫古力" ||
				    Configuration.FindCharaGou && homeWorld == "豆豆柴" ||
				    Configuration.FindRaceRenM && player.CustomizeData is { Race: 1, Sex: 0 } ||
				    Configuration.FindRaceRenF && player.CustomizeData is { Race: 1, Sex: 1 } ||
				    Configuration.FindRaceJingLingM && player.CustomizeData is { Race: 2, Sex: 0 } ||
				    Configuration.FindRaceJingLingF && player.CustomizeData is { Race: 2, Sex: 1 } ||
				    Configuration.FindRaceLaLaFeiErM && player.CustomizeData is { Race: 3, Sex: 0 } ||
				    Configuration.FindRaceLaLaFeiErF && player.CustomizeData is { Race: 3, Sex: 1 } ||
				    Configuration.FindRaceMaoMeiM && player.CustomizeData is { Race: 4, Sex: 0 } ||
				    Configuration.FindRaceMaoMeiF && player.CustomizeData is { Race: 4, Sex: 1 } ||
				    Configuration.FindRaceLuJiaM && player.CustomizeData is { Race: 5, Sex: 0 } ||
				    Configuration.FindRaceLuJiaF && player.CustomizeData is { Race: 5, Sex: 1 } ||
				    Configuration.FindRaceAoLongM && player.CustomizeData is { Race: 6, Sex: 0 } ||
				    Configuration.FindRaceAoLongF && player.CustomizeData is { Race: 6, Sex: 1 } ||
				    Configuration.FindRaceGeShiM && player.CustomizeData is { Race: 7, Sex: 0 } ||
				    Configuration.FindRaceGeShiF && player.CustomizeData is { Race: 7, Sex: 1 } ||
				    Configuration.FindRaceWeiAiLaM && player.CustomizeData is { Race: 8, Sex: 0 } ||
				    Configuration.FindRaceWeiAiLaF && player.CustomizeData is { Race: 8, Sex: 1 })
					poses.Add(player.Position);
			}
			if (findHunts && obj is IBattleChara chara && _huntIds.Contains(chara.NameId))
				poses.Add(obj.Position);
		}
		return poses.Count == 0 ? Array.Empty<Vector3>() : poses.ToArray();
	}

	private IReadOnlyList<PalacePalMarker> CollectPalacePalMarkers(uint territory) {
		if (!Configuration.EnablePalacePal) return Array.Empty<PalacePalMarker>();
		if (PalacePalDatList.Count == 0) {
			PalacePalDatList.Clear();
			foreach (var sp in Ipcs.PalacePalData())
				PalacePalDatList.Add(new(
					uint.Parse(sp[0]),
					int.Parse(sp[1]),
					new(float.Parse(sp[2]), float.Parse(sp[3]), float.Parse(sp[4]))));
			PalacePalDatTerritoryIds = [.. PalacePalDatList.Select(i => i.territoryType)];
		}
		if (PalacePalDatList.Count == 0) return Array.Empty<PalacePalMarker>();
		if (_palacePalCacheReady && _palacePalCacheTerritory == territory) return _palacePalCache;
		_palacePalCacheTerritory = territory;
		_palacePalCacheReady = true;
		_palacePalCache = PalacePalDatTerritoryIds.Contains(territory)
			? [.. PalacePalDatList.Where(i => i.territoryType == territory).Select(i => new PalacePalMarker(i.type, i.position))]
			: Array.Empty<PalacePalMarker>();
		return _palacePalCache;
	}

	private readonly record struct EurekaUpdateResult(IReadOnlyList<EurekaMarker> Markers, string? WeatherNotice);

	private unsafe EurekaUpdateResult UpdateEureka(uint territoryType) {
		var territory = (Territory)territoryType;
		var now = DateTime.Now;
		(_eurekaLiveIds, _eurekaPreviousLiveIds) = (_eurekaPreviousLiveIds, _eurekaLiveIds);
		_eurekaLiveIds.Clear();
		_fateProgress.Clear();
		_fateCooldowns.Clear();
		foreach (var fate in Fates) {
			_eurekaLiveIds.Add(fate.FateId);
			if (!_eurekaPreviousLiveIds.Contains(fate.FateId)) {
				if (fate.FateId is 1367 or 1368 or 1407 or 1408 or 1425) TzFound(fate.FateId);
				else NmFound();
			}
			DeadFateDic[territory][fate.FateId] = "1";
			var progress = FateManager.Instance()->GetFateById(fate.FateId)->Progress;
			_fateProgress[fate.FateId] = progress;
			if (progress > 97)
				DeadFateDic[territory][fate.FateId] = now.ToString(CurrentCulture).Replace("/", "-");
		}
		foreach (var fate in DeadFateDic[territory]) {
			if (!fate.Value.Contains(':')) continue;
			var elapsed = new TimeSpan(now.Ticks - Convert.ToDateTime(fate.Value).Ticks);
			var minuteSpan = elapsed;
			if (minuteSpan.Hours >= 2 ||
			    territory == Territory.Pagos && fate.Key is 1367 or 1368 && minuteSpan.Minutes >= 7 ||
			    territory == Territory.Pyros && fate.Key is 1407 or 1408 && minuteSpan.Minutes >= 7 ||
			    territory == Territory.Hydatos && fate.Key is 1425 && minuteSpan.Minutes >= 7)
				DeadFateDic[territory][fate.Key] = "-1";
			else
				_fateCooldowns[fate.Key] = GetFateCooldown(territory, fate.Key, elapsed);
		}

		var regionWeather = Weathers[territory];
		var weatherNow = EorzeaWeather.GetCurrentWeatherInfo(regionWeather);
		_weatherDic.Clear();
		foreach (var weather in EorzeaWeather.GetAllWeathers(regionWeather)) {
			var timeLeft = EorzeaWeather.GetWeatherUptime(weather.Weather, regionWeather, now).End - now;
			_weatherDic.TryAdd(weather.Weather, (weather.Time.ToString(timeFormat), timeLeft.ToString(timeFormat)));
		}
		var eorzeaTime = EorzeaTime.ToEorzeaTime(now);
		var fates = XFates[territory];
		var markers = new EurekaMarker[fates.Length];
		for (var i = 0; i < fates.Length; i++)
			markers[i] = BuildEurekaMarker(fates[i], _fateProgress, _fateCooldowns, eorzeaTime, weatherNow.weather, _weatherDic);
		Volatile.Write(ref _eurekaPreviousLiveIdsSnapshot, [.. _eurekaPreviousLiveIds]);
		return new(markers, Configuration.Overlay2DWeatherMapEnabled ? BuildWeatherNotice(eorzeaTime) : null);
	}

	private static TimeSpan GetFateCooldown(Territory territory, int fateId, TimeSpan elapsed) => territory switch {
		Territory.Anemos => new TimeSpan(2, 0, 0) - elapsed,
		Territory.Pagos => fateId is not (1367 or 1368)
			? new TimeSpan(2, 0, 0) - elapsed
			: new TimeSpan(0, 7, 0) - elapsed,
		Territory.Pyros => fateId is not (1407 or 1408)
			? new TimeSpan(2, 0, 0) - elapsed
			: new TimeSpan(0, 7, 0) - elapsed,
		Territory.Hydatos => fateId != 1425
			? fateId is not (1422 or 1424)
				? new TimeSpan(2, 0, 0) - elapsed
				: new TimeSpan(0, 20, 0) - elapsed
			: new TimeSpan(0, 7, 0) - elapsed
	};

	private static EurekaMarker BuildEurekaMarker(
		EurekaFate fate,
		IReadOnlyDictionary<ushort, int> fateProgress,
		IReadOnlyDictionary<int, TimeSpan> fateCooldowns,
		EorzeaTime eorzeaTime,
		EurekaWeather currentWeather,
		IReadOnlyDictionary<EurekaWeather, (string Start, string Left)> weatherDic) {
		var worldPosition = Pos2Map(fate.FatePosition);
		var name = fate.BossShortName;
		if (fateProgress.TryGetValue(fate.FateId, out var progress)) {
			var text = progress > 0 ? $"{name}({progress}%)" : name;
			return new(worldPosition, text, 0xFF00E000);
		}

		if (fateCooldowns.TryGetValue(fate.FateId, out var timeFromCanTriggered)) {
			if (fate is { SpawnRequiredWeather: EurekaWeather.None, SpawnByRequiredNight: false })
				return CreateEurekaMarker(worldPosition, name, timeFromCanTriggered, true);
			if (fate.SpawnRequiredWeather == EurekaWeather.None) {
				var eorzeaHour = int.Parse(eorzeaTime.EorzeaDateTime.ToString("%H"));
				if (eorzeaHour is < 6 or >= 18)
					return CreateEurekaMarker(worldPosition, name, timeFromCanTriggered, true);
				var timeFromNight = eorzeaTime.TimeUntilNight();
				return CreateEurekaMarker(worldPosition, name, timeFromNight < timeFromCanTriggered ? timeFromCanTriggered : timeFromNight, true);
			}
			if (!weatherDic.TryGetValue(fate.SpawnRequiredWeather, out var weather) ||
			    currentWeather == fate.SpawnRequiredWeather ||
			    TimeSpan.Parse(weather.Start) < timeFromCanTriggered)
				return CreateEurekaMarker(worldPosition, name, timeFromCanTriggered, true);
			return CreateEurekaMarker(worldPosition, name, weather.Start, true);
		}

		if (fate is { SpawnRequiredWeather: EurekaWeather.None, SpawnByRequiredNight: false })
			return CreateEurekaMarker(worldPosition, name, null, false);
		if (fate.SpawnRequiredWeather == EurekaWeather.None) {
			var eorzeaHour = int.Parse(eorzeaTime.EorzeaDateTime.ToString("%H"));
			if (eorzeaHour is < 6 or >= 18)
				return CreateEurekaMarker(worldPosition, name, eorzeaTime.TimeUntilDay(), false);
			return CreateEurekaMarker(worldPosition, name, eorzeaTime.TimeUntilNight(), true);
		}
		if (!fate.SpawnByRequiredNight) {
			if (!weatherDic.TryGetValue(fate.SpawnRequiredWeather, out var weather))
				return CreateEurekaMarker(worldPosition, name, null, false);
			if (currentWeather == fate.SpawnRequiredWeather)
				return CreateEurekaMarker(worldPosition, name, weather.Left, false);
			return CreateEurekaMarker(worldPosition, name, weather.Start, true);
		}
		if (!weatherDic.TryGetValue(fate.SpawnRequiredWeather, out var nightWeather))
			return CreateEurekaMarker(worldPosition, name, null, false);
		var hour = int.Parse(eorzeaTime.EorzeaDateTime.ToString("%H"));
		if ((currentWeather == fate.SpawnRequiredWeather) && hour is < 6 or >= 18) {
			var timeFromDay = eorzeaTime.TimeUntilDay();
			return CreateEurekaMarker(
				worldPosition,
				name,
				timeFromDay.Ticks < TimeSpan.Parse(nightWeather.Left).Ticks ? timeFromDay : nightWeather.Left,
				false);
		}
		if (hour is >= 6 and < 18)
			return CreateEurekaMarker(worldPosition, name, eorzeaTime.TimeUntilNight(), true);
		return CreateEurekaMarker(worldPosition, name, nightWeather.Start, true);
	}

	private static EurekaMarker CreateEurekaMarker(Vector3 worldPosition, string name, object? detail, bool dim) {
		var text = Configuration.Overlay2DDetailEnabled && detail != null
			? $"{name}\n{detail switch {
				TimeSpan time => time.ToString(timeFormat),
				_ => detail
			}}"
			: name;
		var color = Configuration.Overlay2DDetailEnabled && dim ? 0xFF808080u : uint.MaxValue;
		return new(worldPosition, text, color);
	}

	private static string BuildWeatherNotice(EorzeaTime eorzeaTime) {
		var notice = new StringBuilder();
		var etimeHour = int.Parse(eorzeaTime.EorzeaDateTime.ToString("%H"));
		notice.Append("风岛：强风");
		var o = EorzeaWeather.GetAllWeathers(Weathers[Territory.Anemos]).First(i => i.Weather == EurekaWeather.Gales);
		if (EorzeaWeather.GetCurrentWeatherInfo(Weathers[Territory.Anemos]).weather != EurekaWeather.Gales)
			notice.Append("×(").Append(o.Time.ToString(timeFormat));
		else
			notice.Append("○(").Append((EorzeaWeather.GetWeatherUptime(o.Weather, Weathers[Territory.Anemos], DateTime.Now).End - DateTime.Now).ToString(timeFormat));
		notice.Append(")    夜晚");
		if (etimeHour is < 6 or >= 18)
			notice.Append("○(").Append(eorzeaTime.TimeUntilDay().ToString(timeFormat));
		else
			notice.Append("×(").Append(eorzeaTime.TimeUntilNight().ToString(timeFormat));
		notice.Append(")\n冰岛：暴雪");
		var pagosweatherNow = EorzeaWeather.GetCurrentWeatherInfo(Weathers[Territory.Pagos]);
		var next = EorzeaWeather.GetAllWeathers(Weathers[Territory.Pagos]);
		o = next.First(i => i.Weather == EurekaWeather.Blizzards);
		if (pagosweatherNow.weather != EurekaWeather.Blizzards)
			notice.Append("×(").Append(o.Time.ToString(timeFormat));
		else
			notice.Append("○(").Append((EorzeaWeather.GetWeatherUptime(o.Weather, Weathers[Territory.Pagos], DateTime.Now).End - DateTime.Now).ToString(timeFormat));
		notice.Append(")    薄雾");
		o = next.First(i => i.Weather == EurekaWeather.Fog);
		if (pagosweatherNow.weather != EurekaWeather.Fog)
			notice.Append("×(").Append(o.Time.ToString(timeFormat));
		else
			notice.Append("○(").Append((EorzeaWeather.GetWeatherUptime(o.Weather, Weathers[Territory.Pagos], DateTime.Now).End - DateTime.Now).ToString(timeFormat));
		notice.Append(")\n火岛：暴雪");
		next = EorzeaWeather.GetAllWeathers(Weathers[Territory.Pyros]);
		o = next.First(i => i.Weather == EurekaWeather.Blizzards);
		if (pagosweatherNow.weather != EurekaWeather.Blizzards)
			notice.Append("×(").Append(o.Time.ToString(timeFormat));
		else
			notice.Append("○(").Append((EorzeaWeather.GetWeatherUptime(o.Weather, Weathers[Territory.Pyros], DateTime.Now).End - DateTime.Now).ToString(timeFormat));
		notice.Append(")    热浪");
		o = next.First(i => i.Weather == EurekaWeather.HeatWaves);
		if (pagosweatherNow.weather != EurekaWeather.Blizzards)
			notice.Append("×(").Append(o.Time.ToString(timeFormat));
		else
			notice.Append("○(").Append((EorzeaWeather.GetWeatherUptime(o.Weather, Weathers[Territory.Pagos], DateTime.Now).End - DateTime.Now).ToString(timeFormat));
		notice.Append(')');
		return notice.ToString();
	}

	private void DrawMapOverlay(OverlayCache cache) {
		if (_mapOrigin == null) return;
		var valueOrDefault = _mapOrigin.GetValueOrDefault();
		if (valueOrDefault == Vector2.Zero || cache.TerritoryType == 0) return;
		_bdl.PushClipRect(_mapPosSize[0], _mapPosSize[1]);
		foreach (var marker in cache.EurekaMarkers)
			_bdl.DrawText(WorldToMap(valueOrDefault, marker.WorldPosition, cache.LocalPlayerPosition), marker.Text, marker.Color);
		if (Configuration.Overlay2DWeatherMapEnabled && cache.WeatherNotice != null)
			_bdl.DrawText(WorldToMap(valueOrDefault, (Territory)cache.TerritoryType switch {
				Territory.Anemos or Territory.Pagos => new(-9.1946f, 0f, 584.4f),
				Territory.Pyros => new(0.2181f, 0f, 865.32275f),
				Territory.Hydatos => new(89.62729f, 0f, -1241.035f)
			}, cache.LocalPlayerPosition), cache.WeatherNotice, uint.MaxValue);
		foreach (var e in cache.ElementalPositions)
			_bdl.DrawText(WorldToMap(valueOrDefault, e, cache.LocalPlayerPosition), "元灵", 0xFF0000FF);
		if (Configuration.ShowCurrentElemental) {
			foreach (var p in PData.ElementalPositions[(Territory)cache.TerritoryType])
				_bdl.DrawMapDot(WorldToMap(valueOrDefault, p, cache.LocalPlayerPosition), 0x7FFFFF00, 0x7F000000, 15);
		}
		_bdl.PopClipRect();
	}

	private void DrawPalacePal(OverlayCache cache) {
		if (cache.PalacePalMarkers.Count == 0) return;
		if (_paramsPalacePal == null) {
			const float radius = 2.5f;
			_paramsPalacePal = new (float, float)[DefaultCircleSegments + 1];
			for (var i = 0; i <= DefaultCircleSegments; i++) {
				var currentRotation = i * DefaultCircleSegmentFullRotation;
				_paramsPalacePal[i] = (radius * MathF.Sin(currentRotation), radius * MathF.Cos(currentRotation));
			}
		}
		foreach (var marker in cache.PalacePalMarkers) {
			for (var i = 0; i <= DefaultCircleSegments; i++) {
				var p = _paramsPalacePal[i];
				Gui.WorldToScreen(new(marker.Position.X + p.Item1, marker.Position.Y, marker.Position.Z + p.Item2), out var segment);
				_bdl.PathLineTo(segment);
			}
			_bdl.PathFillConvex(marker.Type == 1 ? 0x50FFFF00u : 0x500000FFu);
			_bdl.PathClear();
		}
	}

	private unsafe void RefreshMapOrigin() {
		_mapOrigin = null;
		if (!MapVisible) return;
		var areaMapAddon = AreaMapAddon;
		if (areaMapAddon == null) return;
		_globalUiScale = areaMapAddon->Scale;
		_mapScale = areaMapAddon->AreaMap.MapScale;
		var areaMapAddonUldManager = areaMapAddon->UldManager;
		if (areaMapAddonUldManager.NodeListCount <= 4) return;
		var ptr = (AtkComponentNode*)areaMapAddonUldManager.NodeList[3];
		if (ptr == null || ptr->Component == null) return;
		var atkResNode = ptr->AtkResNode;
		var ptrUldManager = ptr->Component->UldManager;
		if (ptrUldManager.NodeListCount < 233) return;
		var ptrUldManagerNodeList = ptrUldManager.NodeList;
		for (var i = 6; i < ptrUldManager.NodeListCount - 1; i++) {
			var item = ptrUldManagerNodeList[i];
			if (item == null || !item->IsVisible()) continue;
			var itemx = (AtkComponentNode*)item;
			if (itemx->Component == null || itemx->Component->UldManager.NodeListCount <= 4) continue;
			var ptr3 = (AtkImageNode*)itemx->Component->UldManager.NodeList[4];
			if (ptr3 == null) continue;
			var atkResNode2 = itemx->AtkResNode;
			string? text = null;
			var ptr3PartsList = ptr3->PartsList;
			if (ptr3PartsList != null && ptr3PartsList->Parts != null && ptr3->PartId < ptr3PartsList->PartCount) {
				var part = ptr3PartsList->Parts + ptr3->PartId;
				if (part->UldAsset == null) continue;
				var AtkTexture = part->UldAsset->AtkTexture;
				if (AtkTexture.TextureType == TextureType.Resource && AtkTexture.Resource != null && AtkTexture.Resource->TexFileResourceHandle != null)
					text = Path.GetFileName(AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName.ToString());
			}
			if (text is not ("060443.tex" or "060443_hr1.tex")) continue;
			var basev = ImGui.GetMainViewport().Pos + new Vector2(areaMapAddon->X, areaMapAddon->Y);
			var atkResNodeV = new Vector2(atkResNode.X, atkResNode.Y);
			_mapOrigin = basev + (atkResNodeV + new Vector2(atkResNode2.X, atkResNode2.Y) + new Vector2(atkResNode2.OriginX, atkResNode2.OriginY)) * _globalUiScale;
			_mapPosSize[0] = basev + atkResNodeV * _globalUiScale;
			_mapPosSize[1] = basev + atkResNodeV + new Vector2(atkResNode.Width, atkResNode.Height) * _globalUiScale;
			break;
		}
	}

	private Vector2 WorldToMap(Vector2 origin, Vector3 worldVector3, Vector3 localPlayerPosition) =>
		origin + ToVector2(worldVector3 - localPlayerPosition) * _mapScale * _globalUiScale;

	internal static void NmFound() {
		Player1.Stop();
		Player1.SoundLocation = Path.Combine(PluginInterface.AssemblyLocation.Directory!.FullName, "nm.wav");
		Player1.Load();
		Player1.Play();
	}

	private static void TzFound(int fateid) {
		Player2.Stop();
		Player2.SoundLocation = Path.Combine(PluginInterface.AssemblyLocation.Directory!.FullName, "tz.wav");
		Player2.Load();
		Player2.Play();
		FindRabbit(fateid);
	}
}
