using System;
using System.Collections.Generic;
using System.Numerics;
using static SkyEye.SkyEye.Data.PData.EurekaWeather;

namespace SkyEye.SkyEye.Data;

internal static class EurekaAnemos {
    public static readonly (int, PData.EurekaWeather)[] Weathers = [
        (30, FairSkies),
        (60, Gales),
        (90, Showers),
        (100, Snow)
    ];

    public static readonly Dictionary<int, string> DeadFateDic = new() {
        {
            1332, "-1"
        }, {
            1348, "-1"
        }, {
            1333, "-1"
        }, {
            1328, "-1"
        }, {
            1344, "-1"
        }, {
            1347, "-1"
        }, {
            1345, "-1"
        }, {
            1334, "-1"
        }, {
            1335, "-1"
        }, {
            1336, "-1"
        }, {
            1339, "-1"
        }, {
            1346, "-1"
        }, {
            1343, "-1"
        }, {
            1337, "-1"
        }, {
            1342, "-1"
        }, {
            1341, "-1"
        }, {
            1331, "-1"
        }, {
            1340, "-1"
        }, {
            1338, "-1"
        }, {
            1329, "-1"
        }
    };


    public static readonly EurekaFate[] AnemosFates = [
        new(1332, "仙人掌", new Vector2(14f, 22f), None, false),
        new(1348, "章鱼", new Vector2(30f, 27f), None, false),
        new(1333, "鸟", new Vector2(25.9f, 27f), None, false),
        new(1328, "蜻蜓", new Vector2(17.1f, 22.2f), None, false),
        new(1344, "熊", new Vector2(26f, 22f), None, false),
        new(1347, "群偶", new Vector2(24f, 23f), None, false),
        new(1345, "台风", new Vector2(19.1f, 19.6f), Gales, false),
        new(1334, "暴龙", new Vector2(15f, 16f), None, false),
        new(1335, "盖因", new Vector2(14f, 13f), None, false),
        new(1336, "举高高", new Vector2(28.2f, 20.3f), None, true),
        new(1339, "蝎子", new Vector2(24.9f, 18.2f), None, false),
        new(1346, "魔界花", new Vector2(21.9f, 14.5f), None, false),
        new(1343, "白骑士", new Vector2(20f, 13f), None, true),
        new(1337, "独眼", new Vector2(26f, 14f), None, false),
        new(1342, "阔步西牟鸟", new Vector2(29f, 13f), None, false),
        new(1341, "极度危险物质", new Vector2(35f, 18f), None, false),
        new(1331, "法夫纳", new Vector2(36f, 22f), None, true),
        new(1340, "阿玛洛克", new Vector2(7.7f, 17.9f), None, false),
        new(1338, "拉玛什图", new Vector2(7.6f, 26.6f), None, true),
        new(1329, "帕祖祖", new Vector2(7.4f, 21.6f), Gales, true)
    ];


    public static (PData.EurekaWeather Weather, TimeSpan Timeleft) GetCurrentWeatherInfo() => EorzeaWeather.GetCurrentWeatherInfo(Weathers);

    public static List<(PData.EurekaWeather Weather, TimeSpan Time)> GetAllNextWeatherTime() => EorzeaWeather.GetAllWeathers(Weathers);
}