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
		{ 1388, "-1" },
		{ 1389, "-1" },
		{ 1390, "-1" },
		{ 1391, "-1" },
		{ 1392, "-1" },
		{ 1393, "-1" },
		{ 1394, "-1" },
		{ 1395, "-1" },
		{ 1396, "-1" },
		{ 1397, "-1" },
		{ 1398, "-1" },
		{ 1399, "-1" },
		{ 1400, "-1" },
		{ 1401, "-1" },
		{ 1402, "-1" },
		{ 1403, "-1" },
		{ 1404, "-1" },
		{ 1407, "-1" },
		{ 1408, "-1" }
	};


	internal static readonly List<EurekaFate> PyrosFates = [
		new(1388, "惨叫", new Vector2(320.9842f, 201.438f), PData.EurekaWeather.None, true),
		new(1389, "雷兽", new Vector2(386.33926f, 358.24274f), PData.EurekaWeather.Thunder, false),
		new(1390, "诡辩者", new Vector2(513.146f, 468.17218f), PData.EurekaWeather.None, false),
		new(1391, "塔塔露", new Vector2(94.2181f, 765.32275f), PData.EurekaWeather.None, false),
		new(1392, "阿福", new Vector2(-112.91578f, 342.06964f), PData.EurekaWeather.UmbralWind, false),
		new(1393, "大公", new Vector2(-176.88625f, -394.7388f), PData.EurekaWeather.None, true),
		new(1394, "雷鸟", new Vector2(-578.14294f, -386.20517f), PData.EurekaWeather.None, false),
		new(1395, "蝎子", new Vector2(-433.2961f, -543.60785f), PData.EurekaWeather.None, false),
		new(1396, "火巨人", new Vector2(-315.6653f, -738.8786f), PData.EurekaWeather.None, false),
		new(1397, "海燕", new Vector2(-12.539706f, -486.02472f), PData.EurekaWeather.None, false),
		new(1398, "哥布林", new Vector2(24.877388f, -666.72644f), PData.EurekaWeather.None, false),
		new(1399, "雷军", new Vector2(291.65533f, -631.25104f), PData.EurekaWeather.Thunder, false),
		new(1400, "树人", new Vector2(429.29465f, -496.1107f), PData.EurekaWeather.None, false),
		new(1401, "明眸", new Vector2(543.06006f, -316.8035f), PData.EurekaWeather.None, false),
		new(1402, "阴阳", new Vector2(-491.60144f, 632.4779f), PData.EurekaWeather.None, false),
		new(1403, "狼", new Vector2(126.20996f, 413.7807f), PData.EurekaWeather.Blizzards, false),
		new(1404, "彭女士", new Vector2(720.3792f, -780.43463f), PData.EurekaWeather.HeatWaves, false),
		new(1407, "小兔子", new Vector2(144.02539f, 214.77539f), PData.EurekaWeather.None, false),
		new(1408, "大兔子", new Vector2(172.51315f, -524.5715f), PData.EurekaWeather.None, false)
	];

	public static (PData.EurekaWeather Weather, TimeSpan Timeleft) GetCurrentWeatherInfo() => EorzeaWeather.GetCurrentWeatherInfo(Weathers);
	
	public static List<(PData.EurekaWeather Weather, TimeSpan Time)> GetAllNextWeatherTime() => EorzeaWeather.GetAllWeathers(Weathers);
}