using System;
using System.Collections.Generic;
using System.Numerics;

namespace SkyEye.SkyEye.Data;

internal static class EurekaPagos {
    internal static readonly (int, PData.EurekaWeather)[] Weathers = [
        (10, PData.EurekaWeather.FairSkies),
        (28, PData.EurekaWeather.Fog),
        (46, PData.EurekaWeather.HeatWaves),
        (64, PData.EurekaWeather.Snow),
        (82, PData.EurekaWeather.Thunder),
        (100, PData.EurekaWeather.Blizzards)
    ];

    internal static readonly Dictionary<int, string> DeadFateDic = new() {
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


    internal static readonly EurekaFate[] PagosFates = [
        new(1367, 20, "雪上的幸福兔", "小兔子", "", -1, new Vector2(18, 27.5f)),
        new(1368, 31, "盯上宝石的幸福兔", "大兔子", "", -1, new Vector2(21, 21.5f)),
        new(1351, 20, "纯白的支配者——雪之女王", "周冬雨", "雪童子", 25, new Vector2(21.9f, 26.8f)),
        new(1369, 21, "腐烂的读书家——塔克西姆", "读书人", "珍卷恶魔", 26, new Vector2(25.4f, 27.4f), spawnByRequiredNight: true),
        new(1353, 22, "灰壳的鳞王——灰烬龙", "灰烬龙", "血魔", 27, new Vector2(29f, 30f)),
        new(1354, 23, "地壳变动之谜——异形魔虫", "魔虫", "瓦尔巨虫", 28, new Vector2(33f, 27f)),
        new(1355, 24, "融雪的化身——安娜波", "安娜波", "融雪元精", 29, new Vector2(33f, 21.5f), PData.EurekaWeather.Fog),
        new(1366, 25, "五行眼的主人——白泽", "白泽", "啜泣百目妖", 30, new Vector2(29f, 22f)),
        new(1357, 26, "移动的雪洞——雪屋王", "雪屋王", "胡瓦西", 31, new Vector2(17f, 16f)),
        new(1356, 27, "硬质的病魔——阿萨格", "阿萨格", "徘徊欧浦肯", 32, new Vector2(10.4f, 11.4f)),
        new(1352, 28, "家畜的慈母——苏罗毗", "山羊", "恒冰公山羊", 33, new Vector2(10.3f, 19.5f)),
        new(1360, 29, "圆桌的雾王——亚瑟罗王", "螃蟹", "瓦尔利螯陆蟹", 34, new Vector2(8.7f, 15.5f), PData.EurekaWeather.Fog),
        new(1358, 30, "唇亡齿寒", "双牛", "研究所弥诺陶洛斯", 35, new Vector2(14f, 19f)),
        new(1361, 31, "野牛的救世主——优雷卡圣牛", "圣牛", "古老水牛", 36, new Vector2(26f, 16f)),
        new(1362, 32, "雷云的魔兽——哈达约什", "贝爷", "虚无小龙", 37, new Vector2(30f, 19f), PData.EurekaWeather.Thunder),
        new(1359, 33, "太阳的使者——荷鲁斯", "荷鲁斯", "虚无薇薇尔飞龙", 38, new Vector2(26f, 20f), PData.EurekaWeather.HeatWaves),
        new(1363, 34, "暗眼王——总领安哥拉·曼纽", "大眼", "瞪视之眼", 39, new Vector2(24f, 25f)),
        new(1365, 35, "模仿犯——复制魔花凯西", "凯西", "阿米雷戴", 40, new Vector2(22.3f, 14.4f), PData.EurekaWeather.Blizzards),
        new(1364, 35, "苍蓝冰刃——娄希", "娄希", "瓦尔腐尸", 40, new Vector2(36f, 19f), spawnByRequiredNight: true)
    ];


    internal static (PData.EurekaWeather Weather, TimeSpan Timeleft) GetCurrentWeatherInfo() => EorzeaWeather.GetCurrentWeatherInfo(Weathers);


    internal static List<(PData.EurekaWeather Weather, TimeSpan Time)> GetAllNextWeatherTime() => EorzeaWeather.GetAllWeathers(Weathers);
}