using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using SkyEye.Data;
using static SkyEye.Data.Territory;
using static SkyEye.Plugin;

namespace SkyEye;

public partial class ConfigWindow {
	private static void DrawOccultCrowd() {
		if (ImGui.Checkbox("启用众包数据", ref Configuration.EnableOccultCrowd)) Configuration.Save();
		if (ImGui.Checkbox("显示罐子计时到服务器信息栏", ref Configuration.ShowOccultPotDtrBar)) Configuration.Save();
		if (ImGui.InputInt("轮询间隔(秒)", ref Configuration.OccultTrackerIntervalSeconds)) {
			Configuration.OccultTrackerIntervalSeconds = Math.Clamp(Configuration.OccultTrackerIntervalSeconds, 1, 60);
			Configuration.Save();
		}
		if (ImGui.Button("立即刷新")) Plugin.OccultTracker.RequestRefresh();
		ImGui.SameLine();
		if (ImGui.Button("清除缓存")) Plugin.OccultTracker.ClearCache();

		var snapshot = Plugin.OccultTracker.Snapshot;
		if (snapshot.Error != null)
			ImGui.TextColored(new Vector4(1f, 0.35f, 0.35f, 1f), $"获取失败: {snapshot.Error}");
		if (snapshot.Records.Count == 0) {
			ImGui.Text("暂无众包数据。");
			return;
		}

		ImGui.Text($"上次更新: {snapshot.RetrievedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
		var territories = InOccult()
			? new[] { (Territory)ClientState.TerritoryType }
			: new[] { SouthHorn, NorthHorn };
		foreach (var territory in territories) DrawTerritoryCrowd(territory);
	}

	private static void DrawTerritoryCrowd(Territory territory) {
		var records = Plugin.OccultTracker.Snapshot.Records.Count(record => record.Territory == (uint)territory);
		ImGui.Separator();
		ImGui.Text($"{GetTerritoryName(territory)}：{records} 条记录");
		DrawCrowdCategory(territory, "罐子 FATE", OccultEventCategory.Bunny);
		DrawCrowdCategory(territory, "CE", OccultEventCategory.Encounter);
		DrawCrowdCategory(territory, "FATE", OccultEventCategory.Fate);
	}

	private static void DrawCrowdCategory(Territory territory, string title, OccultEventCategory category) {
		var events = Plugin.OccultTracker.GetEvents((uint)territory, category);
		if (!ImGui.CollapsingHeader($"{title} ({events.Count})", ImGuiTreeNodeFlags.DefaultOpen)) return;
		if (events.Count == 0) {
			ImGui.Text("暂无记录。");
			return;
		}
		if (!ImGui.BeginTable($"OccultCrowd{territory}{category}", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg)) return;
		ImGui.TableSetupColumn("名字");
		ImGui.TableSetupColumn("状态");
		ImGui.TableSetupColumn("时间");
		ImGui.TableSetupColumn("最近看到");
		ImGui.TableSetupColumn("FATE击杀");
		ImGui.TableSetupColumn("CE击杀");
		ImGui.TableHeadersRow();
		foreach (var fate in events) {
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.Text(FateDict[fate.FateId]);
			ImGui.TableNextColumn();
			ImGui.Text(OccultTracker.IsActive(fate) ? "active" : "history");
			ImGui.TableNextColumn();
			ImGui.Text(OccultTracker.FormatEpoch(OccultTracker.IsActive(fate) ? fate.SpawnTime : fate.DeathTime));
			ImGui.TableNextColumn();
			ImGui.Text(OccultTracker.FormatEpoch(fate.LastSeen));
			ImGui.TableNextColumn();
			ImGui.Text(fate.KilledFates.ToString());
			ImGui.TableNextColumn();
			ImGui.Text(fate.KilledCes.ToString());
		}
		ImGui.EndTable();
	}

	private static string GetTerritoryName(Territory territory) => territory switch {
		SouthHorn => "南岛",
		NorthHorn => "北岛",
		_ => territory.ToString()
	};
}