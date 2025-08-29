using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.Sheets;
using SkyEye.SkyEye.Data;
using static System.Globalization.CultureInfo;

namespace SkyEye.SkyEye;

public class UiBuilder : IDisposable {
	private static readonly SoundPlayer Player1 = new();

	private static readonly SoundPlayer Player2 = new();
	private readonly List<(Vector3 worldpos, uint fgcolor, uint bgcolor, string name, string fateId, PData.EurekaWeather SpawnRequiredWeather, bool SpawnByRequiredNight)> _eurekaList2D = [];
	private readonly List<string> _eurekaLiveIdList2D = [];

	private readonly List<string> _eurekaLiveIdList2DOld = [];

	private readonly Vector2[] _mapPosSize = new Vector2[2];
	private readonly IDalamudPluginInterface _pi;

	private readonly Plugin _pl;

	private readonly Dictionary<uint, ushort> _sizeFactorDict;

	private ImDrawListPtr _bdl;

	private EorzeaTime _eorzeaTime;

	private float _globalUiScale = 1f;

	private Vector2? _mapOrigin = Vector2.Zero;

	private Dictionary<PData.EurekaWeather, (string, string)> _weatherDic;

	private (PData.EurekaWeather Weather, TimeSpan Time) _weatherNow;

	private List<(PData.EurekaWeather Weather, TimeSpan Time)> _weathers;

	public UiBuilder(Plugin plugin, IDalamudPluginInterface pluginInterface) {
		_pi = pluginInterface;
		_pl = plugin;
		_sizeFactorDict = Plugin.DataManager.GetExcelSheet<TerritoryType>().ToDictionary(k => k.RowId, v => v.Map.Value.SizeFactor);
		Plugin.ClientState.TerritoryChanged += TerritoryChanged;
		_pi.UiBuilder.Draw += UiBuilder_OnBuildUi;
		Plugin.Framework.Update += OnUpdate;
	}

	public void Dispose() {
		_pi.UiBuilder.Draw -= UiBuilder_OnBuildUi;
		Plugin.ClientState.TerritoryChanged -= TerritoryChanged;
	}

	private static void TerritoryChanged(ushort territoryId) {
		Plugin.Log.Info($"territory changed to: {territoryId}");
		foreach (var k in EurekaAnemos.DeadFateDic.Keys) EurekaAnemos.DeadFateDic[k] = "-1";
		foreach (var k in EurekaPagos.DeadFateDic.Keys) EurekaPagos.DeadFateDic[k] = "-1";
		foreach (var k in EurekaPyros.DeadFateDic.Keys) EurekaPyros.DeadFateDic[k] = "-1";
		foreach (var k in EurekaHydatos.DeadFateDic.Keys) EurekaHydatos.DeadFateDic[k] = "-1";
	}

	private void OnUpdate(IFramework f) {
	}

	private void UiBuilder_OnBuildUi() {
		var flag = false;
		try {
			flag = Plugin.ClientState.LocalPlayer != null &&
			       (Plugin.ClientState.TerritoryType == 732 || Plugin.ClientState.TerritoryType == 763 || Plugin.ClientState.TerritoryType == 795 || Plugin.ClientState.TerritoryType == 827 || Plugin.ClientState.TerritoryType == 628) &&
			       !Plugin.Condition[ConditionFlag.BetweenAreas] && !Plugin.Condition[ConditionFlag.BetweenAreas51];
		}
		catch (Exception) {
			// ignored
		}
		if (flag) {
			_eorzeaTime = EorzeaTime.ToEorzeaTime(DateTime.Now);
			_bdl = ImGui.GetBackgroundDrawList(ImGui.GetMainViewport());
			RefreshEureka();
			if (Plugin.Configuration.Overlay2DEnabled) DrawMapOverlay();
			if (Plugin.Configuration.Overlay3DEnabled)
				foreach (var pos in _pl.DetectedTreasurePositions)
					if (Plugin.Gui.WorldToScreen(pos, out var v))
						_bdl.DrawMapDot(v, 0xFF00FFFFu, 0xFF00FFFFu);
		}
		_eurekaList2D.Clear();
		foreach (var item in _eurekaLiveIdList2D) _eurekaLiveIdList2DOld.Add(item);
		_eurekaLiveIdList2D.Clear();
	}

	private void RefreshEureka() {
		switch (Plugin.ClientState.TerritoryType) {
			case 732:
				foreach (var o7 in Plugin.Fates) {
					_eurekaLiveIdList2D.Add(o7.FateId.ToString());
					if (!_eurekaLiveIdList2DOld.Contains(o7.FateId.ToString())) NmFound();
					EurekaAnemos.DeadFateDic[o7.FateId] = "1";
				}
				_eurekaLiveIdList2DOld.Clear();
				foreach (var o8 in from o8 in EurekaAnemos.DeadFateDic where o8.Value.Contains(':') let minuteSpan4 = new TimeSpan(DateTime.Now.Ticks - Convert.ToDateTime(o8.Value).Ticks) where minuteSpan4.Hours >= 2 select o8)
					EurekaAnemos.DeadFateDic[o8.Key] = "-1";
				break;
			case 763:
				foreach (var o3 in Plugin.Fates) {
					_eurekaLiveIdList2D.Add(o3.FateId.ToString());
					if (!_eurekaLiveIdList2DOld.Contains(o3.FateId.ToString())) {
						if (o3.FateId is 1367 or 1368) TzFound();
						else NmFound();
					}
					EurekaPagos.DeadFateDic[o3.FateId] = "1";
				}
				_eurekaLiveIdList2DOld.Clear();
				foreach (var o4 in from o4 in EurekaPagos.DeadFateDic
				         where o4.Value.Contains(':')
				         let minuteSpan2 = new TimeSpan(DateTime.Now.Ticks - Convert.ToDateTime(o4.Value).Ticks)
				         where o4.Key is 1367 or 1368 && minuteSpan2.Minutes >= 7 || minuteSpan2.Hours >= 2
				         select o4)
					EurekaPagos.DeadFateDic[o4.Key] = "-1";
				break;
			case 795:
				foreach (var o5 in Plugin.Fates) {
					_eurekaLiveIdList2D.Add(o5.FateId.ToString());
					if (!_eurekaLiveIdList2DOld.Contains(o5.FateId.ToString())) {
						if (o5.FateId is 1407 or 1408) TzFound();
						else NmFound();
					}
					EurekaPyros.DeadFateDic[o5.FateId] = "1";
				}
				_eurekaLiveIdList2DOld.Clear();
				foreach (var o6 in from o6 in EurekaPyros.DeadFateDic
				         where o6.Value.Contains(':')
				         let minuteSpan3 = new TimeSpan(DateTime.Now.Ticks - Convert.ToDateTime(o6.Value).Ticks)
				         where o6.Key is 1407 or 1408 && minuteSpan3.Minutes >= 7 || minuteSpan3.Hours >= 2
				         select o6)
					EurekaPyros.DeadFateDic[o6.Key] = "-1";
				break;
			case 827:
				foreach (var o in Plugin.Fates) {
					_eurekaLiveIdList2D.Add(o.FateId.ToString());
					if (!_eurekaLiveIdList2DOld.Contains(o.FateId.ToString())) {
						if (o.FateId == 1425) TzFound();
						else NmFound();
					}
					EurekaHydatos.DeadFateDic[o.FateId] = "1";
				}
				_eurekaLiveIdList2DOld.Clear();
				foreach (var o2 in from o2 in EurekaHydatos.DeadFateDic
				         where o2.Value.Contains(':')
				         let minuteSpan = new TimeSpan(DateTime.Now.Ticks - Convert.ToDateTime(o2.Value).Ticks)
				         where o2.Key == 1425 && minuteSpan.Minutes >= 7 || minuteSpan.Hours >= 2
				         select o2)
					EurekaHydatos.DeadFateDic[o2.Key] = "-1";
				break;
		}
		switch (Plugin.ClientState.TerritoryType) {
			case 732: {
				_weathers = EurekaAnemos.GetAllNextWeatherTime();
				_weatherNow = EurekaAnemos.GetCurrentWeatherInfo();
				_weatherDic = new Dictionary<PData.EurekaWeather, (string, string)>();
				foreach (var o9 in _weathers) {
					var timeLeft = EorzeaWeather.GetWeatherUptime(o9.Weather, EurekaAnemos.Weathers, DateTime.Now).End - DateTime.Now;
					_weatherDic.Add(o9.Weather, (o9.Time.ToString(@"hh\:mm\:ss"), timeLeft.ToString(@"hh\:mm\:ss")));
				}
				var map = Plugin.DataManager.GetExcelSheet<Map>().GetRow(732u);
				foreach (var o10 in EurekaAnemos.AnemosFates)
					_eurekaList2D.Add((ToVector3(MapToWorld(o10.FatePosition, map)), uint.MaxValue, uint.MaxValue, o10.BossShortName, o10.FateId.ToString(), o10.SpawnRequiredWeather, o10.SpawnByRequiredNight));
				return;
			}
			case 763: {
				_weathers = EurekaPagos.GetAllNextWeatherTime();
				_weatherNow = EurekaPagos.GetCurrentWeatherInfo();
				_weatherDic = new Dictionary<PData.EurekaWeather, (string, string)>();
				foreach (var o11 in _weathers) {
					var timeLeft2 = EorzeaWeather.GetWeatherUptime(o11.Weather, EurekaPagos.Weathers, DateTime.Now).End - DateTime.Now;
					_weatherDic.Add(o11.Weather, (o11.Time.ToString(@"hh\:mm\:ss"), timeLeft2.ToString(@"hh\:mm\:ss")));
				}
				Plugin.DataManager.GetExcelSheet<Map>().GetRow(763u);
				foreach (var o12 in EurekaPagos.PagosFates)
					_eurekaList2D.Add((ToVector3(o12.FatePosition), uint.MaxValue, uint.MaxValue, o12.BossShortName, o12.FateId.ToString(), o12.SpawnRequiredWeather, o12.SpawnByRequiredNight));
				return;
			}
			case 795: {
				_weathers = EurekaPyros.GetAllNextWeatherTime();
				_weatherNow = EurekaPyros.GetCurrentWeatherInfo();
				_weatherDic = new Dictionary<PData.EurekaWeather, (string, string)>();
				foreach (var o13 in _weathers) {
					var timeLeft3 = EorzeaWeather.GetWeatherUptime(o13.Weather, EurekaPyros.Weathers, DateTime.Now).End - DateTime.Now;
					_weatherDic.Add(o13.Weather, (o13.Time.ToString(@"hh\:mm\:ss"), timeLeft3.ToString(@"hh\:mm\:ss")));
				}
				Plugin.DataManager.GetExcelSheet<Map>().GetRow(795u);
				foreach (var o14 in EurekaPyros.PyrosFates)
					_eurekaList2D.Add((ToVector3(o14.FatePosition), uint.MaxValue, uint.MaxValue, o14.BossShortName, o14.FateId.ToString(), o14.SpawnRequiredWeather, o14.SpawnByRequiredNight));
				return;
			}
			case 827: {
				_weathers = EurekaHydatos.GetAllNextWeatherTime();
				_weatherNow = EurekaHydatos.GetCurrentWeatherInfo();
				_weatherDic = new Dictionary<PData.EurekaWeather, (string, string)>();
				foreach (var o15 in _weathers) {
					var timeLeft4 = EorzeaWeather.GetWeatherUptime(o15.Weather, EurekaHydatos.Weathers, DateTime.Now).End - DateTime.Now;
					_weatherDic.Add(o15.Weather, (o15.Time.ToString(@"hh\:mm\:ss"), timeLeft4.ToString(@"hh\:mm\:ss")));
				}
				Plugin.DataManager.GetExcelSheet<Map>().GetRow(827u);
				foreach (var o16 in EurekaHydatos.HydatosFates)
					_eurekaList2D.Add((ToVector3(o16.FatePosition), uint.MaxValue, uint.MaxValue, o16.BossShortName, o16.FateId.ToString(), o16.SpawnRequiredWeather, o16.SpawnByRequiredNight));
				return;
			}
		}
	}

	private unsafe void DrawMapOverlay() {
		RefreshMapOrigin();
		var vector = _mapOrigin;
		if (!vector.HasValue) return;
		var valueOrDefault = vector.GetValueOrDefault();
		if (!(valueOrDefault != Vector2.Zero) || Plugin.ClientState.TerritoryType == 0) return;
		_bdl.PushClipRect(_mapPosSize[0], _mapPosSize[1]);
		foreach (var item2 in _eurekaList2D) {
			var pos2 = WorldToMap(valueOrDefault, item2.worldpos);
			if (_eurekaLiveIdList2D.Contains(item2.fateId)) {
				var fateProgress = FateManager.Instance()->GetFateById(ushort.Parse(item2.fateId))->Progress;
				if (fateProgress > 0) _bdl.DrawText(pos2, item2.name + "(" + fateProgress + "%)", 4278247424u, true);
				else _bdl.DrawText(pos2, item2.name, 0xFF00E000u, true);
				if (fateProgress > 97)
					switch (Plugin.ClientState.TerritoryType) {
						case 732:
							EurekaAnemos.DeadFateDic[ushort.Parse(item2.fateId)] = DateTime.Now.ToString(CurrentCulture).Replace("/", "-");
							break;
						case 763:
							EurekaPagos.DeadFateDic[ushort.Parse(item2.fateId)] = DateTime.Now.ToString(CurrentCulture).Replace("/", "-");
							break;
						case 795:
							EurekaPyros.DeadFateDic[ushort.Parse(item2.fateId)] = DateTime.Now.ToString(CurrentCulture).Replace("/", "-");
							break;
						case 827:
							EurekaHydatos.DeadFateDic[ushort.Parse(item2.fateId)] = DateTime.Now.ToString(CurrentCulture).Replace("/", "-");
							break;
						default:
							Plugin.Log.Info("not in Eureka.");
							break;
					}
			}
			else if (Plugin.ClientState.TerritoryType == 732 && EurekaAnemos.DeadFateDic[ushort.Parse(item2.fateId)].Contains(':') || Plugin.ClientState.TerritoryType == 763 && EurekaPagos.DeadFateDic[ushort.Parse(item2.fateId)].Contains(':') ||
			         Plugin.ClientState.TerritoryType == 795 && EurekaPyros.DeadFateDic[ushort.Parse(item2.fateId)].Contains(':') || Plugin.ClientState.TerritoryType == 827 && EurekaHydatos.DeadFateDic[ushort.Parse(item2.fateId)].Contains(':')) {
				TimeSpan timeFromCanTriggered;
				switch (Plugin.ClientState.TerritoryType) {
					case 732:
						timeFromCanTriggered = new TimeSpan(2, 0, 0) - (DateTime.Now - Convert.ToDateTime(EurekaAnemos.DeadFateDic[ushort.Parse(item2.fateId)]));
						break;
					case 763:
						timeFromCanTriggered = !"1367".Equals(item2.fateId) && !"1368".Equals(item2.fateId)
							? new TimeSpan(2, 0, 0) - (DateTime.Now - Convert.ToDateTime(EurekaPagos.DeadFateDic[ushort.Parse(item2.fateId)]))
							: new TimeSpan(0, 7, 0) - (DateTime.Now - Convert.ToDateTime(EurekaPagos.DeadFateDic[ushort.Parse(item2.fateId)]));
						break;
					case 795:
						timeFromCanTriggered = !"1407".Equals(item2.fateId) && !"1408".Equals(item2.fateId)
							? new TimeSpan(2, 0, 0) - (DateTime.Now - Convert.ToDateTime(EurekaPyros.DeadFateDic[ushort.Parse(item2.fateId)]))
							: new TimeSpan(0, 7, 0) - (DateTime.Now - Convert.ToDateTime(EurekaPyros.DeadFateDic[ushort.Parse(item2.fateId)]));
						break;
					case 827:
						timeFromCanTriggered = !"1425".Equals(item2.fateId)
							? !"1422".Equals(item2.fateId) && !"1424".Equals(item2.fateId)
								? new TimeSpan(2, 0, 0) - (DateTime.Now - Convert.ToDateTime(EurekaHydatos.DeadFateDic[ushort.Parse(item2.fateId)]))
								: new TimeSpan(0, 20, 0) - (DateTime.Now - Convert.ToDateTime(EurekaHydatos.DeadFateDic[ushort.Parse(item2.fateId)]))
							: new TimeSpan(0, 7, 0) - (DateTime.Now - Convert.ToDateTime(EurekaHydatos.DeadFateDic[ushort.Parse(item2.fateId)]));
						break;
					default:
						Plugin.Log.Info("not in Eureka.");
						timeFromCanTriggered = TimeSpan.Zero;
						break;
				}
				if (item2 is { SpawnRequiredWeather: PData.EurekaWeather.None, SpawnByRequiredNight: false }) _bdl.DrawText(pos2, item2.name + "\n" + timeFromCanTriggered.ToString(@"hh\:mm\:ss"), 4286611584u, true);
				else if (item2.SpawnRequiredWeather == PData.EurekaWeather.None) {
					var etimeHour = int.Parse(_eorzeaTime.EorzeaDateTime.ToString("%H"));
					if (etimeHour is < 6 or >= 18) {
						_bdl.DrawText(pos2, item2.name + "\n" + timeFromCanTriggered.ToString(@"hh\:mm\:ss"), 4286611584u, true);
						continue;
					}
					var timeFromNight = _eorzeaTime.TimeUntilNight();
					if (timeFromNight < timeFromCanTriggered) _bdl.DrawText(pos2, item2.name + "\n" + timeFromCanTriggered.ToString(@"hh\:mm\:ss"), 4286611584u, true);
					else _bdl.DrawText(pos2, item2.name + "\n" + timeFromNight.ToString(@"hh\:mm\:ss"), 4286611584u, true);
				}
				else if (!_weatherDic.TryGetValue(item2.SpawnRequiredWeather, out var value) || _weatherNow.Weather == item2.SpawnRequiredWeather) _bdl.DrawText(pos2, item2.name + "\n" + timeFromCanTriggered.ToString(@"hh\:mm\:ss"), 4286611584u, true);
				else if (TimeSpan.Parse(value.Item1) < timeFromCanTriggered) _bdl.DrawText(pos2, item2.name + "\n" + timeFromCanTriggered.ToString(@"hh\:mm\:ss"), 4286611584u, true);
				else _bdl.DrawText(pos2, item2.name + "\n" + value.Item1, 4286611584u, true);
			}
			else if (item2 is { SpawnRequiredWeather: PData.EurekaWeather.None, SpawnByRequiredNight: false }) _bdl.DrawText(pos2, item2.name, item2.fgcolor, true);
			else if (item2 is { SpawnRequiredWeather: PData.EurekaWeather.None, SpawnByRequiredNight: true }) {
				var etimeHour2 = int.Parse(_eorzeaTime.EorzeaDateTime.ToString("%H"));
				if (etimeHour2 is < 6 or >= 18) {
					var timeFromDay = _eorzeaTime.TimeUntilDay();
					_bdl.DrawText(pos2, item2.name + "\n" + timeFromDay.ToString(@"hh\:mm\:ss"), item2.fgcolor, true);
				}
				else {
					var timeFromNight2 = _eorzeaTime.TimeUntilNight();
					_bdl.DrawText(pos2, item2.name + "\n" + timeFromNight2.ToString(@"hh\:mm\:ss"), 4286611584u, true);
				}
			}
			else if (item2.SpawnRequiredWeather != PData.EurekaWeather.None && !item2.SpawnByRequiredNight) {
				if (!_weatherDic.TryGetValue(item2.SpawnRequiredWeather, out var value) || _weatherNow.Weather == item2.SpawnRequiredWeather) _bdl.DrawText(pos2, item2.name + "\n" + _weatherDic[item2.SpawnRequiredWeather].Item2, item2.fgcolor, true);
				else _bdl.DrawText(pos2, item2.name + "\n" + value.Item1, 4286611584u, true);
			}
			else {
				if (item2.SpawnRequiredWeather == PData.EurekaWeather.None || !item2.SpawnByRequiredNight) continue;
				var etimeHour3 = int.Parse(_eorzeaTime.EorzeaDateTime.ToString("%H"));
				var weatherLeftTime = _weatherDic[item2.SpawnRequiredWeather].Item2;
				if ((!_weatherDic.ContainsKey(item2.SpawnRequiredWeather) || _weatherNow.Weather == item2.SpawnRequiredWeather) && etimeHour3 is < 6 or >= 18) {
					var timeFromDay2 = _eorzeaTime.TimeUntilDay();
					if (timeFromDay2.Ticks < TimeSpan.Parse(weatherLeftTime).Ticks) _bdl.DrawText(pos2, item2.name + "\n" + timeFromDay2.ToString(@"hh\:mm\:ss"), item2.fgcolor, true);
					else _bdl.DrawText(pos2, item2.name + "\n" + weatherLeftTime, item2.fgcolor, true);
				}
				else if (etimeHour3 is >= 6 and < 18) {
					var timeFromNight3 = _eorzeaTime.TimeUntilNight();
					_bdl.DrawText(pos2, item2.name + "\n" + timeFromNight3.ToString(@"hh\:mm\:ss"), 4286611584u, true);
				}
				else _bdl.DrawText(pos2, item2.name + "\n" + _weatherDic[item2.SpawnRequiredWeather].Item1, 4286611584u, true);
			}
		}
		if (Plugin.Configuration.Overlay2DWeatherMapEnabled) DrawWeatherMap(valueOrDefault);
		_bdl.PopClipRect();
	}


	private unsafe void RefreshMapOrigin() {
		_mapOrigin = null;
		if (!AreaMap.MapVisible) return;
		var areaMapAddon = AreaMap.AreaMapAddon;
		_globalUiScale = areaMapAddon->Scale;
		if (areaMapAddon->UldManager.NodeListCount <= 4) return;
		var ptr = (AtkComponentNode*)areaMapAddon->UldManager.NodeList[3];
		var atkResNode = ptr->AtkResNode;
		if (ptr->Component->UldManager.NodeListCount < 233) return;
		for (var i = 6; i < ptr->Component->UldManager.NodeListCount - 1; i++) {
			if (!ptr->Component->UldManager.NodeList[i]->IsVisible()) continue;
			var ptr2 = (AtkComponentNode*)ptr->Component->UldManager.NodeList[i];
			var ptr3 = (AtkImageNode*)ptr2->Component->UldManager.NodeList[4];
			string text = null;
			if (ptr3->PartsList != null && ptr3->PartId <= ptr3->PartsList->PartCount) {
				var uldAsset = ((AtkUldPart*)((byte*)ptr3->PartsList->Parts + ptr3->PartId * (nint)Unsafe.SizeOf<AtkUldPart>()))->UldAsset;
				if (uldAsset->AtkTexture.TextureType == TextureType.Resource) {
					var fileName = uldAsset->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName;
					text = Path.GetFileName(fileName.ToString());
				}
			}
			if (text is not ("060443.tex" or "060443_hr1.tex")) continue;
			var ptr4 = (AtkComponentNode*)ptr->Component->UldManager.NodeList[i];
			// Plugin.Log.Verbose($"node found {i}");
			var atkResNode2 = ptr4->AtkResNode;
			var vector = new Vector2(areaMapAddon->X, areaMapAddon->Y);
			_mapOrigin = ImGui.GetMainViewport().Pos + vector + (new Vector2(atkResNode.X, atkResNode.Y) + new Vector2(atkResNode2.X, atkResNode2.Y) + new Vector2(atkResNode2.OriginX, atkResNode2.OriginY)) * _globalUiScale;
			_mapPosSize[0] = ImGui.GetMainViewport().Pos + vector + new Vector2(atkResNode.X, atkResNode.Y) * _globalUiScale;
			_mapPosSize[1] = ImGui.GetMainViewport().Pos + vector + new Vector2(atkResNode.X, atkResNode.Y) + new Vector2(atkResNode.Width, atkResNode.Height) * _globalUiScale;
			break;
		}
	}

	private Vector2 WorldToMap(Vector2 origin, Vector3 worldVector3) =>
		origin + ToVector2(worldVector3 - Plugin.ClientState.LocalPlayer!.Position) * AreaMap.MapScale * _sizeFactorDict[Plugin.ClientState.TerritoryType] / 100f * _globalUiScale;


	private static Vector2 ToVector2(Vector3 v) => new(v.X, v.Z);

	private static Vector3 ToVector3(Vector2 v) => new(v.X, 0f, v.Y);

	private static float MapToWorld(float value, uint scale, int offset = 0) => offset * (scale / 100f) + 50f * (value - 1f) * (scale / 100f);

	private static Vector2 MapToWorld(Vector2 coordinates, Map map) {
		var vector = new Vector2(MapToWorld(coordinates.X, map.SizeFactor, map.OffsetX), MapToWorld(coordinates.Y, map.SizeFactor, map.OffsetY));
		return (vector - new Vector2(1024f, 1024f)) / map.SizeFactor * 100F;
	}

	private void NmFound() {
		Player1.Stop();
		Player1.SoundLocation = Path.Combine(_pi.AssemblyLocation.Directory!.FullName, "nm.wav");
		Player1.Load();
		Player1.Play();
	}

	private void TzFound() {
		Player2.Stop();
		Player2.SoundLocation = Path.Combine(_pi.AssemblyLocation.Directory!.FullName, "tz.wav");
		Player2.Load();
		Player2.Play();
	}

	private void DrawWeatherMap(Vector2 valueOrDefault) {
		var notice = "";
		var allNextWeatherTime = EurekaAnemos.GetAllNextWeatherTime();
		(PData.EurekaWeather, TimeSpan) anemosweatherNow = EurekaAnemos.GetCurrentWeatherInfo();
		var anemosweatherDic = new Dictionary<PData.EurekaWeather, (string, string)>();
		foreach (var o in allNextWeatherTime) {
			var timeLeft = EorzeaWeather.GetWeatherUptime(o.Weather, EurekaAnemos.Weathers, DateTime.Now).End - DateTime.Now;
			anemosweatherDic.Add(o.Weather, (o.Time.ToString(@"hh\:mm\:ss"), timeLeft.ToString(@"hh\:mm\:ss")));
		}
		notice += "风岛：";
		var etimeHour = int.Parse(_eorzeaTime.EorzeaDateTime.ToString("%H"));
		notice = anemosweatherNow.Item1 != PData.EurekaWeather.Gales ? notice + "强风×(" + anemosweatherDic[PData.EurekaWeather.Gales].Item1 + ")" : notice + "强风○(" + anemosweatherDic[PData.EurekaWeather.Gales].Item2 + ")";
		notice = etimeHour is < 6 or >= 18 ? notice + "    夜晚○(" + _eorzeaTime.TimeUntilDay().ToString(@"hh\:mm\:ss") + ")" : notice + "    夜晚×(" + _eorzeaTime.TimeUntilNight().ToString(@"hh\:mm\:ss") + ")";
		notice += "\n";
		var allNextWeatherTime2 = EurekaPagos.GetAllNextWeatherTime();
		(PData.EurekaWeather, TimeSpan) pagosweatherNow = EurekaPagos.GetCurrentWeatherInfo();
		var pagosweatherDic = new Dictionary<PData.EurekaWeather, (string, string)>();
		foreach (var o2 in allNextWeatherTime2) {
			var timeLeft2 = EorzeaWeather.GetWeatherUptime(o2.Weather, EurekaPagos.Weathers, DateTime.Now).End - DateTime.Now;
			pagosweatherDic.Add(o2.Weather, (o2.Time.ToString(@"hh\:mm\:ss"), timeLeft2.ToString(@"hh\:mm\:ss")));
		}
		notice += "冰岛：";
		notice = pagosweatherNow.Item1 != PData.EurekaWeather.Blizzards ? notice + "暴雪×(" + pagosweatherDic[PData.EurekaWeather.Blizzards].Item1 + ")" : notice + "暴雪○(" + pagosweatherDic[PData.EurekaWeather.Blizzards].Item2 + ")";
		notice = pagosweatherNow.Item1 != PData.EurekaWeather.Fog ? notice + "    薄雾×(" + pagosweatherDic[PData.EurekaWeather.Fog].Item1 + ")" : notice + "    薄雾○(" + pagosweatherDic[PData.EurekaWeather.Fog].Item2 + ")";
		notice += "\n";
		var allNextWeatherTime3 = EurekaPyros.GetAllNextWeatherTime();
		(PData.EurekaWeather, TimeSpan) pyrosweatherNow = EurekaPyros.GetCurrentWeatherInfo();
		var pyrosweatherDic = new Dictionary<PData.EurekaWeather, (string, string)>();
		foreach (var o3 in allNextWeatherTime3) {
			var timeLeft3 = EorzeaWeather.GetWeatherUptime(o3.Weather, EurekaPyros.Weathers, DateTime.Now).End - DateTime.Now;
			pyrosweatherDic.Add(o3.Weather, (o3.Time.ToString(@"hh\:mm\:ss"), timeLeft3.ToString(@"hh\:mm\:ss")));
		}
		notice += "火岛：";
		notice = pyrosweatherNow.Item1 != PData.EurekaWeather.Blizzards ? notice + "暴雪×(" + pyrosweatherDic[PData.EurekaWeather.Blizzards].Item1 + ")" : notice + "暴雪○(" + pyrosweatherDic[PData.EurekaWeather.Blizzards].Item2 + ")";
		notice = pyrosweatherNow.Item1 != PData.EurekaWeather.HeatWaves ? notice + "    热浪×(" + pyrosweatherDic[PData.EurekaWeather.HeatWaves].Item1 + ")" : notice + "    热浪○(" + pyrosweatherDic[PData.EurekaWeather.HeatWaves].Item2 + ")";
		switch (Plugin.ClientState.TerritoryType) {
			case 732: {
				ToVector3(MapToWorld(new Vector2(25.9f, 27f), Plugin.DataManager.GetExcelSheet<Map>().GetRow(732u)));
				var pos830 = WorldToMap(valueOrDefault, new Vector3(-9.1946f, 0f, 584.4f));
				_bdl.DrawText(pos830, notice, uint.MaxValue, true);
				break;
			}
			case 763: {
				var pos829 = WorldToMap(valueOrDefault, new Vector3(-9.1946f, 0f, 584.4f));
				_bdl.DrawText(pos829, notice, uint.MaxValue, true);
				break;
			}
			case 795: {
				var pos828 = WorldToMap(valueOrDefault, new Vector3(0.2181f, 0f, 865.32275f));
				_bdl.DrawText(pos828, notice, uint.MaxValue, true);
				break;
			}
			case 827: {
				var pos827 = WorldToMap(valueOrDefault, new Vector3(89.62729f, 0f, -1241.035f));
				_bdl.DrawText(pos827, notice, uint.MaxValue, true);
				break;
			}
			case 628: {
				var pos628 = WorldToMap(valueOrDefault, new Vector3(-131.185f, 0f, 251.177f));
				_bdl.DrawText(pos628, notice, uint.MaxValue, true);
				break;
			}
		}
	}
}