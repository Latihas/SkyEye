using System;

namespace SkyEye.SkyEye.Data;

public class EorzeaTime(DateTime dateTime) {
	public static readonly DateTime Zero = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	public DateTime EorzeaDateTime { get; } = dateTime;
	public static EorzeaTime ToEorzeaTime(DateTime dateTime) => new(new DateTime((long)Math.Round((dateTime.ToUniversalTime().Ticks - Zero.Ticks) * 20.571428571428573)));

	public static DateTime GetNearestEarthInterval(DateTime dateTime) {
		var num = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
		return Zero + TimeSpan.FromSeconds((num - num % 1400000) / 1000);
	}

	public TimeSpan TimeUntilDay() {
		var nextNight = EorzeaDateTime.Hour >= 6 ? EorzeaDateTime.Date + new TimeSpan(1, 6, 0, 0) : EorzeaDateTime.Date + new TimeSpan(6, 0, 0);
		return TimeSpan.FromTicks(Convert.ToInt64((nextNight - EorzeaDateTime).Ticks * 7.0 / 144.0));
	}

	public TimeSpan TimeUntilNight() {
		var nextDay = EorzeaDateTime.Hour >= 19 ? EorzeaDateTime.Date + new TimeSpan(1, 19, 0, 0) : EorzeaDateTime.Date + new TimeSpan(19, 0, 0);
		return TimeSpan.FromTicks(Convert.ToInt64((nextDay - EorzeaDateTime).Ticks * 7.0 / 144.0));
	}
}