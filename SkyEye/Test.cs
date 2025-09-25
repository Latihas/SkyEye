// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Numerics;
// using System.Text.RegularExpressions;
// using System.Threading;
// using System.Threading.Tasks;
// using Dalamud.Game;
// using Dalamud.Game.ClientState.Objects.SubKinds;
// using Dalamud.Game.Command;
// using Dalamud.Game.Text;
// using Dalamud.Game.Text.SeStringHandling;
// using Dalamud.Interface.Windowing;
// using Dalamud.IoC;
// using Dalamud.Plugin;
// using Dalamud.Plugin.Services;
// using FFXIVClientStructs.FFXIV.Client.Game;
// using FFXIVClientStructs.FFXIV.Client.Game.Control;
// using SkyEye.SkyEye;
//
// namespace manbo;
//
// public sealed class Plugin : IDalamudPlugin {
// 	private static float _lSpeed = 1f;
// 	
// 	// Static reference to the current plugin instance
// 	internal static Plugin Instance { get; private set; }
//
// 	private static SpeedHackPlugin _shp;
// 	private readonly Lock _speedLock = new();
// 	public readonly WindowSystem WindowSystem = new("哈基米曼波");
// internal List<Vector3> DetectedTreasurePositions = [];
// private float _dspeed = 1f;
// internal readonly List<IPlayerCharacter> OtherPlayer = [];
//
// // 添加调试窗口
// private DebugWindow DebugWindow;
//
//
// 	public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager) {
// 		Instance = this; // Store instance reference
// 		PluginInterface = pluginInterface;
// 		CommandManager = commandManager;
// 		Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
// 		Configuration.Initialize(PluginInterface);
// 		PluginInterface.UiBuilder.Draw += DrawUi;
// 		ConfigWindow = new ConfigWindow(this);
// 		DebugWindow = new DebugWindow(this);
// 		WindowSystem.AddWindow(ConfigWindow);
// 		WindowSystem.AddWindow(DebugWindow);
// 		CommandManager.AddHandler("/manbo", new CommandInfo(OnCommand) {
// 			HelpMessage = "打开主界面"
// 		});
// 		CommandManager.AddHandler("/manbodebug", new CommandInfo(OnDebugCommand) {
// 			HelpMessage = "打开调试界面"
// 		});
// 		Framework.Update += UpdateRoundPlayers;
// 		PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
// 		ChatGui.ChatMessageUnhandled += OnChatMessage;
// 		
// 		// 初始化幸运胡萝卜定时器
// 		InitializeCarrotTimer();
// 		
// 		// 初始化导航网格IPC接口
// 		NavmeshIpc.Init();
// 		
// 		// 如果启用了自动兔子导航功能，尝试加载导航网格
// 		if (Configuration.Overlay2DAutoRabbit)
// 		{
// 			Task.Run(async () => {
// 				try
// 				{
// 					// 等待一段时间，确保插件完全加载
// 					await Task.Delay(3000);
// 					Log.Information("检查导航网格状态...");
// 					
// 					// 确保IPC接口已初始化
// 					NavmeshIpc.Init();
// 					
// 					// 如果导航网格未就绪，尝试重新加载
// 					if (NavmeshIpc.IsEnabled && !NavmeshIpc.IsReady())
// 					{
// 						Log.Information("导航网格未就绪，正在尝试加载导航网格...");
// 						NavmeshIpc.Reload();
// 						
// 						// 再次等待一段时间，让导航网格有时间加载
// 						await Task.Delay(2000);
// 						
// 						// 如果仍未就绪，尝试重建
// 						if (!NavmeshIpc.IsReady())
// 						{
// 							Log.Information("导航网格加载失败，正在尝试重建导航网格...");
// 							NavmeshIpc.Rebuild();
// 							
// 							// 等待重建完成
// 							await Task.Delay(3000);
// 							
// 							// 再次检查
// 							if (NavmeshIpc.IsReady())
// 							{
// 								Log.Information("导航网格已成功就绪，可以使用自动导航功能");
// 							}
// 							else
// 							{
// 								Log.Error("导航网格初始化失败，自动导航功能可能无法正常使用");
// 							}
// 						}
// 						else
// 						{
// 							Log.Information("导航网格已成功加载，可以使用自动导航功能");
// 						}
// 					}
// 					else if (NavmeshIpc.IsEnabled && NavmeshIpc.IsReady())
// 					{
// 						Log.Information("导航网格已就绪，可以使用自动导航功能");
// 					}
// 				}
// 				catch (Exception ex)
// 				{
// 					Log.Error($"初始化导航网格时发生错误: {ex.Message}");
// 				}
// 			});
// 		}
// 	}
//
// 	internal IDalamudPluginInterface PluginInterface { get; init; }
//
// 	private ICommandManager CommandManager { get; }
//
// 	public static Configuration Configuration { get; private set; }
//
// 	[PluginService] public static IClientState ClientState { get; set; }
//
// 	[PluginService] internal static IDataManager DataManager { get; set; }
//
// 	[PluginService] internal static IPluginLog Log { get; set; }
//
// 	[PluginService] internal static ICondition Condition { get; set; }
//
// 	[PluginService] internal static IGameGui Gui { get; set; }
//
// 	[PluginService] internal static IObjectTable Objects { get; set; }
//
// 	[PluginService] internal static IFateTable Fates { get; set; }
//
// 	[PluginService] internal static IFramework Framework { get; set; } = null;
//
// 	[PluginService] internal static ISigScanner SigScanner { get; set; } = null;
//
// 	[PluginService] internal static IChatGui ChatGui { get; set; } = null;
//
//
// 	private ConfigWindow ConfigWindow { get; }
//
// 	public void Dispose() {
// 		WindowSystem.RemoveAllWindows();
// 		ChatGui.ChatMessageUnhandled -= OnChatMessage;
// 		ConfigWindow.Dispose();
// 		Framework.Update -= UpdateRoundPlayers;
// 		_shp?.Dispose();
// 		CommandManager.RemoveHandler("/manbo");
// 		StopCarrotTimer();
// 		Instance = null; // Clean up instance reference
// 	}
//
// 	private void OnCommand(string command, string args) {
// 		ToggleConfigUi();
// 	}
//
// 	private void OnDebugCommand(string command, string args) {
// 		ToggleDebugUi();
// 	}
//
// 	public void ToggleDebugUi() {
// 		DebugWindow.IsOpen = !DebugWindow.IsOpen;
// 	}
//
// 	// 幸运胡萝卜物品ID
// 	private const uint LuckyCarrotItemId = 2002482;
// 	// 幸运胡萝卜CD时间（秒）
// 	private const int LuckyCarrotCooldown = 7;
// 	// 自动使用幸运胡萝卜的定时器
// 	private System.Timers.Timer carrotTimer;
// 	
// 	private void OnChatMessage(XivChatType type, int timestamp, SeString sender, SeString message)
// 	{
// 		if (ClientState.LocalPlayer == null || message == null || ClientState.TerritoryType != 732 && ClientState.TerritoryType != 763 && ClientState.TerritoryType != 795 && ClientState.TerritoryType != 827) return;
// 		var msg = message.TextValue.Trim();
// 		
// 		// 检测到找到财宝的消息
// 		if (msg.Contains("发现了财宝！！") || msg.StartsWith("找到了财宝")) 
// 		{
// 			DetectedTreasurePositions = [];
// 			
// 			// 停止使用幸运胡萝卜
// 			Configuration.ShouldUseCarrot = false;
// 			StopCarrotTimer();
// 			Log.Information("已找到财宝，停止使用幸运胡萝卜");
// 			
// 			// 如果启用了返回初始位置功能，且有缓存的初始位置，则返回初始位置
// 			if (Configuration.ReturnToStartPosition && Configuration.LastPlayerPosition != Vector3.Zero)
// 			{
// 				Log.Information("已找到财宝，正在返回初始位置...");
// 				NavigateToPosition(Configuration.LastPlayerPosition, "初始位置");
// 				// 清除缓存的初始位置
// 				Configuration.LastPlayerPosition = Vector3.Zero;
// 			}
// 			
// 			return;
// 		}
// 		
// 		// 检测是否可以使用幸运胡萝卜的消息
// 		if (msg.Contains("可以使用任务道具\"幸运胡萝卜\"，请它帮忙寻找财宝！"))
// 		{
// 			if (Configuration.AutoUseCarrot)
// 			{
// 				Configuration.ShouldUseCarrot = true;
// 				Configuration.IsVeryClose = false;
// 				Log.Information("检测到可以使用幸运胡萝卜的消息，开始自动使用幸运胡萝卜");
// 				StartCarrotTimer();
// 			}
// 			return;
// 		}
// 		
// 		// 检测"很近"的消息
// 		if (msg.Contains("财宝就在附近") || msg.Contains("很近"))
// 		{
// 			if (Configuration.AutoUseCarrot && Configuration.ShouldUseCarrot)
// 			{
// 				Configuration.IsVeryClose = true;
// 				Log.Information("检测到财宝很近的消息，将在当前位置等待CD后继续使用幸运胡萝卜");
// 			}
// 			return;
// 		}
// 		
// 		if (!msg.StartsWith("财宝好像是在")) return;
// 		var result = Regex.Match(msg, "财宝好像是在(?<direction>正北|东北|正东|东南|正南|西南|正西|西北)方向(?<distance>(很远|稍远|不远|很近))的地方！");
// 		if (!result.Success) return;
// 		var direction = result.Groups["direction"].Value;
// 		int minDistance;
// 		int maxDistance;
// 		
// 		// 检测是否是"很近"状态
// 		if (result.Groups["distance"].Value == "很近")
// 		{
// 			if (Configuration.AutoUseCarrot && Configuration.ShouldUseCarrot)
// 			{
// 				Configuration.IsVeryClose = true;
// 				Log.Information("检测到财宝很近的消息，将在当前位置等待CD后继续使用幸运胡萝卜");
// 			}
// 		}
// 		
// 		// 缓存当前方向
// 		Configuration.LastTreasureDirection = direction;
// 		
// 		// 如果是第一次导航，缓存玩家初始位置
// 		if (Configuration.LastPlayerPosition == Vector3.Zero)
// 		{
// 			Configuration.LastPlayerPosition = ClientState.LocalPlayer.Position;
// 			Log.Information("已缓存玩家初始位置: " + Configuration.LastPlayerPosition);
// 		}
// 		
// 		switch (result.Groups["distance"].Value)
// 		{
// 			case "很远":
// 				minDistance = 200;
// 				maxDistance = int.MaxValue;
// 				break;
// 			case "稍远":
// 				minDistance = 100;
// 				maxDistance = 200;
// 				break;
// 			case "不远":
// 				minDistance = 25;
// 				maxDistance = 100;
// 				break;
// 			default:
// 				minDistance = 0;
// 				maxDistance = 25;
// 				break;
// 		}
// 		var playerPos = ClientState.LocalPlayer.Position;
// 		
// 		// 根据方向筛选财宝点位
// 		var treasures = from c in PData.RabbitTreasurePositions[ClientState.TerritoryType].Where(delegate (Vector3 c)
// 		{
// 			var num = Vector3.Distance(playerPos, c);
// 			bool inDistanceRange = num >= minDistance && num <= maxDistance;
// 			
// 			// 根据方向进行筛选
// 			bool inDirectionRange = IsPositionInDirection(playerPos, c, direction);
// 			
// 			return inDistanceRange && inDirectionRange;
// 		})
// 			orderby Vector3.Distance(playerPos, c)
//
//             select c;
//
// 		// 如果启用了自动导航功能，则导航到可能的财宝位置
// 		if (Configuration.Overlay2DAutoRabbit)
// 		{
// 			var treasureList = treasures.ToList();
// 			DetectedTreasurePositions = treasureList; // 更新检测到的财宝位置列表（用于UI显示）
// 			
// 			if (treasureList.Count > 0)
// 			{
// 				// 根据配置决定是否随机选择财宝位置点
// 				Vector3 selectedPosition;
// 				if (Configuration.Overlay2DAutoRabbitRandom && treasureList.Count > 1)
// 				{
// 					// 随机选择一个位置点开始导航
// 					Random random = new Random();
// 					int randomIndex = random.Next(0, treasureList.Count);
// 					selectedPosition = treasureList[randomIndex];
// 					
// 					Log.Information($"从 {treasureList.Count} 个可能的财宝位置中随机选择第 {randomIndex + 1} 个位置进行导航（方向：{direction}）");
// 				}
// 				else
// 				{
// 					// 使用第一个位置点
// 					selectedPosition = treasureList[0];
// 					Log.Information($"使用第一个财宝位置点进行导航（共 {treasureList.Count} 个可能位置，方向：{direction}）");
// 				}
// 				
// 				NavigateToRabbitTreasure(selectedPosition);
// 			}
// 			else
// 			{
// 				Log.Warning($"未找到符合条件的财宝位置，无法进行导航（方向：{direction}）");
// 			}
// 		}
//     }
//     
//     /// <summary>
//     /// 判断目标位置是否在指定方向上
//     /// </summary>
//     /// <param name="playerPos">玩家位置</param>
//     /// <param name="targetPos">目标位置</param>
//     /// <param name="direction">方向描述（正北、东北等）</param>
//     /// <returns>如果目标位置在指定方向上，则返回true</returns>
//     private bool IsPositionInDirection(Vector3 playerPos, Vector3 targetPos, string direction)
//     {
//         // 计算目标位置相对于玩家位置的方向向量
//         Vector3 directionVector = new Vector3(targetPos.X - playerPos.X, 0, targetPos.Z - playerPos.Z);
//         
//         // 计算方向角度（弧度）
//         float angle = (float)Math.Atan2(directionVector.Z, directionVector.X);
//         
//         // 将弧度转换为角度（0-360度），注意游戏中0度是正东方向，顺时针旋转
//         float degrees = (float)(angle * (180.0 / Math.PI));
//         if (degrees < 0) degrees += 360;
//         
//         // 记录调试信息
//         Plugin.Log?.Debug($"目标位置方向角度: {degrees}度, 指定方向: {direction}");
//         
//         // 根据方向描述确定角度范围
//         // 根据用户反馈调整角度范围
//         bool isInDirection = direction switch
//         {
//             "正北" => (degrees > 247.5 && degrees <= 292.5),   // 北方对应270度
//             "东北" => (degrees > 292.5 && degrees <= 337.5),   // 东北方向是315度
//             "正东" => (degrees > 337.5 || degrees <= 22.5),    // 东方对应0度或360度
//             "东南" => (degrees > 22.5 && degrees <= 67.5),     // 东南方向是45度
//             "正南" => (degrees > 67.5 && degrees <= 112.5),    // 南方对应90度
//             "西南" => (degrees > 112.5 && degrees <= 157.5),   // 西南方向是135度
//             "正西" => (degrees > 157.5 && degrees <= 202.5),   // 西方对应180度
//             "西北" => (degrees > 202.5 && degrees <= 247.5),   // 西北方向是225度
//             _ => true // 如果方向不明确，则不进行筛选
//         };
//         
//         // 添加更详细的调试日志
//         Plugin.Log?.Debug($"方向判断结果: {isInDirection}, 角度: {degrees}度, 方向: {direction}");
//         
//         return isInDirection;
//     }
//
// 	/// <summary>
// 	/// 导航到兔子财宝位置
// 	/// </summary>
// 	/// <param name="position">财宝的位置坐标</param>
// 	private void NavigateToRabbitTreasure(Vector3 position)
// 	{
// 		// 调用通用导航方法
// 		NavigateToPosition(position, "财宝");
// 	}
// 	
// 	/// <summary>
// 	/// 导航到手动输入的位置（供配置窗口调用）
// 	/// </summary>
// 	/// <param name="position">目标位置坐标</param>
// 	public void NavigateToManualPosition(Vector3 position)
// 	{
// 		// 调用通用导航方法
// 		NavigateToPosition(position, "手动输入");
// 	}
// 	
// 	/// <summary>
// 	/// 通用导航方法 - 使用SimpleMove.PathfindAndMoveTo直接寻路并移动
// 	/// </summary>
// 	/// <param name="position">目标位置坐标</param>
// 	/// <param name="positionType">位置类型描述（用于日志）</param>
// 	private void NavigateToPosition(Vector3 position, string positionType)
// 	{
// 		try
// 		{
// 			if (!NavmeshIpc.IsEnabled)
// 			{
// 				Log.Error("vnavmesh插件未启用，无法进行自动导航");
// 				return;
// 			}
//
// 			// 检查导航网格是否准备就绪，不重新加载或重建
// 			if (!NavmeshIpc.IsReady())
// 			{
// 				Log.Warning("导航网格未准备就绪，请在配置窗口中手动初始化或重建导航网格");
// 				return;
// 			}
// 			
// 			// 如果是导航到财宝位置，且启用了自动使用幸运胡萝卜功能，则开始使用幸运胡萝卜
// 			if (positionType == "财宝" && Configuration.AutoUseCarrot && Configuration.ShouldUseCarrot)
// 			{
// 				// 如果不是很近状态，则在导航时使用幸运胡萝卜
// 				if (!Configuration.IsVeryClose)
// 				{
// 					Log.Information("导航到财宝位置，开始使用幸运胡萝卜");
// 					StartCarrotTimer();
// 				}
// 			}
//
// 			// 停止当前的导航（如果有）
// 			if (NavmeshIpc.PathIsRunning())
// 			{
// 				NavmeshIpc.PathStop();
// 			}
//
// 			// 获取玩家当前位置
// 			var playerPos = ClientState.LocalPlayer.Position;
//
// 			// 判断当前地图是否可以飞行
// 			// 首先检查用户配置是否启用了飞行模式
// 			bool canFly = Configuration.EnableFlyingMode;
// 			
// 			// 如果用户启用了飞行模式，还需要检查当前地图是否支持飞行
// 			if (canFly)
// 			{
// 				// 兔子财宝地图ID: 732(拉诺西亚), 763(黑衣森林), 795(萨纳兰), 827(摩杜纳)
// 				// 这些地图支持飞行，其他地图可能不支持
// 				int[] flyableMapIds = { 732, 763, 795, 827 };
// 				if (!flyableMapIds.Contains(ClientState.TerritoryType))
// 				{
// 					// 如果当前地图不在支持飞行的列表中，强制禁用飞行模式
// 					canFly = false;
// 					Log.Warning("当前地图不支持飞行，已自动禁用飞行模式");
// 				}
// 			}
//
// 			// 使用SimpleMove.PathfindAndMoveTo直接寻路并移动
// 			try
// 			{
// 				// 使用vnavmesh的PathfindAndMoveTo方法，该方法会自动处理寻路和移动
// 				// 根据地图类型决定是否启用飞行模式
// 				Log.Information($"正在尝试导航到{positionType}位置，飞行模式: {(canFly ? "启用" : "禁用")}");
// 				bool success = NavmeshIpc.PathfindAndMoveTo(position, canFly);
// 				if (success)
// 				{
// 					Log.Information($"正在导航到{positionType}位置: {position}");
// 				}
// 				else
// 				{
// 					// 如果启用飞行模式失败，尝试禁用飞行模式再次尝试
// 					if (canFly)
// 					{
// 						Log.Warning("启用飞行模式导航失败，尝试禁用飞行模式重新导航");
// 						success = NavmeshIpc.PathfindAndMoveTo(position, false);
// 						if (success)
// 						{
// 							Log.Information($"禁用飞行模式后成功导航到{positionType}位置: {position}");
// 						}
// 						else
// 						{
// 							Log.Error($"无法导航到{positionType}位置，即使禁用飞行模式也失败");
// 						}
// 					}
// 					else
// 					{
// 						Log.Error($"无法导航到{positionType}位置，PathfindAndMoveTo返回失败");
// 					}
// 				}
// 			}
// 			catch (Exception ex)
// 			{
// 				Log.Error($"导航到{positionType}位置过程中发生错误: {ex.Message}");
// 			}
// 		}
// 		catch (Exception ex)
// 		{
// 			Log.Error($"导航到{positionType}位置时出错: {ex.Message}");
// 		}
// 	}
//
//
//     private void UpdateRoundPlayers(IFramework framework) {
// 		if (ClientState.LocalPlayer == null) return;
// 		if (ClientState.TerritoryType != 732 && ClientState.TerritoryType != 763 && ClientState.TerritoryType != 795 && ClientState.TerritoryType != 827 || !Configuration.Overlay2DEnabled) {
// 			var ts = Configuration.Overlay2DSpeedUpTerritory.Split('|');
// 			if (!ts.Contains(ClientState.TerritoryType.ToString())) {
// 				lock (_speedLock) SetSpeed(1f);
// 				return;
// 			}
// 		}
// 		lock (OtherPlayer) {
// 			OtherPlayer.Clear();
// 			if (Objects == null) return;
// 			foreach (var obj in Objects)
// 				try {
// 					if (obj != null && obj.GameObjectId != ClientState.LocalPlayer.GameObjectId & obj.Address.ToInt64() != 0 && obj is IPlayerCharacter rcTemp) OtherPlayer.Add(rcTemp);
// 				}
// 				catch (Exception) {
// 					Log.Error("error");
// 				}
// 		}
// 		lock (_speedLock) {
// 			if (Configuration.Overlay2DSpeedUpEnabled) {
// 				var friends = Configuration.Overlay2DSpeedUpFriendly.Split('|');
// 				_dspeed = OtherPlayer.Any(i => !friends.Contains(i.Name.ToString()) && Vector3.Distance(i.Position, ClientState.LocalPlayer.Position) < (110 ^ 2)) ? 1f : Configuration.Overlay2DSpeedUpN;
// 			}
// 			else _dspeed = 1f;
// 			SetSpeed(_dspeed);
// 		}
// 	}
//
// 	public static void SetSpeed(float speedBase) {
// 		if (_lSpeed == speedBase) return;
// 		_lSpeed = speedBase;
// 		_shp ??= new SpeedHackPlugin();
// 		_shp.SetSpeedMultiplier(speedBase);
// 	}
//
// 	private void DrawUi() {
// 		WindowSystem.Draw();
// 	}
//
// 	private void ToggleConfigUi() {
// 		ConfigWindow.Toggle();
// 	}
// 	
// 	// 初始化幸运胡萝卜定时器
// 	private void InitializeCarrotTimer() {
// 		carrotTimer = new System.Timers.Timer(LuckyCarrotCooldown * 1000);
// 		carrotTimer.AutoReset = true;
// 		carrotTimer.Elapsed += (sender, e) => {
// 			if (Configuration.ShouldUseCarrot) {
// 				UseCarrot();
// 			} else {
// 				StopCarrotTimer();
// 			}
// 		};
// 	}
// 	
// 	// 开始使用幸运胡萝卜定时器
// 	private void StartCarrotTimer() {
// 		if (carrotTimer == null) {
// 			InitializeCarrotTimer();
// 		}
// 		
// 		// 立即使用一次，然后开始定时器
// 		UseCarrot();
// 		
// 		if (!carrotTimer.Enabled) {
// 			carrotTimer.Start();
// 			Log.Information("开始自动使用幸运胡萝卜，间隔：" + LuckyCarrotCooldown + "秒");
// 		}
// 	}
// 	
// 	// 停止使用幸运胡萝卜定时器
// 	private void StopCarrotTimer() {
// 		if (carrotTimer != null && carrotTimer.Enabled) {
// 			carrotTimer.Stop();
// 			Log.Information("停止自动使用幸运胡萝卜");
// 		}
// 	}
// 	
// 	// 使用幸运胡萝卜
// 	private void UseCarrot() {
// 		try {
// 			// 确保玩家存在且在兔子地图中
// 			if (ClientState.LocalPlayer == null || (ClientState.TerritoryType != 732 && ClientState.TerritoryType != 763 && ClientState.TerritoryType != 795 && ClientState.TerritoryType != 827)) {
// 				StopCarrotTimer();
// 				return;
// 			}
// 			
// 			// 使用物品
// 			unsafe {
// 				var inventoryManager = InventoryManager.Instance();
// 				if (inventoryManager != null) {
// 					// 检查是否有幸运胡萝卜
// 					var itemCount = inventoryManager->GetInventoryItemCount(LuckyCarrotItemId);
// 					if (itemCount > 0) {
// 						Log.Information("使用幸运胡萝卜");
// 						// 使用ActionManager来使用物品
// 						var actionManager = ActionManager.Instance();
// 						if (actionManager != null) {
// 							actionManager->UseAction(ActionType.Item, LuckyCarrotItemId);
// 						}
// 					} else {
// 						Log.Warning("没有幸运胡萝卜可用，停止自动使用");
// 						Configuration.ShouldUseCarrot = false;
// 						StopCarrotTimer();
// 					}
// 				}
// 			}
// 		} catch (Exception ex) {
// 			Log.Error($"使用幸运胡萝卜时出错: {ex.Message}");
// 		}
// 	}
// }