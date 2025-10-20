using System;
using System.Collections.Generic;
using System.Numerics;
using static SkyEye.SkyEye.Data.PData.EurekaWeather;

namespace SkyEye.SkyEye.Data;

internal static class EurekaAnemos {
    internal static readonly (int, PData.EurekaWeather)[] Weathers = [
        (30, FairSkies),
        (60, Gales),
        (90, Showers),
        (100, Snow)
    ];

    internal static readonly Dictionary<int, string> DeadFateDic = new() {
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


    internal static readonly EurekaFate[] AnemosFates = [
        new(1332, 1, "舞动花王——科里多仙人刺", "仙人掌", "仙人花", 6, new Vector2(13.9f, 21.6f)),
        new(1348, 2, "章鱼统领——常风领主", "章鱼", "海祭司", 7, new Vector2(29.7f, 27.1f)),
        new(1333, 3, "绝命美声——忒勒斯", "鸟", "常风哈佩亚鸟妖", 8, new Vector2(25.6f, 27.4f)),
        new(1328, 4, "御驾亲征——常风皇帝", "蜻蜓", "晏蜓", 9, new Vector2(17.2f, 22.2f)),
        new(1344, 5, "行尸走肉——卡利斯托", "熊", "瓦尔巨熊", 10, new Vector2(25.5f, 22.3f)),
        new(1347, 6, "无主傀儡——群偶", "群偶", "夺灵魔", 11, new Vector2(23.5f, 22.7f)),
        new(1345, 7, "强风妖精——哲罕南", "台风", "台风元精", 12, new Vector2(17.7f, 18.6f), Gales),
        new(1334, 8, "贪食者——阿米特", "暴龙", "阿卜拉克萨斯", 13, new Vector2(15f, 15.6f)),
        new(1335, 9, "兽脚怪人——盖因", "盖因", "追踪席兹", 14, new Vector2(13.8f, 12.5f)),
        new(1336, 10, "腐臭贤者——庞巴德", "举高高", "古老贪吃鬼", 15, new Vector2(28.3f, 20.4f), None, true),
        new(1339, 11, "幻魔蝎——塞尔凯特", "蝎子", "河道巨钳虾", 16, new Vector2(24.8f, 17.9f)),
        new(1346, 12, "播种者——武断魔花茱莉卡", "魔界花", "天仙子", 17, new Vector2(21.9f, 15.6f)),
        new(1343, 13, "胜利象征——白骑士", "白骑士", "黄昏无头骑士", 18, new Vector2(20.3f, 13f), None, true),
        new(1337, 14, "巨人的复仇——波吕斐摩斯", "独眼", "独眼怪", 19, new Vector2(26.4f, 14.3f)),
        new(1342, 15, "狂怒怪鸟——阔步西牟鸟", "阔步西牟鸟", "旧世界祖", 20, new Vector2(28.6f, 13f)),
        new(1341, 16, "放火大王——极其危险物质", "极其危险物质", "常风阿那罗", 21, new Vector2(35.3f, 18.3f)),
        new(1331, 17, "狂乱暗龙——法夫纳", "法夫纳", "龙化石", 22, new Vector2(35.5f, 21.5f), None, true),
        new(1340, 18, "异界魔犬——阿玛洛克", "阿玛洛克", "虚无鳞龙", 23, new Vector2(7.6f, 18.2f)),
        new(1338, 19, "魔王之后——拉玛什图", "拉玛什图", "瓦尔妖影", 24, new Vector2(7.7f, 23.3f), None, true),
        new(1329, 20, "暴风魔王——帕祖祖", "帕祖祖", "暗影幽灵", 25, new Vector2(7.4f, 21.7f), Gales, true)
    ];


    internal static (PData.EurekaWeather Weather, TimeSpan Timeleft) GetCurrentWeatherInfo() => EorzeaWeather.GetCurrentWeatherInfo(Weathers);

    internal static List<(PData.EurekaWeather Weather, TimeSpan Time)> GetAllNextWeatherTime() => EorzeaWeather.GetAllWeathers(Weathers);
}