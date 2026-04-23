using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using static SkyEye.Ipcs;
using static SkyEye.MConfiguration;
using static SkyEye.Plugin;
using static SkyEye.Util;
using Action = System.Action;

namespace SkyEye;

public partial class ConfigWindow() : Window("SkyEye") {
	private static readonly string[] regions = ["未选择", "陆行鸟", "莫古力", "猫小胖", "豆豆柴"];

	private static void NewTab(string tabname, Action act) {
		if (!ImGui.BeginTabItem(tabname)) return;
		act();
		ImGui.EndTabItem();
	}

	private static void ValidateSpeedInfo() {
		SetSpeed(1);
		if (!Configuration.SpeedUp[0].SpeedUpTerritory.Equals(SpeedInfo.Default().SpeedUpTerritory))
			Configuration.SpeedUp[0].SpeedUpTerritory = SpeedInfo.Default().SpeedUpTerritory;
		if (!Configuration.SpeedUp[0].Desc.Equals(SpeedInfo.Default().Desc))
			Configuration.SpeedUp[0].Desc = SpeedInfo.Default().Desc;
		if (Configuration.SpeedUp[^2].SpeedUpTerritory.IsNullOrEmpty())
			Configuration.SpeedUp.Remove(Configuration.SpeedUp[^2]);
		if (!Configuration.SpeedUp[^1].SpeedUpTerritory.IsNullOrEmpty())
			Configuration.SpeedUp.Add(new SpeedInfo());
		CurrentSpeedInfo = null;
		foreach (var s in Configuration.SpeedUp.Where(s => s.Enabled && s.SpeedUpTerritory.Split('|').Contains(ClientState.TerritoryType.ToString()))) {
			CurrentSpeedInfo = s;
			break;
		}
		SetSpeed(1);
		Configuration.Save();
	}

	public override void Draw() {
		if (ImGui.Checkbox("开关", ref Configuration.PluginEnabled)) Configuration.Save();
		if (!Configuration.PluginEnabled) {
			SetSpeed(1);
			lastFarmPos = null;
			FarmFull = false;
			Stop();
			return;
		}
		if (ImGui.BeginTabBar("tab")) {
			NewTab("基础", () => {
				if (ImGui.Checkbox("稀有天气时间开关", ref Configuration.Overlay2DWeatherMapEnabled)) Configuration.Save();
				if (ImGui.Checkbox("详细信息开关", ref Configuration.Overlay2DDetailEnabled)) Configuration.Save();
				if (!InEureka()) return;
				ImGui.Separator();
				ImGui.Text("NM开战时间喊话");
				if (ImGui.InputText("输入NM开战时间", ref Configuration.NmBattleTimeText, 128)) Configuration.Save();
				ImGui.SameLine();
				if (ImGui.Button("发送至喊话频道") && !string.IsNullOrWhiteSpace(Configuration.NmBattleTimeText))
					ChatBox.SendMessage($"/sh <pos>{Configuration.NmBattleTimeText}");
				ImGui.Separator();
				for (var i = 0; i < YlPositions.Count; i++) {
					var p = YlPositions[i];
					ImGui.Text($"元灵{i}({p.X},{p.Y},{p.Z})");
					ImGui.SameLine();
					if (ImGui.Button($"发送位置{i}")) {
						unsafe {
							AgentMap.Instance()->SetFlagMapMarker(ClientState.TerritoryType, ClientState.MapId, p);
							ChatBox.SendMessage($"/sh 元灵位置{i}: <flag>");
						}
					}
				}
			});
			NewTab("元灵", () => {
				if (ImGui.Checkbox("元灵位置绘制开关", ref Configuration.Overlay3DEnabled)) Configuration.Save();
				ImGui.Text("当前Flag地面坐标: " + FlagToPoint());
				if (ImGui.InputFloat("Flag环绕绘制距离", ref Configuration.FlagR)) Configuration.Save();
				if (ImGui.Button("Flag环绕绘制")) {
					Task.Run(async () => {
						if (ObjectTable.LocalPlayer == null) return;
						const int pointCount = 50;
						var originPosition = ObjectTable.LocalPlayer.Position;
						for (var i = 0; i < pointCount; i++) {
							var angle = 2 * MathF.PI * i / pointCount;
							var offsetX = Configuration.FlagR * MathF.Cos(angle);
							var offsetZ = Configuration.FlagR * MathF.Sin(angle);
							var newPoint = new Vector3(originPosition.X + offsetX, originPosition.Y, originPosition.Z + offsetZ);
							unsafe {
								AgentMap.Instance()->SetFlagMapMarker(ClientState.TerritoryType, ClientState.MapId, newPoint);
							}
							await Task.Delay(20);
						}
					});
				}
				if (ImGui.Checkbox("显示当前地图点位", ref Configuration.ShowCurrentYl)) Configuration.Save();
				foreach (var p in Configuration.AllYlPositions) {
					ImGui.Text(p.Key.ToString());
					ImGui.Indent();
					foreach (var v in p.Value.OrderBy(i => i.X))
						ImGui.Text(v.ToString());
					ImGui.Unindent();
				}
			});
			NewTab("加速", () => {
				ImGui.Text("提示：");
				ImGui.Text("走路倍率6，坐骑倍率6*2。死亡会掉速，点击重置即可恢复。");
				ImGui.Text("地区id用竖线|隔开。");
				if (ImGui.Checkbox("无人就加速", ref Configuration.SpeedUpEnabled)) Configuration.Save();
				ImGui.SameLine();
				if (ImGui.Button("重置")) {
					foreach (var s in Configuration.SpeedUp.Where(s => s.Enabled && s.SpeedUpTerritory.Split('|').Contains(ClientState.TerritoryType.ToString()))) {
						CurrentSpeedInfo = s;
						break;
					}
					SetSpeed(1);
				}
				ImGui.Text("从下往上删没问题，从中间删会导致null项目，点击即可清除");
				ImGui.SameLine();
				if (ImGui.Button("清空null项目")) {
					Configuration.SpeedUp = Configuration.SpeedUp.Where(i => !i.SpeedUpTerritory.IsNullOrEmpty()).ToList();
					ValidateSpeedInfo();
				}
				if (Configuration.SpeedUpEnabled) {
					string[] header = ["启用", "地区Id", "倍率", "最终速度上限(含乘算倍率)", "备注"];
					if (ImGui.BeginTable("TableSpeedInfo", header.Length, ImGuiTableFlag)) {
						foreach (var item in header) ImGui.TableSetupColumn(item, ImGuiTableColumnFlags.WidthStretch);
						ImGui.TableHeadersRow();
						for (var i = 0; i < Configuration.SpeedUp.Count; i++) {
							ImGui.TableNextRow();
							ImGui.TableSetColumnIndex(0);
							if (ImGui.Checkbox($"##启用{i}", ref Configuration.SpeedUp[i].Enabled)) ValidateSpeedInfo();
							ImGui.TableSetColumnIndex(1);
							ImGui.SetNextItemWidth(-1);
							if (ImGui.InputText($"##地区{i}", ref Configuration.SpeedUp[i].SpeedUpTerritory)) ValidateSpeedInfo();
							if (ImGui.IsItemHovered()) {
								var sb = new StringBuilder();
								foreach (var t in Configuration.SpeedUp[i].SpeedUpTerritory.Split('|'))
									if (!t.IsNullOrEmpty() && MapInfo.TryGetValue(t, out var value))
										sb.Append(t).Append('|').Append(value).Append('\n');
								if (sb.Length != 0) {
									sb.Remove(sb.Length - 1, 1);
									ImGui.SetTooltip(sb.ToString());
								}
							}
							ImGui.TableSetColumnIndex(2);
							ImGui.SetNextItemWidth(-1);
							if (ImGui.InputFloat($"##倍率{i}", ref Configuration.SpeedUp[i].SpeedUpN)) ValidateSpeedInfo();
							ImGui.TableSetColumnIndex(3);
							ImGui.SetNextItemWidth(-1);
							if (ImGui.InputFloat($"##最大{i}", ref Configuration.SpeedUp[i].SpeedUpMax)) ValidateSpeedInfo();
							ImGui.TableSetColumnIndex(4);
							ImGui.SetNextItemWidth(-1);
							if (ImGui.InputText($"##描述{i}", ref Configuration.SpeedUp[i].Desc)) ValidateSpeedInfo();
						}
						ImGui.EndTable();
					}
				}
				ImGui.Text("无视周边的挂壁亲友（用竖线|隔开）");
				if (ImGui.InputText("Friendly names", ref Configuration.SpeedUpFriendly, 114514)) Configuration.Save();
				ImGui.Text($"周围人数：{(InArea() ? OtherPlayer.Count : "不在区域内")};区域id：{ClientState.TerritoryType}");
				ImGui.Separator();
				NewTable(["Id", "名称"], MapInfo.Select(p => (p.Key, p.Value)).ToArray(), [
					i => ImGui.Text(i.Key),
					i => ImGui.Text(i.Value)
				], [
					i => i.Key,
					i => i.Value
				], "Territory");
			});
			NewTab("宝箱", () => {
				if (ImGui.Checkbox("宝箱位置绘制开关", ref Configuration.Overlay3DEnabled)) Configuration.Save();
				if (ImGui.Checkbox("进入新场景禁用开宝箱", ref Configuration.DisableAutoRabbitWhenTerritoryChanged)) Configuration.Save();
				if (ImGui.Checkbox("自动开宝箱", ref Configuration.AutoRabbit)) Configuration.Save();
				if (Configuration.AutoRabbit) {
					ImGui.Indent();
					if (ImGui.InputText("开始寻找宝箱前指令", ref Configuration.BeforeFindTreasure)) Configuration.Save();
					if (ImGui.InputText("开宝箱后指令", ref Configuration.AfterFindTreasure)) Configuration.Save();
					ImGui.Unindent();
				}
				if (ImGui.Checkbox("自动导航到新兔子", ref Configuration.AutoRabbitWait)) Configuration.Save();
				if (Configuration.AutoRabbitWait) {
					ImGui.Indent();
					if (ImGui.InputText("开始导航到兔子前指令", ref Configuration.BeforeGotoNewRabbit)) Configuration.Save();
					ImGui.Unindent();
				}
				ImGui.Separator();
				ImGui.Text("统计");
				foreach (var p in Configuration.TotalChest)
					ImGui.Text($"{p.Key}:{p.Value}");
				if (ImGui.Button("清空统计")) {
					Configuration.TotalChest = [];
					Configuration.Save();
				}
			});
			NewTab("农怪", () => {
				ImGui.Text("提示：");
				ImGui.Text("该功能会占用Vnav寻路功能");
				ImGui.Text("移动到北萨那兰会自动切换为刷B怪,名称为");
				ImGui.SameLine();
				if (ImGui.Button("永恒不灭的菲兰德副耀士")) {
					ImGui.SetClipboardText("永恒不灭的菲兰德副耀士");
					NotificationManager.AddNotification(new Notification {
						Title = "已复制",
						Content = "永恒不灭的菲兰德副耀士"
					});
				}
				ImGui.Text("自动农怪可能在第一次开启时无反应，/xivplugins关闭打开一次SkeEye即可。");
				if (ImGui.Checkbox("自动农怪", ref Configuration.AutoFarm)) {
					LastKill = DateTime.Now;
					Configuration.Save();
				}
				if (ImGui.InputText("怪名称", ref Configuration.FarmTarget, 114514)) Configuration.Save();
				string[] distAlgo = ["从第一个怪位置开始计算", "从指定点位开始计算"];
				if (ImGui.Combo("引仇距离计算方式", ref Configuration.FarmDistAlgo, distAlgo)) {
					Configuration.Save();
					ImGui.EndCombo();
				}
				if (Configuration.AutoFarm) {
					if (ImGui.InputText("开怪指令", ref Configuration.FarmStartCommand, 114514)) Configuration.Save();
					if (ImGui.InputInt("最大引仇目标", ref Configuration.FarmTargetMax, 1)) Configuration.Save();
					if (Configuration.FarmDistAlgo == 1) {
						if (ImGui.InputFloat("FarmWaitX", ref Configuration.FarmWaitX)) Configuration.Save();
						if (ImGui.InputFloat("FarmWaitY", ref Configuration.FarmWaitY)) Configuration.Save();
						if (ImGui.InputFloat("FarmWaitZ", ref Configuration.FarmWaitZ)) Configuration.Save();
						if (ImGui.Button("设置农怪返回点为当前坐标") && ObjectTable.LocalPlayer != null) {
							Configuration.FarmWaitX = ObjectTable.LocalPlayer.Position.X;
							Configuration.FarmWaitY = ObjectTable.LocalPlayer.Position.Y;
							Configuration.FarmWaitZ = ObjectTable.LocalPlayer.Position.Z;
							Configuration.Save();
							if (lastFarmPos != null) lastFarmPos = new Vector3(Configuration.FarmWaitX, Configuration.FarmWaitY, Configuration.FarmWaitZ);
						}
					}
					if (ImGui.InputFloat("最大引仇距离", ref Configuration.FarmMaxDistance, 1)) Configuration.Save();
					if (ImGui.Checkbox("打完一波再拉下一波", ref Configuration.FarmWait)) Configuration.Save();
				}
				if (ClientState.TerritoryType == 147 && Configuration.AutoFarm) ImGui.Text($"超时：{(DateTime.Now - LastKill).Seconds}/{FarmTimeout}");
				ImGui.Separator();
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
			});
			NewTab("史书", () => {
				if (ImGui.Checkbox("连接史书", ref Configuration.EnableWss)) {
					WebSocket.nmalive.Clear();
					WebSocket.nmdead.Clear();
					Configuration.Save();
				}
				if (ImGui.InputText("提醒(Fate原名，包含即可，用竖线|隔开)", ref Configuration.WssNotify, 114514)) Configuration.Save();
				if (Configuration.EnableWss) {
					try {
						if (ImGui.Combo("选择大区", ref Configuration.WssRegion, regions)) {
							Configuration.Save();
							if (Configuration.WssRegion is >= 1 and <= 4) WebSocket.StartWssService();
							ImGui.EndCombo();
						}
						if (Configuration.WssRegion is < 1 or > 4) return;
						if (!WebSocket.inited) {
							WebSocket.StartWssService();
							WebSocket.inited = true;
						}
						var acts = new Action<WebSocket.NmInfo>[] {
							info => {
								switch (info.territory_id) {
									case 1:
										ImGui.PushStyleColor(ImGuiCol.TableRowBg, green);
										ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, green_alt);
										break;
									case 2:
										ImGui.PushStyleColor(ImGuiCol.TableRowBg, cyan);
										ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, cyan_alt);
										break;
									case 3:
										ImGui.PushStyleColor(ImGuiCol.TableRowBg, red);
										ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, red_alt);
										break;
									case 4:
										ImGui.PushStyleColor(ImGuiCol.TableRowBg, blue);
										ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, blue_alt);
										break;
								}
								ImGui.Text(info.territory_name_ori);
							},
							info => ImGui.Text(info.oriname), info => ImGui.Text(info.hp.ToString()), info => ImGui.Text(getDeltaMin(info.appeared_at).ToString()), info => { ImGui.Text(getDeltaMin(info.defeated_at).ToString()); }
						};
						ImGui.Text("活着的");
						var data = WebSocket.nmalive.OrderBy(a => a.territory_id).ThenBy(a => getDeltaMin(a.defeated_at)).ToArray();
						NewTable(["地点", "名称", "血量", "触发时间(min)", "击杀时间(min)"], data, acts);
						ImGui.PopStyleColor(2 * data.Length);
						ImGui.Text("已死亡");
						var data2 = WebSocket.nmdead.OrderBy(a => a.territory_id).ThenBy(a => getDeltaMin(a.defeated_at)).ToArray();
						NewTable(["地点", "名称", "血量", "触发时间(min)", "击杀时间(min)"], data2, acts);
						ImGui.PopStyleColor(2 * data2.Length);
					} catch (Exception e) {
						Log.Error(e.ToString());
					}
				} else WebSocket.StopWss();
			});
			NewTab("Fate", DrawFate);
			NewTab("改名", DrawNameReplacement);
			if (HasCore()) {
				NewTab("Tp", DrawTp);
				NewTab("深宫", DrawPalacePal);
			}
		}
	}


	private static int getDeltaMin(string d) {
		try {
			return (int)new TimeSpan(getT(d)).TotalMinutes;
		} catch {
			return 0;
		}
	}
}