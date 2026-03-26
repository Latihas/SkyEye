using System;
using System.Collections.Generic;
using System.Linq;
using static SkyEye.Data.EorzeaTime;
using static SkyEye.Data.PData;

namespace SkyEye.Data;

internal static class EorzeaWeather {
	private static int CalculateTarget(DateTime dateTime) {
		var num = (int)(dateTime - Zero).TotalSeconds;
		var bell = num / 175;
		var increment = (uint)(bell + 8 - bell % 8) % 24u;
		var calcBase = (uint)(num / 4200 * 100) + increment;
		var step1 = calcBase << 11 ^ calcBase;
		return (int)((step1 >> 8 ^ step1) % 100);
	}

	private static EurekaWeather Forecast((int, EurekaWeather)[] weathers, int chance) =>
		weathers.Where(t => chance < t.Item1).Select(t => t.Item2).FirstOrDefault();

	internal static (EurekaWeather weather, TimeSpan time) GetCurrentWeatherInfo((int, EurekaWeather)[] weathers) =>
		(Forecast(weathers, CalculateTarget(DateTime.Now.ToUniversalTime())), (GetNearestEarthInterval(DateTime.Now) + TimeSpan.FromMilliseconds(1400000)).ToLocalTime() - DateTime.Now);


	internal static List<(EurekaWeather Weather, TimeSpan Time)> GetAllWeathers((int, EurekaWeather)[] weathers) {
		var results = new List<(EurekaWeather, TimeSpan)>();
		var ws = weathers.Select(i => i.Item2).ToArray();
		var bws = new List<EurekaWeather>(ws);
		DateTime nextInterval;
		for (nextInterval = GetNearestEarthInterval(DateTime.Now); results.Count < Plugin.Configuration.NextWeatherCount || bws.Count > 0; nextInterval += TimeSpan.FromMilliseconds(1400000)) {
			var f = Forecast(weathers, CalculateTarget(nextInterval));
			if (!ws.Contains(f)) continue;
			results.Add((f, nextInterval.ToLocalTime() - DateTime.Now));
			bws.Remove(f);
		}
		return results;
	}

	private static List<DateTime> GetCountWeatherForecasts(EurekaWeather targetWeather, int count, (int, EurekaWeather)[] weathers, DateTime start) {
		var timeNow = GetNearestEarthInterval(start);
		var counter = 0;
		var result = new List<DateTime>();
		do {
			var chance = CalculateTarget(timeNow);
			if (Forecast(weathers, chance) == targetWeather) {
				result.Add(timeNow.ToLocalTime());
				counter++;
			}
			timeNow += TimeSpan.FromMilliseconds(1400000);
		} while (counter < count);
		return result;
	}

	internal static (DateTime Start, DateTime End) GetWeatherUptime(EurekaWeather targetWeather, (int, EurekaWeather)[] weathers, DateTime start) {
		var dateTime = GetCountWeatherForecasts(targetWeather, 1, weathers, start)[0];
		return (dateTime, dateTime + TimeSpan.FromMilliseconds(1400000));
	}
}