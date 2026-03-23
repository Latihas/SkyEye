using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SkyEye.Data;
using static System.Globalization.CultureInfo;
using static SkyEye.Data.PData;
using static SkyEye.Plugin;
using static SkyEye.Util;

namespace SkyEye;

internal class UiBuilder : IDisposable {
	private const string timeFormat = @"hh\:mm\:ss";
	private static readonly SoundPlayer Player1 = new(), Player2 = new();
	private static ushort lastTerritoryId;
	private static readonly List<(Vector3 worldpos, uint fgcolor, uint bgcolor, string name, ushort fateId, EurekaWeather SpawnRequiredWeather, bool SpawnByRequiredNight)> _eurekaList2D = [];
	private static readonly List<ushort> _eurekaLiveIdList2D = [];
	internal static readonly List<ushort> _eurekaLiveIdList2DOld = [];
	private readonly Vector2[] _mapPosSize = new Vector2[2];
	private readonly Dictionary<EurekaWeather, (string, string)> _weatherDic = new();
	private ImDrawListPtr _bdl;
	private EorzeaTime? _eorzeaTime;
	private float _globalUiScale = 1f;
	private Vector2? _mapOrigin = Vector2.Zero;
	private (EurekaWeather Weather, TimeSpan Time) _weatherNow;

	public UiBuilder() {
		ClientState.TerritoryChanged += TerritoryChanged;
		PluginInterface.UiBuilder.Draw += UiBuilder_OnBuildUi;
	}

	public void Dispose() {
		PluginInterface.UiBuilder.Draw -= UiBuilder_OnBuildUi;
		ClientState.TerritoryChanged -= TerritoryChanged;
	}

	private static void TerritoryChanged(ushort _) {
		if (InEureka(lastTerritoryId) || InEureka(ClientState.TerritoryType)) {
			YlPositions.Clear();
			Yl.Clear();
			lastFarmPos = null;
			FarmFull = false;
			foreach (var k in EurekaAnemos.DeadFateDic.Keys) EurekaAnemos.DeadFateDic[k] = "-1";
			foreach (var k in EurekaPagos.DeadFateDic.Keys) EurekaPagos.DeadFateDic[k] = "-1";
			foreach (var k in EurekaPyros.DeadFateDic.Keys) EurekaPyros.DeadFateDic[k] = "-1";
			foreach (var k in EurekaHydatos.DeadFateDic.Keys) EurekaHydatos.DeadFateDic[k] = "-1";
		}
		lastTerritoryId = ClientState.TerritoryType;
		CurrentSpeedInfo = null;
		foreach (var s in Configuration.SpeedUp.Where(s => s.Enabled && s.SpeedUpTerritory.Split('|').Contains(ClientState.TerritoryType.ToString()))) {
			CurrentSpeedInfo = s;
			break;
		}
		SetSpeed(1);
	}

	private void UiBuilder_OnBuildUi() {
		if (!Configuration.PluginEnabled || ObjectTable.LocalPlayer == null) return;
		_bdl = ImGui.GetBackgroundDrawList(ImGui.GetMainViewport());
		if (InEureka() && !Condition[ConditionFlag.BetweenAreas] && !Condition[ConditionFlag.BetweenAreas51]) {
			_eorzeaTime = EorzeaTime.ToEorzeaTime(DateTime.Now);
			RefreshEureka();
			DrawMapOverlay();
			if (Configuration.Overlay3DEnabled)
				foreach (var pos in DetectedTreasurePositions)
					if (Gui.WorldToScreen(pos, out var v))
						_bdl.DrawMapDot(v, 0xFF00FFFF, 0xFF00FFFF);
		}
		if (lastFarmPos != null)
			if (Gui.WorldToScreen(lastFarmPos.Value, out var v))
				_bdl.DrawMapDot(v, 0xFFFF0000, 0xFF00FF00, 10f);
		_eurekaList2D.Clear();
		foreach (var item in _eurekaLiveIdList2D) _eurekaLiveIdList2DOld.Add(item);
		_eurekaLiveIdList2D.Clear();
		if (!string.IsNullOrEmpty(Configuration.FindEntity)) {
			foreach (var en in ObjectTable.Where(i => i.Name.ToString().Contains(Configuration.FindEntity))) {
				if (Gui.WorldToScreen(ObjectTable.LocalPlayer.Position, out var v) && Gui.WorldToScreen(en.Position, out var v2))
					_bdl.DrawLine(v, v2, 0x7F0000FF);
			}
		}
	}

	private void RefreshEureka() {
		List<(EurekaWeather Weather, TimeSpan Time)> _weathers = null!;
		(int, EurekaWeather)[] regionWeather = null!;
		EurekaFate[] fates = null!;
		switch ((Territory)ClientState.TerritoryType) {
			case Territory.Anemos:
				foreach (var o7 in Fates) {
					_eurekaLiveIdList2D.Add(o7.FateId);
					if (!_eurekaLiveIdList2DOld.Contains(o7.FateId)) NmFound();
					EurekaAnemos.DeadFateDic[o7.FateId] = "1";
				}
				foreach (var o8 in from o8 in EurekaAnemos.DeadFateDic where o8.Value.Contains(':') let minuteSpan4 = new TimeSpan(DateTime.Now.Ticks - Convert.ToDateTime(o8.Value).Ticks) where minuteSpan4.Hours >= 2 select o8)
					EurekaAnemos.DeadFateDic[o8.Key] = "-1";
				_weathers = EurekaAnemos.GetAllNextWeatherTime();
				_weatherNow = EurekaAnemos.GetCurrentWeatherInfo();
				regionWeather = EurekaAnemos.Weathers;
				fates = EurekaAnemos.AnemosFates;
				break;
			case Territory.Pagos:
				foreach (var o3 in Fates) {
					_eurekaLiveIdList2D.Add(o3.FateId);
					if (!_eurekaLiveIdList2DOld.Contains(o3.FateId)) {
						if (o3.FateId is 1367 or 1368) TzFound(o3.FateId);
						else NmFound();
					}
					EurekaPagos.DeadFateDic[o3.FateId] = "1";
				}
				foreach (var o4 in EurekaPagos.DeadFateDic.Where(o4 => o4.Value.Contains(':'))
					         .Select(o4 => new {
						         o4,
						         minuteSpan2 = new TimeSpan(DateTime.Now.Ticks - Convert.ToDateTime(o4.Value).Ticks)
					         })
					         .Where(t => t.o4.Key is 1367 or 1368 && t.minuteSpan2.Minutes >= 7 || t.minuteSpan2.Hours >= 2)
					         .Select(t => t.o4))
					EurekaPagos.DeadFateDic[o4.Key] = "-1";
				_weathers = EurekaPagos.GetAllNextWeatherTime();
				_weatherNow = EurekaPagos.GetCurrentWeatherInfo();
				regionWeather = EurekaPagos.Weathers;
				fates = EurekaPagos.PagosFates;
				break;
			case Territory.Pyros:
				foreach (var o5 in Fates) {
					_eurekaLiveIdList2D.Add(o5.FateId);
					if (!_eurekaLiveIdList2DOld.Contains(o5.FateId)) {
						if (o5.FateId is 1407 or 1408) TzFound(o5.FateId);
						else NmFound();
					}
					EurekaPyros.DeadFateDic[o5.FateId] = "1";
				}
				foreach (var o6 in EurekaPyros.DeadFateDic.Where(o6 => o6.Value.Contains(':'))
					         .Select(o6 => new {
						         o6,
						         minuteSpan3 = new TimeSpan(DateTime.Now.Ticks - Convert.ToDateTime(o6.Value).Ticks)
					         })
					         .Where(t => t.o6.Key is 1407 or 1408 && t.minuteSpan3.Minutes >= 7 || t.minuteSpan3.Hours >= 2)
					         .Select(t => t.o6))
					EurekaPyros.DeadFateDic[o6.Key] = "-1";
				_weathers = EurekaPyros.GetAllNextWeatherTime();
				_weatherNow = EurekaPyros.GetCurrentWeatherInfo();
				regionWeather = EurekaPyros.Weathers;
				fates = EurekaPyros.PyrosFates;
				break;
			case Territory.Hydatos:
				foreach (var o in Fates) {
					_eurekaLiveIdList2D.Add(o.FateId);
					if (!_eurekaLiveIdList2DOld.Contains(o.FateId)) {
						if (o.FateId == 1425) TzFound(o.FateId);
						else NmFound();
					}
					EurekaHydatos.DeadFateDic[o.FateId] = "1";
				}
				foreach (var o2 in EurekaHydatos.DeadFateDic.Where(o2 => o2.Value.Contains(':'))
					         .Select(o2 => new {
						         o2,
						         minuteSpan = new TimeSpan(DateTime.Now.Ticks - Convert.ToDateTime(o2.Value).Ticks)
					         })
					         .Where(t => t.o2.Key == 1425 && t.minuteSpan.Minutes >= 7 || t.minuteSpan.Hours >= 2)
					         .Select(t => t.o2))
					EurekaHydatos.DeadFateDic[o2.Key] = "-1";
				_weathers = EurekaHydatos.GetAllNextWeatherTime();
				_weatherNow = EurekaHydatos.GetCurrentWeatherInfo();
				regionWeather = EurekaHydatos.Weathers;
				fates = EurekaHydatos.HydatosFates;
				break;
		}
		_eurekaLiveIdList2DOld.Clear();
		_weatherDic.Clear();
		foreach (var o9 in _weathers) {
			var timeLeft = EorzeaWeather.GetWeatherUptime(o9.Weather, regionWeather, DateTime.Now).End - DateTime.Now;
			_weatherDic.Add(o9.Weather, (o9.Time.ToString(timeFormat), timeLeft.ToString(timeFormat)));
		}
		foreach (var o10 in fates)
			_eurekaList2D.Add((Pos2Map(o10.FatePosition),
				uint.MaxValue, uint.MaxValue, o10.BossShortName, o10.FateId, o10.SpawnRequiredWeather, o10.SpawnByRequiredNight));
	}


	private unsafe void DrawMapOverlay() {
		RefreshMapOrigin();
		if (_mapOrigin == null) return;
		var valueOrDefault = _mapOrigin.GetValueOrDefault();
		if (valueOrDefault == Vector2.Zero || ClientState.TerritoryType == 0) return;
		_bdl.PushClipRect(_mapPosSize[0], _mapPosSize[1]);
		var territory = (Territory)ClientState.TerritoryType;
		foreach (var item2 in _eurekaList2D) {
			var pos2 = WorldToMap(valueOrDefault, item2.worldpos);
			var fateid = item2.fateId;
			if (_eurekaLiveIdList2D.Contains(fateid)) {
				var fateProgress = FateManager.Instance()->GetFateById(fateid)->Progress;
				if (fateProgress > 0) _bdl.DrawText(pos2, item2.name + "(" + fateProgress + "%)", 0xFF00E000);
				else _bdl.DrawText(pos2, item2.name, 0xFF00E000);
				if (fateProgress > 97) {
					var time = DateTime.Now.ToString(CurrentCulture).Replace("/", "-");
					switch (territory) {
						case Territory.Anemos:
							EurekaAnemos.DeadFateDic[fateid] = time;
							break;
						case Territory.Pagos:
							EurekaPagos.DeadFateDic[fateid] = time;
							break;
						case Territory.Pyros:
							EurekaPyros.DeadFateDic[fateid] = time;
							break;
						case Territory.Hydatos:
							EurekaHydatos.DeadFateDic[fateid] = time;
							break;
					}
				}
			} else if (territory == Territory.Anemos && EurekaAnemos.DeadFateDic[fateid].Contains(':') ||
			           territory == Territory.Pagos && EurekaPagos.DeadFateDic[fateid].Contains(':') ||
			           territory == Territory.Pyros && EurekaPyros.DeadFateDic[fateid].Contains(':') ||
			           territory == Territory.Hydatos && EurekaHydatos.DeadFateDic[fateid].Contains(':')) {
				var timeFromCanTriggered = territory switch {
					Territory.Anemos => new TimeSpan(2, 0, 0) - (DateTime.Now - Convert.ToDateTime(EurekaAnemos.DeadFateDic[fateid])),
					Territory.Pagos => fateid is not (1367 or 1368)
						? new TimeSpan(2, 0, 0) - (DateTime.Now - Convert.ToDateTime(EurekaPagos.DeadFateDic[fateid]))
						: new TimeSpan(0, 7, 0) - (DateTime.Now - Convert.ToDateTime(EurekaPagos.DeadFateDic[fateid])),
					Territory.Pyros => fateid is not (1407 or 1408)
						? new TimeSpan(2, 0, 0) - (DateTime.Now - Convert.ToDateTime(EurekaPyros.DeadFateDic[fateid]))
						: new TimeSpan(0, 7, 0) - (DateTime.Now - Convert.ToDateTime(EurekaPyros.DeadFateDic[fateid])),
					Territory.Hydatos => fateid != 1425
						? fateid is not (1422 or 1424)
							? new TimeSpan(2, 0, 0) - (DateTime.Now - Convert.ToDateTime(EurekaHydatos.DeadFateDic[fateid]))
							: new TimeSpan(0, 20, 0) - (DateTime.Now - Convert.ToDateTime(EurekaHydatos.DeadFateDic[fateid]))
						: new TimeSpan(0, 7, 0) - (DateTime.Now - Convert.ToDateTime(EurekaHydatos.DeadFateDic[fateid]))
				};
				if (item2 is { SpawnRequiredWeather: EurekaWeather.None, SpawnByRequiredNight: false })
					_bdl.DrawText(pos2, item2.name + (Configuration.Overlay2DDetailEnabled ? "\n" + timeFromCanTriggered.ToString(timeFormat) : ""), Configuration.Overlay2DDetailEnabled ? 0xFF808080 : item2.fgcolor);
				else if (item2.SpawnRequiredWeather == EurekaWeather.None) {
					if (_eorzeaTime == null) continue;
					var etimeHour = int.Parse(_eorzeaTime.EorzeaDateTime.ToString("%H"));
					if (etimeHour is < 6 or >= 18) {
						_bdl.DrawText(pos2, item2.name + (Configuration.Overlay2DDetailEnabled ? "\n" + timeFromCanTriggered.ToString(timeFormat) : ""), Configuration.Overlay2DDetailEnabled ? 0xFF808080 : item2.fgcolor);
						continue;
					}
					var timeFromNight = _eorzeaTime.TimeUntilNight();
					if (timeFromNight < timeFromCanTriggered) _bdl.DrawText(pos2, item2.name + (Configuration.Overlay2DDetailEnabled ? "\n" + timeFromCanTriggered.ToString(timeFormat) : ""), Configuration.Overlay2DDetailEnabled ? 0xFF808080 : item2.fgcolor);
					else _bdl.DrawText(pos2, item2.name + "\n" + timeFromNight.ToString(timeFormat), Configuration.Overlay2DDetailEnabled ? 0xFF808080 : item2.fgcolor);
				} else if (!_weatherDic.TryGetValue(item2.SpawnRequiredWeather, out var value) || _weatherNow.Weather == item2.SpawnRequiredWeather || TimeSpan.Parse(value.Item1) < timeFromCanTriggered)
					_bdl.DrawText(pos2, item2.name + (Configuration.Overlay2DDetailEnabled ? "\n" + timeFromCanTriggered.ToString(timeFormat) : ""), Configuration.Overlay2DDetailEnabled ? 0xFF808080 : item2.fgcolor);
				else _bdl.DrawText(pos2, item2.name + (Configuration.Overlay2DDetailEnabled ? "\n" + value.Item1 : ""), Configuration.Overlay2DDetailEnabled ? 0xFF808080 : item2.fgcolor);
			} else
				switch (item2) {
					case { SpawnRequiredWeather: EurekaWeather.None, SpawnByRequiredNight: false }:
						_bdl.DrawText(pos2, item2.name, item2.fgcolor);
						break;
					case { SpawnRequiredWeather: EurekaWeather.None, SpawnByRequiredNight: true } when _eorzeaTime == null:
						continue;
					case { SpawnRequiredWeather: EurekaWeather.None, SpawnByRequiredNight: true }: {
						var etimeHour2 = int.Parse(_eorzeaTime.EorzeaDateTime.ToString("%H"));
						if (etimeHour2 is < 6 or >= 18) {
							var timeFromDay = _eorzeaTime.TimeUntilDay();
							_bdl.DrawText(pos2, item2.name + (Configuration.Overlay2DDetailEnabled ? "\n" + timeFromDay.ToString(timeFormat) : ""), item2.fgcolor);
						} else {
							var timeFromNight2 = _eorzeaTime.TimeUntilNight();
							_bdl.DrawText(pos2, item2.name + (Configuration.Overlay2DDetailEnabled ? "\n" + timeFromNight2.ToString(timeFormat) : ""), Configuration.Overlay2DDetailEnabled ? 0xFF808080 : item2.fgcolor);
						}
						break;
					}
					default: {
						if (item2.SpawnRequiredWeather != EurekaWeather.None && !item2.SpawnByRequiredNight) {
							if (!_weatherDic.TryGetValue(item2.SpawnRequiredWeather, out var value) || _weatherNow.Weather == item2.SpawnRequiredWeather)
								_bdl.DrawText(pos2, item2.name + (Configuration.Overlay2DDetailEnabled ? "\n" + _weatherDic[item2.SpawnRequiredWeather].Item2 : ""), item2.fgcolor);
							else _bdl.DrawText(pos2, item2.name + (Configuration.Overlay2DDetailEnabled ? "\n" + value.Item1 : ""), Configuration.Overlay2DDetailEnabled ? 0xFF808080 : item2.fgcolor);
						} else {
							if (item2.SpawnRequiredWeather == EurekaWeather.None || !item2.SpawnByRequiredNight || _eorzeaTime == null) continue;
							var etimeHour3 = int.Parse(_eorzeaTime.EorzeaDateTime.ToString("%H"));
							var weatherLeftTime = _weatherDic[item2.SpawnRequiredWeather].Item2;
							if ((!_weatherDic.ContainsKey(item2.SpawnRequiredWeather) || _weatherNow.Weather == item2.SpawnRequiredWeather) && etimeHour3 is < 6 or >= 18) {
								var timeFromDay2 = _eorzeaTime.TimeUntilDay();
								if (timeFromDay2.Ticks < TimeSpan.Parse(weatherLeftTime).Ticks) _bdl.DrawText(pos2, item2.name + (Configuration.Overlay2DDetailEnabled ? "\n" + timeFromDay2.ToString(timeFormat) : ""), item2.fgcolor);
								else _bdl.DrawText(pos2, item2.name + "\n" + weatherLeftTime, item2.fgcolor);
							} else if (etimeHour3 is >= 6 and < 18) {
								var timeFromNight3 = _eorzeaTime.TimeUntilNight();
								_bdl.DrawText(pos2, item2.name + (Configuration.Overlay2DDetailEnabled ? "\n" + timeFromNight3.ToString(timeFormat) : ""), Configuration.Overlay2DDetailEnabled ? 0xFF808080 : item2.fgcolor);
							} else _bdl.DrawText(pos2, item2.name + (Configuration.Overlay2DDetailEnabled ? "\n" + _weatherDic[item2.SpawnRequiredWeather].Item1 : ""), Configuration.Overlay2DDetailEnabled ? 0xFF808080 : item2.fgcolor);
						}
						break;
					}
				}
		}
		if (Configuration.Overlay2DWeatherMapEnabled) DrawWeatherMap(valueOrDefault);
		foreach (var yl in YlPositions) _bdl.DrawText(WorldToMap(valueOrDefault, yl), "元灵", 0xFF0000FF);
		if (Configuration.ShowCurrentYl) {
			foreach (var p in ElementalPositions[territory])
				_bdl.DrawMapDot(WorldToMap(valueOrDefault, p), 0x7FFFFF00, 0x7F000000, 15);
		}
		_bdl.PopClipRect();
	}


	private unsafe void RefreshMapOrigin() {
		_mapOrigin = null;
		if (!AreaMap.MapVisible) return;
		var areaMapAddon = AreaMap.AreaMapAddon;
		_globalUiScale = areaMapAddon->Scale;
		var areaMapAddonUldManager = areaMapAddon->UldManager;
		if (areaMapAddonUldManager.NodeListCount <= 4) return;
		var ptr = (AtkComponentNode*)areaMapAddonUldManager.NodeList[3];
		var atkResNode = ptr->AtkResNode;
		var ptrUldManager = ptr->Component->UldManager;
		if (ptrUldManager.NodeListCount < 233) return;
		var ptrUldManagerNodeList = ptrUldManager.NodeList;
		for (var i = 6; i < ptrUldManager.NodeListCount - 1; i++) {
			var item = ptrUldManagerNodeList[i];
			if (!item->IsVisible()) continue;
			var itemx = (AtkComponentNode*)item;
			var ptr3 = (AtkImageNode*)itemx->Component->UldManager.NodeList[4];
			var atkResNode2 = itemx->AtkResNode;
			string? text = null;
			var ptr3PartsList = ptr3->PartsList;
			if (ptr3PartsList != null && ptr3->PartId <= ptr3PartsList->PartCount) {
				var AtkTexture = (ptr3PartsList->Parts + ptr3->PartId * Unsafe.SizeOf<AtkUldPart>())->UldAsset->AtkTexture;
				if (AtkTexture.TextureType == TextureType.Resource)
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

	private Vector2 WorldToMap(Vector2 origin, Vector3 worldVector3) =>
		origin + ToVector2(worldVector3 - ObjectTable.LocalPlayer!.Position) * AreaMap.MapScale * _globalUiScale;


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

	private void DrawWeatherMap(Vector2 valueOrDefault) {
		if (_eorzeaTime == null) return;
		var notice = new StringBuilder();
		var etimeHour = int.Parse(_eorzeaTime.EorzeaDateTime.ToString("%H"));
		notice.Append("风岛：强风");
		var o = EurekaAnemos.GetAllNextWeatherTime().First(i => i.Weather == EurekaWeather.Gales);
		if (EurekaAnemos.GetCurrentWeatherInfo().Weather != EurekaWeather.Gales)
			notice.Append("×(").Append(o.Time.ToString(timeFormat));
		else
			notice.Append("○(").Append((EorzeaWeather.GetWeatherUptime(o.Weather, EurekaAnemos.Weathers, DateTime.Now).End - DateTime.Now).ToString(timeFormat));
		notice.Append(")    夜晚");
		if (etimeHour is < 6 or >= 18)
			notice.Append("○(").Append(_eorzeaTime.TimeUntilDay().ToString(timeFormat));
		else
			notice.Append("×(").Append(_eorzeaTime.TimeUntilNight().ToString(timeFormat));
		var pagosweatherNow = EurekaPagos.GetCurrentWeatherInfo();
		notice.Append(")\n冰岛：暴雪");
		var next = EurekaPagos.GetAllNextWeatherTime();
		o = next.First(i => i.Weather == EurekaWeather.Blizzards);
		if (pagosweatherNow.Weather != EurekaWeather.Blizzards)
			notice.Append("×(").Append(o.Time.ToString(timeFormat));
		else
			notice.Append("○(").Append((EorzeaWeather.GetWeatherUptime(o.Weather, EurekaPagos.Weathers, DateTime.Now).End - DateTime.Now).ToString(timeFormat));
		notice.Append(")    薄雾");
		o = next.First(i => i.Weather == EurekaWeather.Fog);
		if (pagosweatherNow.Weather != EurekaWeather.Fog)
			notice.Append("×(").Append(o.Time.ToString(timeFormat));
		else
			notice.Append("○(").Append((EorzeaWeather.GetWeatherUptime(o.Weather, EurekaPagos.Weathers, DateTime.Now).End - DateTime.Now).ToString(timeFormat));
		notice.Append(")\n火岛：暴雪");
		next = EurekaPyros.GetAllNextWeatherTime();
		o = next.First(i => i.Weather == EurekaWeather.Blizzards);
		if (pagosweatherNow.Weather != EurekaWeather.Blizzards)
			notice.Append("×(").Append(o.Time.ToString(timeFormat));
		else
			notice.Append("○(").Append((EorzeaWeather.GetWeatherUptime(o.Weather, EurekaPyros.Weathers, DateTime.Now).End - DateTime.Now).ToString(timeFormat));
		notice.Append(")    热浪");
		o = next.First(i => i.Weather == EurekaWeather.HeatWaves);
		if (pagosweatherNow.Weather != EurekaWeather.Blizzards)
			notice.Append("×(").Append(o.Time.ToString(timeFormat));
		else
			notice.Append("○(").Append((EorzeaWeather.GetWeatherUptime(o.Weather, EurekaPyros.Weathers, DateTime.Now).End - DateTime.Now).ToString(timeFormat));
		notice.Append(')');
		_bdl.DrawText(WorldToMap(valueOrDefault, (Territory)ClientState.TerritoryType switch {
			Territory.Anemos or Territory.Pagos => new Vector3(-9.1946f, 0f, 584.4f),
			Territory.Pyros => new Vector3(0.2181f, 0f, 865.32275f),
			Territory.Hydatos => new Vector3(89.62729f, 0f, -1241.035f)
		}), notice.ToString(), uint.MaxValue);
	}
}