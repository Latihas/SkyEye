using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyEye.SkyEye.Data;

internal static class EorzeaWeather {
	private static int CalculateTarget(DateTime dateTime) {
		var num = (int)(dateTime - EorzeaTime.Zero).TotalSeconds;
		var bell = num / 175;
		var increment = (uint)(bell + 8 - bell % 8) % 24u;
		var calcBase = (uint)(num / 4200 * 100) + increment;
		var step1 = calcBase << 11 ^ calcBase;
		return (int)((step1 >> 8 ^ step1) % 100);
	}

	private static PData.EurekaWeather Forecast((int, PData.EurekaWeather)[] weathers, int chance) =>
		(from _ in weathers
		where chance < _.Item1
		select _.Item2).FirstOrDefault();

	internal static (PData.EurekaWeather weather, TimeSpan time) GetCurrentWeatherInfo((int, PData.EurekaWeather)[] weathers) {
		var chance = CalculateTarget(DateTime.Now.ToUniversalTime());
		var item = Forecast(weathers, chance);
		var timeNow = EorzeaTime.GetNearestEarthInterval(DateTime.Now);
		return (weather: item, time: (timeNow + TimeSpan.FromMilliseconds(1400000.0)).ToLocalTime() - DateTime.Now);
	}

	internal static List<(PData.EurekaWeather Weather, TimeSpan Time)> GetAllWeathers((int, PData.EurekaWeather)[] weathers) {
		var results = new List<(PData.EurekaWeather, TimeSpan)>();
		foreach (var weather in weathers) {
			DateTime nextInterval;
			for (nextInterval = EorzeaTime.GetNearestEarthInterval(DateTime.Now) + TimeSpan.FromMilliseconds(1400000.0); Forecast(weathers, CalculateTarget(nextInterval)) != weather.Item2; nextInterval += TimeSpan.FromMilliseconds(1400000.0)) {
			}
			results.Add((weather.Item2, nextInterval.ToLocalTime() - DateTime.Now));
		}
		return results;
	}

	private static List<DateTime> GetCountWeatherForecasts(PData.EurekaWeather targetWeather, int count, (int, PData.EurekaWeather)[] weathers, DateTime start) {
		var timeNow = EorzeaTime.GetNearestEarthInterval(start);
		var counter = 0;
		var result = new List<DateTime>();
		do {
			var chance = CalculateTarget(timeNow);
			if (Forecast(weathers, chance) == targetWeather) {
				result.Add(timeNow.ToLocalTime());
				counter++;
			}
			timeNow += TimeSpan.FromMilliseconds(1400000.0);
		} while (counter < count);
		return result;
	}

	internal static (DateTime Start, DateTime End) GetWeatherUptime(PData.EurekaWeather targetWeather, (int, PData.EurekaWeather)[] weathers, DateTime start) {
		var dateTime = GetCountWeatherForecasts(targetWeather, 1, weathers, start)[0];
		var timeEnd = dateTime + TimeSpan.FromMilliseconds(1400000.0);
		return (Start: dateTime, End: timeEnd);
	}
}