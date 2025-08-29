using System;
using System.Collections.Generic;
using System.Numerics;

namespace SkyEye.SkyEye.Data;

internal static class EurekaHydatos {
	public static readonly (int, PData.EurekaWeather)[] Weathers = [
		(12, PData.EurekaWeather.FairSkies),
		(34, PData.EurekaWeather.Showers),
		(56, PData.EurekaWeather.Gloom),
		(78, PData.EurekaWeather.Thunderstorms),
		(100, PData.EurekaWeather.Snow)
	];

	public static readonly Dictionary<int, string> DeadFateDic = new() {
		{ 1412, "-1" },
		{ 1413, "-1" },
		{ 1414, "-1" },
		{ 1415, "-1" },
		{ 1416, "-1" },
		{ 1417, "-1" },
		{ 1418, "-1" },
		{ 1419, "-1" },
		{ 1420, "-1" },
		{ 1421, "-1" },
		{ 1422, "-1" },
		{ 1423, "-1" },
		{ 1424, "-1" },
		{ 1425, "-1" }
	};


	public static readonly List<EurekaFate> HydatosFates = [
		new(1412, "墨鱼", new Vector2(-527.2876f, -314.13358f), PData.EurekaWeather.None, false),
		new(1413, "象", new Vector2(-611.0604f, -650.2218f), PData.EurekaWeather.None, false),
		new(1414, "摩洛", new Vector2(-687.49335f, -460.28763f), PData.EurekaWeather.None, false),
		new(1415, "皮鸟", new Vector2(-720.3792f, -780.43463f), PData.EurekaWeather.None, false),
		new(1416, "老虎", new Vector2(-666.6416f, -240.2232f), PData.EurekaWeather.None, false),
		new(1417, "达芙涅", new Vector2(209.62729f, -741.035f), PData.EurekaWeather.None, false),
		new(1418, "马王", new Vector2(374.07266f, -356.36008f), PData.EurekaWeather.None, true),
		new(1419, "琉刻", new Vector2(790.61536f, -210.9793f), PData.EurekaWeather.None, false),
		new(1420, "巴龙", new Vector2(542.1913f, -343.35782f), PData.EurekaWeather.None, false),
		new(1421, "刻托", new Vector2(729.041f, -878.44293f), PData.EurekaWeather.None, false),
		new(1423, "守望者", new Vector2(559.084f, -574.3144f), PData.EurekaWeather.None, false),
		new(1424, "UFO", new Vector2(260.78485f, -108.390564f), PData.EurekaWeather.None, false),
		new(1422, "光灵鳐", new Vector2(-134.37721f, -74.77259f), PData.EurekaWeather.None, false),
		new(1425, "兔子", new Vector2(-370.83264f, -485.80295f), PData.EurekaWeather.None, false)
	];


	public static (PData.EurekaWeather Weather, TimeSpan Timeleft) GetCurrentWeatherInfo() => EorzeaWeather.GetCurrentWeatherInfo(Weathers);

	public static List<(PData.EurekaWeather Weather, TimeSpan Time)> GetAllNextWeatherTime() => EorzeaWeather.GetAllWeathers(Weathers);
}