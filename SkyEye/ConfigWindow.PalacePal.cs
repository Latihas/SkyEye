using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using static SkyEye.Plugin;

namespace SkyEye;

public partial class ConfigWindow {
	internal record PalacePalDat(uint territoryType, int type, Vector3 position);

	internal static readonly List<PalacePalDat> PalacePalDatList = [];
	internal static uint[] PalacePalDatTerritoryIds = [];
	internal static Dictionary<uint, List<Hunt>> HuntData = [];

	public class Hunt {
		public string rank;
		public uint id;
		public int level;
	}

	private static void DrawPalacePal() {
		if (ImGui.Checkbox("启用", ref Configuration.EnablePalacePal)) Configuration.Save();
		foreach (var p in PalacePalDatList)
			ImGui.Text($"{p.position.X},{p.position.Y},{p.position.Z}");
	}
}