using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dalamud.Bindings.ImGui;
using Microsoft.Data.Sqlite;
using static SkyEye.Plugin;

namespace SkyEye;

public partial class ConfigWindow {
	private record PalacePalDat(int territoryType, int type, float x, float y, float z);

	private readonly List<PalacePalDat> PalacePalDatList = [];

	private void DrawPalacePal() {
		if (ImGui.Button("读取数据库")) {
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SkyEye.PalacePal.dat");
			if (stream == null) {
				Log.Error("未找到PalacePal资源");
				return;
			}
			var data = new byte[stream.Length];
			stream.ReadExactly(data);
			var f = System.Text.Encoding.UTF8.GetString(data);
			foreach (var p in f.Split("\r\n")) {
				if (string.IsNullOrEmpty(p)) continue;
				var sp = p.Split(',');
				PalacePalDatList.Add(new PalacePalDat(
					int.Parse(sp[0]),
					int.Parse(sp[1]),
					float.Parse(sp[2]),
					float.Parse(sp[3]),
					float.Parse(sp[4])));
			}
		}
		foreach (var p in PalacePalDatList)
			ImGui.Text($"{p.x},{p.y},{p.z}");
	}
}