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
using SkyEye.SkyEye.Data;
using static System.Globalization.CultureInfo;
using static SkyEye.SkyEye.Data.PData;
using static SkyEye.SkyEye.Plugin;
using static SkyEye.SkyEye.Util;

namespace SkyEye.SkyEye;

internal class UiBuilder : IDisposable {
    private const string timeFormat = @"hh\:mm\:ss";
    private static readonly SoundPlayer Player1 = new(), Player2 = new();
    private static ushort lastTerritoryId;
    private readonly List<(Vector3 worldpos, uint fgcolor, uint bgcolor, string name, string fateId, EurekaWeather SpawnRequiredWeather, bool SpawnByRequiredNight)> _eurekaList2D = [];
    private readonly List<string> _eurekaLiveIdList2D = [], _eurekaLiveIdList2DOld = [];
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

    private static void TerritoryChanged(ushort territoryId) {
        if (InEureka(lastTerritoryId) || InEureka(territoryId)) {
            YlPositions.Clear();
            Yl.Clear();
            lastFarmPos = null;
            FarmFull = false;
            foreach (var k in EurekaAnemos.DeadFateDic.Keys) EurekaAnemos.DeadFateDic[k] = "-1";
            foreach (var k in EurekaPagos.DeadFateDic.Keys) EurekaPagos.DeadFateDic[k] = "-1";
            foreach (var k in EurekaPyros.DeadFateDic.Keys) EurekaPyros.DeadFateDic[k] = "-1";
            foreach (var k in EurekaHydatos.DeadFateDic.Keys) EurekaHydatos.DeadFateDic[k] = "-1";
        }
        lastTerritoryId = territoryId;
        CurrentSpeedInfo = null;
        foreach (var s in Configuration.SpeedUp.Where(s => s.Enabled && s.SpeedUpTerritory.Split('|').Contains(territoryId.ToString()))) {
            CurrentSpeedInfo = s;
            break;
        }
    }

    private void UiBuilder_OnBuildUi() {
        if (!Configuration.PluginEnabled) return;
        if (InEureka() && !Condition[ConditionFlag.BetweenAreas] && !Condition[ConditionFlag.BetweenAreas51]) {
            _eorzeaTime = EorzeaTime.ToEorzeaTime(DateTime.Now);
            _bdl = ImGui.GetBackgroundDrawList(ImGui.GetMainViewport());
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
    }

    private void RefreshEureka() {
        var TerritoryType = ClientState.TerritoryType;
        List<(EurekaWeather Weather, TimeSpan Time)> _weathers = null!;
        (int, EurekaWeather)[] regionWeather = null!;
        EurekaFate[] fates = null!;
        switch (TerritoryType) {
            case 732:
                foreach (var o7 in Fates) {
                    _eurekaLiveIdList2D.Add(o7.FateId.ToString());
                    if (!_eurekaLiveIdList2DOld.Contains(o7.FateId.ToString())) NmFound();
                    EurekaAnemos.DeadFateDic[o7.FateId] = "1";
                }
                foreach (var o8 in from o8 in EurekaAnemos.DeadFateDic where o8.Value.Contains(':') let minuteSpan4 = new TimeSpan(DateTime.Now.Ticks - Convert.ToDateTime(o8.Value).Ticks) where minuteSpan4.Hours >= 2 select o8)
                    EurekaAnemos.DeadFateDic[o8.Key] = "-1";
                _weathers = EurekaAnemos.GetAllNextWeatherTime();
                _weatherNow = EurekaAnemos.GetCurrentWeatherInfo();
                regionWeather = EurekaAnemos.Weathers;
                fates = EurekaAnemos.AnemosFates;
                break;
            case 763:
                foreach (var o3 in Fates) {
                    _eurekaLiveIdList2D.Add(o3.FateId.ToString());
                    if (!_eurekaLiveIdList2DOld.Contains(o3.FateId.ToString())) {
                        if (o3.FateId is 1367 or 1368) TzFound();
                        else NmFound();
                    }
                    EurekaPagos.DeadFateDic[o3.FateId] = "1";
                }
                foreach (var o4 in from o4 in EurekaPagos.DeadFateDic
                         where o4.Value.Contains(':')
                         let minuteSpan2 = new TimeSpan(DateTime.Now.Ticks - Convert.ToDateTime(o4.Value).Ticks)
                         where o4.Key is 1367 or 1368 && minuteSpan2.Minutes >= 7 || minuteSpan2.Hours >= 2
                         select o4)
                    EurekaPagos.DeadFateDic[o4.Key] = "-1";
                _weathers = EurekaPagos.GetAllNextWeatherTime();
                _weatherNow = EurekaPagos.GetCurrentWeatherInfo();
                regionWeather = EurekaPagos.Weathers;
                fates = EurekaPagos.PagosFates;
                break;
            case 795:
                foreach (var o5 in Fates) {
                    _eurekaLiveIdList2D.Add(o5.FateId.ToString());
                    if (!_eurekaLiveIdList2DOld.Contains(o5.FateId.ToString())) {
                        if (o5.FateId is 1407 or 1408) TzFound();
                        else NmFound();
                    }
                    EurekaPyros.DeadFateDic[o5.FateId] = "1";
                }
                foreach (var o6 in from o6 in EurekaPyros.DeadFateDic
                         where o6.Value.Contains(':')
                         let minuteSpan3 = new TimeSpan(DateTime.Now.Ticks - Convert.ToDateTime(o6.Value).Ticks)
                         where o6.Key is 1407 or 1408 && minuteSpan3.Minutes >= 7 || minuteSpan3.Hours >= 2
                         select o6)
                    EurekaPyros.DeadFateDic[o6.Key] = "-1";
                _weathers = EurekaPyros.GetAllNextWeatherTime();
                _weatherNow = EurekaPyros.GetCurrentWeatherInfo();
                regionWeather = EurekaPyros.Weathers;
                fates = EurekaPyros.PyrosFates;
                break;
            case 827:
                foreach (var o in Fates) {
                    _eurekaLiveIdList2D.Add(o.FateId.ToString());
                    if (!_eurekaLiveIdList2DOld.Contains(o.FateId.ToString())) {
                        if (o.FateId == 1425) TzFound();
                        else NmFound();
                    }
                    EurekaHydatos.DeadFateDic[o.FateId] = "1";
                }
                foreach (var o2 in from o2 in EurekaHydatos.DeadFateDic
                         where o2.Value.Contains(':')
                         let minuteSpan = new TimeSpan(DateTime.Now.Ticks - Convert.ToDateTime(o2.Value).Ticks)
                         where o2.Key == 1425 && minuteSpan.Minutes >= 7 || minuteSpan.Hours >= 2
                         select o2)
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
            _eurekaList2D.Add((ToVector3(MapToWorld(o10.FatePosition, 200, 11, 11.25f)),
                uint.MaxValue, uint.MaxValue, o10.BossShortName, o10.FateId.ToString(), o10.SpawnRequiredWeather, o10.SpawnByRequiredNight));
    }


    private unsafe void DrawMapOverlay() {
        RefreshMapOrigin();
        if (_mapOrigin == null) return;
        var valueOrDefault = _mapOrigin.GetValueOrDefault();
        if (valueOrDefault == Vector2.Zero || ClientState.TerritoryType == 0) return;
        _bdl.PushClipRect(_mapPosSize[0], _mapPosSize[1]);
        foreach (var item2 in _eurekaList2D) {
            var pos2 = WorldToMap(valueOrDefault, item2.worldpos);
            if (_eurekaLiveIdList2D.Contains(item2.fateId)) {
                var fateProgress = FateManager.Instance()->GetFateById(ushort.Parse(item2.fateId))->Progress;
                if (fateProgress > 0) _bdl.DrawText(pos2, item2.name + "(" + fateProgress + "%)", 0xFF00E000);
                else _bdl.DrawText(pos2, item2.name, 0xFF00E000);
                if (fateProgress > 97) {
                    var time = DateTime.Now.ToString(CurrentCulture).Replace("/", "-");
                    var fateId = ushort.Parse(item2.fateId);
                    switch (ClientState.TerritoryType) {
                        case 732:
                            EurekaAnemos.DeadFateDic[fateId] = time;
                            break;
                        case 763:
                            EurekaPagos.DeadFateDic[fateId] = time;
                            break;
                        case 795:
                            EurekaPyros.DeadFateDic[fateId] = time;
                            break;
                        case 827:
                            EurekaHydatos.DeadFateDic[fateId] = time;
                            break;
                    }
                }
            }
            else if (ClientState.TerritoryType == 732 && EurekaAnemos.DeadFateDic[ushort.Parse(item2.fateId)].Contains(':') || ClientState.TerritoryType == 763 && EurekaPagos.DeadFateDic[ushort.Parse(item2.fateId)].Contains(':') ||
                     ClientState.TerritoryType == 795 && EurekaPyros.DeadFateDic[ushort.Parse(item2.fateId)].Contains(':') || ClientState.TerritoryType == 827 && EurekaHydatos.DeadFateDic[ushort.Parse(item2.fateId)].Contains(':')) {
                var timeFromCanTriggered = ClientState.TerritoryType switch {
                    732 => new TimeSpan(2, 0, 0) - (DateTime.Now - Convert.ToDateTime(EurekaAnemos.DeadFateDic[ushort.Parse(item2.fateId)])),
                    763 => item2.fateId is not ("1367" or "1368")
                        ? new TimeSpan(2, 0, 0) - (DateTime.Now - Convert.ToDateTime(EurekaPagos.DeadFateDic[ushort.Parse(item2.fateId)]))
                        : new TimeSpan(0, 7, 0) - (DateTime.Now - Convert.ToDateTime(EurekaPagos.DeadFateDic[ushort.Parse(item2.fateId)])),
                    795 => item2.fateId is not ("1407" or "1408")
                        ? new TimeSpan(2, 0, 0) - (DateTime.Now - Convert.ToDateTime(EurekaPyros.DeadFateDic[ushort.Parse(item2.fateId)]))
                        : new TimeSpan(0, 7, 0) - (DateTime.Now - Convert.ToDateTime(EurekaPyros.DeadFateDic[ushort.Parse(item2.fateId)])),
                    827 => "1425" != item2.fateId
                        ? item2.fateId is not ("1422" or "1424")
                            ? new TimeSpan(2, 0, 0) - (DateTime.Now - Convert.ToDateTime(EurekaHydatos.DeadFateDic[ushort.Parse(item2.fateId)]))
                            : new TimeSpan(0, 20, 0) - (DateTime.Now - Convert.ToDateTime(EurekaHydatos.DeadFateDic[ushort.Parse(item2.fateId)]))
                        : new TimeSpan(0, 7, 0) - (DateTime.Now - Convert.ToDateTime(EurekaHydatos.DeadFateDic[ushort.Parse(item2.fateId)])),
                    _ => TimeSpan.Zero
                };
                if (item2 is { SpawnRequiredWeather: EurekaWeather.None, SpawnByRequiredNight: false }) _bdl.DrawText(pos2, item2.name + "\n" + timeFromCanTriggered.ToString(timeFormat), 0xFF808080);
                else if (item2.SpawnRequiredWeather == EurekaWeather.None) {
                    if (_eorzeaTime == null) continue;
                    var etimeHour = int.Parse(_eorzeaTime.EorzeaDateTime.ToString("%H"));
                    if (etimeHour is < 6 or >= 18) {
                        _bdl.DrawText(pos2, item2.name + "\n" + timeFromCanTriggered.ToString(timeFormat), 0xFF808080);
                        continue;
                    }
                    var timeFromNight = _eorzeaTime.TimeUntilNight();
                    if (timeFromNight < timeFromCanTriggered) _bdl.DrawText(pos2, item2.name + "\n" + timeFromCanTriggered.ToString(timeFormat), 0xFF808080);
                    else _bdl.DrawText(pos2, item2.name + "\n" + timeFromNight.ToString(timeFormat), 0xFF808080);
                }
                else if (!_weatherDic.TryGetValue(item2.SpawnRequiredWeather, out var value) || _weatherNow.Weather == item2.SpawnRequiredWeather || TimeSpan.Parse(value.Item1) < timeFromCanTriggered)
                    _bdl.DrawText(pos2, item2.name + "\n" + timeFromCanTriggered.ToString(timeFormat), 0xFF808080);
                else _bdl.DrawText(pos2, item2.name + "\n" + value.Item1, 0xFF808080);
            }
            else
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
                            _bdl.DrawText(pos2, item2.name + "\n" + timeFromDay.ToString(timeFormat), item2.fgcolor);
                        }
                        else {
                            var timeFromNight2 = _eorzeaTime.TimeUntilNight();
                            _bdl.DrawText(pos2, item2.name + "\n" + timeFromNight2.ToString(timeFormat), 0xFF808080);
                        }
                        break;
                    }
                    default: {
                        if (item2.SpawnRequiredWeather != EurekaWeather.None && !item2.SpawnByRequiredNight) {
                            if (!_weatherDic.TryGetValue(item2.SpawnRequiredWeather, out var value) || _weatherNow.Weather == item2.SpawnRequiredWeather) _bdl.DrawText(pos2, item2.name + "\n" + _weatherDic[item2.SpawnRequiredWeather].Item2, item2.fgcolor);
                            else _bdl.DrawText(pos2, item2.name + "\n" + value.Item1, 0xFF808080);
                        }
                        else {
                            if (item2.SpawnRequiredWeather == EurekaWeather.None || !item2.SpawnByRequiredNight || _eorzeaTime == null) continue;
                            var etimeHour3 = int.Parse(_eorzeaTime.EorzeaDateTime.ToString("%H"));
                            var weatherLeftTime = _weatherDic[item2.SpawnRequiredWeather].Item2;
                            if ((!_weatherDic.ContainsKey(item2.SpawnRequiredWeather) || _weatherNow.Weather == item2.SpawnRequiredWeather) && etimeHour3 is < 6 or >= 18) {
                                var timeFromDay2 = _eorzeaTime.TimeUntilDay();
                                if (timeFromDay2.Ticks < TimeSpan.Parse(weatherLeftTime).Ticks) _bdl.DrawText(pos2, item2.name + "\n" + timeFromDay2.ToString(timeFormat), item2.fgcolor);
                                else _bdl.DrawText(pos2, item2.name + "\n" + weatherLeftTime, item2.fgcolor);
                            }
                            else if (etimeHour3 is >= 6 and < 18) {
                                var timeFromNight3 = _eorzeaTime.TimeUntilNight();
                                _bdl.DrawText(pos2, item2.name + "\n" + timeFromNight3.ToString(timeFormat), 0xFF808080);
                            }
                            else _bdl.DrawText(pos2, item2.name + "\n" + _weatherDic[item2.SpawnRequiredWeather].Item1, 0xFF808080);
                        }
                        break;
                    }
                }
        }
        if (Configuration.Overlay2DWeatherMapEnabled) DrawWeatherMap(valueOrDefault);
        foreach (var yl in YlPositions) _bdl.DrawText(WorldToMap(valueOrDefault, yl), "元灵", 0xFF0000FF);
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
                var AtkTexture = ((AtkUldPart*)((byte*)ptr3PartsList->Parts + ptr3->PartId * (nint)Unsafe.SizeOf<AtkUldPart>()))->UldAsset->AtkTexture;
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
        origin + ToVector2(worldVector3 - ClientState.LocalPlayer!.Position) * AreaMap.MapScale * _globalUiScale;


    internal static void NmFound() {
        Player1.Stop();
        Player1.SoundLocation = Path.Combine(PluginInterface.AssemblyLocation.Directory!.FullName, "nm.wav");
        Player1.Load();
        Player1.Play();
    }

    private static void TzFound() {
        Player2.Stop();
        Player2.SoundLocation = Path.Combine(PluginInterface.AssemblyLocation.Directory!.FullName, "tz.wav");
        Player2.Load();
        Player2.Play();
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
        var ns = notice.ToString();
        _bdl.DrawText(WorldToMap(valueOrDefault, ClientState.TerritoryType switch {
            732 or 763 => new Vector3(-9.1946f, 0f, 584.4f),
            795 => new Vector3(0.2181f, 0f, 865.32275f),
            827 => new Vector3(89.62729f, 0f, -1241.035f),
            _ => throw new Exception()
        }), ns, uint.MaxValue);
    }
}