using System;
using System.Collections.Generic;
using System.Numerics;

namespace SkyEye.SkyEye.Data;

internal static class EurekaHydatos {
    internal static readonly (int, PData.EurekaWeather)[] Weathers = [
        (12, PData.EurekaWeather.FairSkies),
        (34, PData.EurekaWeather.Showers),
        (56, PData.EurekaWeather.Gloom),
        (78, PData.EurekaWeather.Thunderstorms),
        (100, PData.EurekaWeather.Snow)
    ];

    internal static readonly Dictionary<int, string> DeadFateDic = new() {
        {
            1412, "-1"
        }, {
            1413, "-1"
        }, {
            1414, "-1"
        }, {
            1415, "-1"
        }, {
            1416, "-1"
        }, {
            1417, "-1"
        }, {
            1418, "-1"
        }, {
            1419, "-1"
        }, {
            1420, "-1"
        }, {
            1421, "-1"
        }, {
            1422, "-1"
        }, {
            1423, "-1"
        }, {
            1424, "-1"
        }, {
            1425, "-1"
        }
    };


    internal static readonly EurekaFate[] HydatosFates = [
        new(1425, 50, "戏水的幸福兔", "兔子", "", -1, new Vector2(14.4f, 22f)),
        new(1412, 50, "奇怪的乌贼——卡拉墨鱼", "墨鱼", "左米特", 55, new Vector2(10.8f, 25.5f)),
        new(1413, 51, "暴虐的魔兽——剑齿象", "象", "丰水曙象", 56, new Vector2(9f, 17f)),
        new(1414, 52, "落泪的君主——摩洛", "摩洛", "瓦尔泥口花", 57, new Vector2(7.8f, 22.2f)),
        new(1415, 53, "惊鸿艳影——皮艾萨邪鸟", "皮鸟", "多彩冠恐鸟", 58, new Vector2(7f, 14f)),
        new(1416, 54, "高傲的猎人——霜鬃猎魔", "老虎", "北方猛虎", 59, new Vector2(8f, 25f)),
        new(1417, 55, "浴血的妖妃——达佛涅", "达芙涅", "暗黑虚无鬼鱼", 60, new Vector2(25f, 15f)),
        new(1418, 56, "异界的锻冶王——戈尔德马尔王", "马王", "丰水幽灵", 61, new Vector2(29f, 23.5f), spawnByRequiredNight: true),
        new(1419, 57, "食妖植物——琉刻", "琉刻", "虎鹰", 62, new Vector2(37f, 26f)),
        new(1420, 58, "业火狮子王——巴龙", "巴龙", "研究所雄狮", 63, new Vector2(32.5f, 24.5f)),
        new(1421, 59, "魔蛇女王——刻托", "刻托", "丰水达菲妮", 64, new Vector2(36f, 14f)),
        new(1423, 60, "水晶之龙——起源守望者", "守望者", "水晶爪", 65, new Vector2(32.8f, 19.7f)),
        new(1424, 60, "未知的威胁——未确认飞行物体", "UFO", "", -1, new Vector2(27.1f, 29f)),
        new(1422, 60, "兵武塔调查支援", "光灵鳐", "", -1, new Vector2(18.8f, 28.9f))
    ];


    internal static (PData.EurekaWeather Weather, TimeSpan Timeleft) GetCurrentWeatherInfo() => EorzeaWeather.GetCurrentWeatherInfo(Weathers);

    internal static List<(PData.EurekaWeather Weather, TimeSpan Time)> GetAllNextWeatherTime() => EorzeaWeather.GetAllWeathers(Weathers);
}