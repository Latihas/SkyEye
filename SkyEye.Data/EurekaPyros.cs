using System;
using System.Collections.Generic;
using System.Numerics;

namespace SkyEye.SkyEye.Data;

internal static class EurekaPyros {
    internal static readonly (int, PData.EurekaWeather)[] Weathers = [
        (10, PData.EurekaWeather.FairSkies),
        (28, PData.EurekaWeather.HeatWaves),
        (46, PData.EurekaWeather.Thunder),
        (64, PData.EurekaWeather.Blizzards),
        (82, PData.EurekaWeather.UmbralWind),
        (100, PData.EurekaWeather.Snow)
    ];

    internal static readonly Dictionary<int, string> DeadFateDic = new() {
        {
            1388, "-1"
        }, {
            1389, "-1"
        }, {
            1390, "-1"
        }, {
            1391, "-1"
        }, {
            1392, "-1"
        }, {
            1393, "-1"
        }, {
            1394, "-1"
        }, {
            1395, "-1"
        }, {
            1396, "-1"
        }, {
            1397, "-1"
        }, {
            1398, "-1"
        }, {
            1399, "-1"
        }, {
            1400, "-1"
        }, {
            1401, "-1"
        }, {
            1402, "-1"
        }, {
            1403, "-1"
        }, {
            1404, "-1"
        }, {
            1407, "-1"
        }, {
            1408, "-1"
        }
    };


    internal static readonly EurekaFate[] PyrosFates = [
        new(1407, 35, "瞄准珊瑚的幸福兔", "小兔子", "", -1, new Vector2(24f, 26f)),
        new(1408, 46, "困入岩石的幸福兔", "大兔子", "", -1, new Vector2(25f, 11.1f)),
        new(1388, 35, "洁白的惨叫——琉科西亚", "惨叫", "涌火浮灵", 40, new Vector2(26.9f, 26.6f), spawnByRequiredNight: true),
        new(1389, 36, "狰狞的雷兽——佛劳洛斯", "雷兽", "雷暴元精", 41, new Vector2(30f, 28.4f), PData.EurekaWeather.Thunder),
        new(1390, 37, "妖异中的辩论家——诡辩者", "诡辩者", "涌火阿班达", 42, new Vector2(31.9f, 31.3f)),
        new(1391, 38, "恐怖的人偶——格拉菲亚卡内", "塔塔露", "瓦尔维京人偶", 43, new Vector2(23f, 37.2f)),
        new(1392, 39, "图书守护者——阿斯卡拉福斯", "阿福", "过期魔导书", 44, new Vector2(19.1f, 29.1f), PData.EurekaWeather.UmbralWind),
        new(1393, 40, "深渊贵族——巴钦大公爵", "大公", "暗黑行吟诗人", 45, new Vector2(17.7f, 14.5f), spawnByRequiredNight: true),
        new(1394, 41, "闪电的指挥者——埃托洛斯", "雷鸟", "瓦尔独爪妖禽", 46, new Vector2(10f, 14f)),
        new(1395, 42, "灼热的刺剑——来萨特", "蝎子", "食鸟者", 47, new Vector2(13.7f, 11.5f)),
        new(1396, 43, "炎热霸主——火巨人", "火巨人", "涌火陆蟹", 48, new Vector2(15.4f, 7f)),
        new(1397, 44, "落泪的海燕——伊丽丝", "海燕", "北境盐蓝燕", 49, new Vector2(21.3f, 11.8f)),
        new(1398, 45, "奇迹的生还者——佣兵雷姆普里克斯", "哥布林", "青蓝之手逃亡者", 50, new Vector2(22.1f, 8.3f)),
        new(1399, 46, "雷兽统领——闪电督军", "雷军", "遗弃象魔", 51, new Vector2(27.1f, 9f), PData.EurekaWeather.Thunder),
        new(1400, 47, "樵夫杰科的死亡对决", "树人", "涌火树精", 52, new Vector2(29.9f, 11.8f)),
        new(1401, 48, "智慧与战斗之母——明眸", "明眸", "瓦尔斯卡尼特", 53, new Vector2(31.8f, 15.1f)),
        new(1402, 49, "相反的双子——阴·阳", "阴阳", "涌火百目妖", 54, new Vector2(11.7f, 34.3f)),
        new(1403, 50, "嘲讽的霜狼——斯库尔", "狼", "涌火狗灵", 55, new Vector2(24f, 30f), PData.EurekaWeather.Blizzards),
        new(1404, 50, "炎蝶的女王——彭忒西勒亚", "彭女士", "瓦尔血飞蛾", 55, new Vector2(36f, 6f), PData.EurekaWeather.HeatWaves)
    ];

    internal static (PData.EurekaWeather Weather, TimeSpan Timeleft) GetCurrentWeatherInfo() => EorzeaWeather.GetCurrentWeatherInfo(Weathers);

    internal static List<(PData.EurekaWeather Weather, TimeSpan Time)> GetAllNextWeatherTime() => EorzeaWeather.GetAllWeathers(Weathers);
}