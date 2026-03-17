using System;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Ipc;

namespace SkyEye.SkyEye;

internal static class NavmeshIpc {
	private const string Name = "vnavmesh";
	private static bool _hasLoggedInitSuccess, _hasLoggedPluginNotFound;
	private static ICallGateSubscriber<bool>? _navIsReady, _pathIsRunning;
	private static ICallGateSubscriber<object>? _pathStop;
	private static ICallGateSubscriber<Vector3, bool, bool>? _pathfindAndMoveTo;
	private static ICallGateSubscriber<Vector3>? _flagToPoint;
	private static bool IsEnabled => IsPluginLoaded();

	private static bool IsPluginLoaded() {
		try {
			return Plugin.PluginInterface.InstalledPlugins.Any(p => p is { Name: Name, IsLoaded: true });
		}
		catch (Exception ex) {
			Plugin.Log.Error($"检查插件加载状态时发生错误: {ex.Message}");
			return false;
		}
	}

	internal static void Init() {
		if (IsPluginLoaded()) {
			try {
				var pi = Plugin.PluginInterface;
				_navIsReady = pi.GetIpcSubscriber<bool>($"{Name}.Nav.IsReady"); // 检查导航网格是否已准备就绪
				// pi.GetIpcSubscriber<float>($"{Name}.Nav.BuildProgress"); // 获取导航网格构建进度（0.0-1.0）
				_pathStop = pi.GetIpcSubscriber<object>($"{Name}.Path.Stop"); // 停止当前移动
				_pathIsRunning = pi.GetIpcSubscriber<bool>($"{Name}.Path.IsRunning"); // 检查是否正在移动中
				_pathfindAndMoveTo = pi.GetIpcSubscriber<Vector3, bool, bool>($"{Name}.SimpleMove.PathfindAndMoveTo"); // 计算路径并移动到目标位置（组合了寻路和移动功能）
				_flagToPoint = pi.GetIpcSubscriber<Vector3>($"{Name}.Query.Mesh.FlagToPoint"); // 计算路径并移动到目标位置（组合了寻路和移动功能）
				// pi.GetIpcSubscriber<bool>($"{Name}.SimpleMove.PathfindInProgress"); // 检查寻路过程是否正在进行中
				if (_hasLoggedInitSuccess) return;
				Plugin.Log.Information("NavmeshIPC初始化成功");
				_hasLoggedInitSuccess = true;
			}
			catch (Exception ex) {
				Plugin.Log.Error($"NavmeshIPC初始化失败: {ex}");
			}
		}
		else {
			if (_hasLoggedPluginNotFound) return;
			Plugin.Log.Warning($"未找到 {Name} 插件，导航功能不可用");
			_hasLoggedPluginNotFound = true;
		}
	}

	private static T? Execute<T>(Func<T> func) {
		if (!IsPluginLoaded()) return default;
		try {
			return func();
		}
		catch (Exception ex) {
			Plugin.Log.Error($"IPC执行错误: {ex}");
		}
		return default;
	}

	private static void Execute(Action action) {
		if (!IsPluginLoaded()) return;
		try {
			action();
		}
		catch (Exception ex) {
			Plugin.Log.Error($"IPC执行错误: {ex}");
		}
	}

	internal static bool IsReady() {
		var result = Execute(() => _navIsReady?.InvokeFunc());
		return result.HasValue && result.Value;
	}

	internal static void Stop() {
		Execute(() => _pathStop?.InvokeAction());
	}

	internal static bool IsRunning() {
		var result = Execute(() => _pathIsRunning?.InvokeFunc());
		return result.HasValue && result.Value;
	}

	internal static Vector3? FlagToPoint() {
		return Execute(() => _flagToPoint?.InvokeFunc());
	}

	private static ICallGateSubscriber<Vector3, bool>? _divetp;


	internal static void PathfindAndMoveTo(Vector3 pos, bool fly) {
		if (Plugin.PluginInterface.InstalledPlugins.Any(p => p is { Name: "LatihasDalamudCore", IsLoaded: true })) {
			_divetp ??= Plugin.PluginInterface.GetIpcSubscriber<Vector3, bool>("LatihasDalamudCore.DiveTp");
			_divetp.InvokeAction(pos);
			return;
		}
		if (!IsEnabled || !IsReady()) {
			Plugin.Log.Error("vnavmesh插件异常，尝试重新初始化");
			Init();
			return;
		}
		Execute(() => _pathfindAndMoveTo?.InvokeFunc(pos, fly));
	}
}