using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;

namespace DataGenerator;

internal class Program {
	//OBJECT_TYPE_TRAP = 1;
	//OBJECT_TYPE_HOARD = 2;
	private record PalacePalDTO(int localId, int territoryType, int type, float x, float y, float z, int seen, string sinceVersion);

	public static void Main() {
		using var connection = new SqliteConnection($"Data Source={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncherCN", "pluginConfigs", "PalacePal", "palace-pal.data.sqlite3")};");
		connection.Open();
		using var cmd = new SqliteCommand("SELECT * FROM Locations;", connection);
		using var reader = cmd.ExecuteReader();
		var sb = new StringBuilder();
		while (reader.Read()) {
			var dto = new PalacePalDTO(
				reader.GetInt32(0),
				reader.GetInt32(1),
				reader.GetInt32(2),
				(float)reader.GetDouble(3),
				(float)reader.GetDouble(4),
				(float)reader.GetDouble(5),
				reader.GetInt32(6),
				reader.GetString(7));
			sb.AppendLine($"{dto.territoryType},{dto.type},{dto.x},{dto.y},{dto.z}");
		}
		File.WriteAllText("../../SkyEye/PalacePal.dat", sb.ToString());
	}
}