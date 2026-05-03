using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using static SkyEye.Plugin;


namespace SkyEye;

public partial class ConfigWindow {
	private static void DrawTarget() {
		ImGui.Text("连线");
		if (ImGui.InputText("连线查找怪", ref Configuration.FindEntity, 114514)) Configuration.Save();
		var Cats = new Dictionary<int, int> {
			{ 0, 0 },
			{ 1, 0 },
			{ 2, 0 },
			{ 3, 0 },
			{ 10, 0 },
			{ 11, 0 },
			{ 20, 0 },
			{ 21, 0 },
			{ 30, 0 },
			{ 31, 0 },
			{ 40, 0 },
			{ 41, 0 },
			{ 50, 0 },
			{ 51, 0 },
			{ 60, 0 },
			{ 61, 0 },
			{ 70, 0 },
			{ 71, 0 },
			{ 80, 0 },
			{ 81, 0 }
		};
		foreach (var obj in ObjectTable) {
			if (obj is not IPlayerCharacter i) continue;
			var hw = i.HomeWorld.Value.DataCenter.Value.Name.ToString();
			switch (hw) {
				case "陆行鸟": Cats[0] += 1; break;
				case "猫小胖": Cats[1] += 1; break;
				case "莫古力": Cats[2] += 1; break;
				case "豆豆柴": Cats[3] += 1; break;
			}
			switch (i.CustomizeData) {
				case { Race: 1, Sex: 0 }: Cats[10] += 1; break;
				case { Race: 1, Sex: 1 }: Cats[11] += 1; break;
				case { Race: 2, Sex: 0 }: Cats[20] += 1; break;
				case { Race: 2, Sex: 1 }: Cats[21] += 1; break;
				case { Race: 3, Sex: 0 }: Cats[30] += 1; break;
				case { Race: 3, Sex: 1 }: Cats[31] += 1; break;
				case { Race: 4, Sex: 0 }: Cats[40] += 1; break;
				case { Race: 4, Sex: 1 }: Cats[41] += 1; break;
				case { Race: 5, Sex: 0 }: Cats[50] += 1; break;
				case { Race: 5, Sex: 1 }: Cats[51] += 1; break;
				case { Race: 6, Sex: 0 }: Cats[60] += 1; break;
				case { Race: 6, Sex: 1 }: Cats[61] += 1; break;
				case { Race: 7, Sex: 0 }: Cats[70] += 1; break;
				case { Race: 7, Sex: 1 }: Cats[71] += 1; break;
				case { Race: 8, Sex: 0 }: Cats[80] += 1; break;
				case { Race: 8, Sex: 1 }: Cats[81] += 1; break;
			}
		}
		ImGui.Text("地域歧视");
		if (ImGui.Checkbox($"猪区({Cats[2]})", ref Configuration.FindCharaZhu)) Configuration.Save();
		ImGui.SameLine();
		if (ImGui.Checkbox($"鸟区({Cats[0]})", ref Configuration.FindCharaNiao)) Configuration.Save();
		ImGui.SameLine();
		if (ImGui.Checkbox($"猫区({Cats[1]})", ref Configuration.FindCharaMao)) Configuration.Save();
		ImGui.SameLine();
		if (ImGui.Checkbox($"狗区({Cats[3]})", ref Configuration.FindCharaGou)) Configuration.Save();
		ImGui.Text("种族歧视");
		if (ImGui.Checkbox($"人族男({Cats[10]})", ref Configuration.FindRaceRenM)) Configuration.Save();
		ImGui.SameLine();
		if (ImGui.Checkbox($"人族女({Cats[11]})", ref Configuration.FindRaceRenF)) Configuration.Save();
		if (ImGui.Checkbox($"精灵族男({Cats[20]})", ref Configuration.FindRaceJingLingM)) Configuration.Save();
		ImGui.SameLine();
		if (ImGui.Checkbox($"精灵族女({Cats[21]})", ref Configuration.FindRaceJingLingF)) Configuration.Save();
		if (ImGui.Checkbox($"拉拉菲尔族男({Cats[30]})", ref Configuration.FindRaceLaLaFeiErM)) Configuration.Save();
		ImGui.SameLine();
		if (ImGui.Checkbox($"拉拉菲尔族女({Cats[31]})", ref Configuration.FindRaceLaLaFeiErF)) Configuration.Save();
		if (ImGui.Checkbox($"猫魅族男({Cats[40]})", ref Configuration.FindRaceMaoMeiM)) Configuration.Save();
		ImGui.SameLine();
		if (ImGui.Checkbox($"猫魅族女({Cats[41]})", ref Configuration.FindRaceMaoMeiF)) Configuration.Save();
		if (ImGui.Checkbox($"鲁加族男({Cats[50]})", ref Configuration.FindRaceLuJiaM)) Configuration.Save();
		ImGui.SameLine();
		if (ImGui.Checkbox($"鲁加族女({Cats[51]})", ref Configuration.FindRaceLuJiaF)) Configuration.Save();
		if (ImGui.Checkbox($"敖龙族男({Cats[60]})", ref Configuration.FindRaceAoLongM)) Configuration.Save();
		ImGui.SameLine();
		if (ImGui.Checkbox($"敖龙族女({Cats[61]})", ref Configuration.FindRaceAoLongF)) Configuration.Save();
		if (ImGui.Checkbox($"硌狮族男({Cats[70]})", ref Configuration.FindRaceGeShiM)) Configuration.Save();
		ImGui.SameLine();
		if (ImGui.Checkbox($"硌狮族女({Cats[71]})", ref Configuration.FindRaceGeShiF)) Configuration.Save();
		if (ImGui.Checkbox($"维埃拉族男({Cats[80]})", ref Configuration.FindRaceWeiAiLaM)) Configuration.Save();
		ImGui.SameLine();
		if (ImGui.Checkbox($"维埃拉族女({Cats[81]})", ref Configuration.FindRaceWeiAiLaF)) Configuration.Save();
	}
}