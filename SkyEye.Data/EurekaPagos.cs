using System;
using System.Collections.Generic;
using System.Numerics;

namespace SkyEye.SkyEye.Data;

internal static class EurekaPagos {
    public static readonly (int, PData.EurekaWeather)[] Weathers = [
        (10, PData.EurekaWeather.FairSkies),
        (28, PData.EurekaWeather.Fog),
        (46, PData.EurekaWeather.HeatWaves),
        (64, PData.EurekaWeather.Snow),
        (82, PData.EurekaWeather.Thunder),
        (100, PData.EurekaWeather.Blizzards)
    ];

    public static readonly Dictionary<int, string> DeadFateDic = new() {
        {
            1351, "-1"
        }, {
            1369, "-1"
        }, {
            1353, "-1"
        }, {
            1354, "-1"
        }, {
            1355, "-1"
        }, {
            1366, "-1"
        }, {
            1357, "-1"
        }, {
            1356, "-1"
        }, {
            1352, "-1"
        }, {
            1360, "-1"
        }, {
            1358, "-1"
        }, {
            1361, "-1"
        }, {
            1362, "-1"
        }, {
            1359, "-1"
        }, {
            1363, "-1"
        }, {
            1365, "-1"
        }, {
            1364, "-1"
        }, {
            1367, "-1"
        }, {
            1368, "-1"
        }
    };


    public static readonly List<EurekaFate> PagosFates = [
        new(1351, "周冬雨", new Vector2(-0.1946f, 234.4f), PData.EurekaWeather.None, false),
        new(1369, "读书人", new Vector2(194.67f, 340.845f), PData.EurekaWeather.None, true),
        new(1353, "灰烬龙", new Vector2(428.54f, 425.388f), PData.EurekaWeather.None, false),
        new(1354, "魔虫", new Vector2(559.886f, 320.959f), PData.EurekaWeather.None, false),
        new(1355, "安娜波", new Vector2(575.078f, 10.7835f), PData.EurekaWeather.Fog, false),
        new(1366, "白泽", new Vector2(378.451f, 58.118f), PData.EurekaWeather.None, false),
        new(1357, "雪屋王", new Vector2(-222.164f, -269.87f), PData.EurekaWeather.None, false),
        new(1356, "阿萨格", new Vector2(-548.7f, -520.554f), PData.EurekaWeather.None, false),
        new(1352, "山羊", new Vector2(-579.55f, -132.637f), PData.EurekaWeather.None, false),
        new(1360, "螃蟹", new Vector2(-638.802f, -299.99f), PData.EurekaWeather.Fog, false),
        new(1358, "双牛", new Vector2(-380.98f, -131.9f), PData.EurekaWeather.None, false),
        new(1361, "圣牛", new Vector2(252.05f, -227.33f), PData.EurekaWeather.None, false),
        new(1362, "贝爷", new Vector2(467.758f, -150.738f), PData.EurekaWeather.Thunder, false),
        new(1359, "荷鲁斯", new Vector2(217.191f, -58.8899f), PData.EurekaWeather.HeatWaves, false),
        new(1363, "大眼", new Vector2(118.55f, 178.48f), PData.EurekaWeather.None, false),
        new(1365, "凯西", new Vector2(37.76f, -361.25f), PData.EurekaWeather.Blizzards, false),
        new(1364, "娄希", new Vector2(727.415f, -126.398f), PData.EurekaWeather.None, true),
        new(1367, "小兔子", new Vector2(-163.29f, 300.203f), PData.EurekaWeather.None, false),
        new(1368, "大兔子", new Vector2(-46.6f, -9.67f), PData.EurekaWeather.None, false)
    ];


    public static (PData.EurekaWeather Weather, TimeSpan Timeleft) GetCurrentWeatherInfo() => EorzeaWeather.GetCurrentWeatherInfo(Weathers);


    public static List<(PData.EurekaWeather Weather, TimeSpan Time)> GetAllNextWeatherTime() => EorzeaWeather.GetAllWeathers(Weathers);
}