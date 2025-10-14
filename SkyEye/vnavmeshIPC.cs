using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Ipc;

namespace SkyEye.SkyEye;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "NotAccessedField.Local")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
internal static class NavmeshIpc {
    // 静态变量，用于跟踪是否已经输出过日志
    private static bool _hasLoggedInitSuccess;
    private static bool _hasLoggedPluginNotFound;

    // vnavmesh插件的名称
    internal static readonly string Name = "vnavmesh";
    // 导航网格相关的IPC接口
    // 检查导航网格是否已准备就绪
    private static ICallGateSubscriber<bool>? _navIsReady;
    // 获取导航网格构建进度（0.0-1.0）
    private static ICallGateSubscriber<float>? _navBuildProgress;
    // 重新加载导航网格
    private static ICallGateSubscriber<bool>? _navReload;
    // 重新构建导航网格
    private static ICallGateSubscriber<bool>? _navRebuild;
    // 寻路功能：从起点到终点计算路径，返回路径点列表
    private static ICallGateSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>>? _navPathfind;
    // 检查是否启用了自动加载导航网格
    private static ICallGateSubscriber<bool>? _navIsAutoLoad;
    // 设置是否自动加载导航网格
    private static ICallGateSubscriber<bool, object>? _navSetAutoLoad;

    // 导航网格查询相关的IPC接口
    // 查询给定位置附近的最近导航网格点
    private static ICallGateSubscriber<Vector3, float, float, Vector3?>? _queryMeshNearestPoint;
    // 查询给定位置下方的地面点
    private static ICallGateSubscriber<Vector3, bool, float, Vector3?>? _queryMeshPointOnFloor;
    // 查询给定位置的最远可达点
    private static ICallGateSubscriber<Vector3, float, float, float, Vector3?>? _queryMeshFurthestPoint;

    // 路径移动相关的IPC接口
    // 沿指定路径点移动角色
    private static ICallGateSubscriber<List<Vector3>, bool, object>? _pathMoveTo;
    // 停止当前移动
    private static ICallGateSubscriber<object>? _pathStop;
    // 检查是否正在移动中
    private static ICallGateSubscriber<bool>? _pathIsRunning;
    // 获取当前路径中的路径点数量
    private static ICallGateSubscriber<int>? _pathNumWaypoints;
    // 获取是否允许移动
    private static ICallGateSubscriber<bool>? _pathGetMovementAllowed;
    // 设置是否允许移动
    private static ICallGateSubscriber<bool, object>? _pathSetMovementAllowed;
    // 获取是否在移动时对齐相机
    private static ICallGateSubscriber<bool>? _pathGetAlignCamera;
    // 设置是否在移动时对齐相机
    private static ICallGateSubscriber<bool, object>? _pathSetAlignCamera;
    // 获取路径点到达容差
    private static ICallGateSubscriber<float>? _pathGetTolerance;
    // 设置路径点到达容差
    private static ICallGateSubscriber<float, object>? _pathSetTolerance;

    // 简单移动相关的IPC接口
    // 计算路径并移动到目标位置（组合了寻路和移动功能）
    private static ICallGateSubscriber<Vector3, bool, bool>? _pathfindAndMoveTo;
    // 检查寻路过程是否正在进行中
    private static ICallGateSubscriber<bool>? _pathfindInProgress;
    // 检查vnavmesh插件是否已安装并启用
    internal static bool IsEnabled => IsPluginLoaded();

    /// <summary>
    ///     检查插件是否已加载
    /// </summary>
    /// <returns>如果插件已加载，则返回true</returns>
    private static bool IsPluginLoaded() {
        try {
            return Plugin.PluginInterface.InstalledPlugins.Any(p => p.Name == Name && p.IsLoaded);
        }
        catch (Exception ex) {
            Plugin.Log.Error($"检查插件加载状态时发生错误: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     初始化所有IPC接口
    /// </summary>
    internal static void Init() {
        // 检查vnavmesh插件是否已安装
        if (IsPluginLoaded()) {
            try {
                var pi = Plugin.PluginInterface;
                _navIsReady = pi.GetIpcSubscriber<bool>($"{Name}.Nav.IsReady"); // 检查导航网格是否已准备就绪
                _navBuildProgress = pi.GetIpcSubscriber<float>($"{Name}.Nav.BuildProgress"); // 获取导航网格构建进度（0.0-1.0）
                _navReload = pi.GetIpcSubscriber<bool>($"{Name}.Nav.Reload"); // 重新加载导航网格
                _navRebuild = pi.GetIpcSubscriber<bool>($"{Name}.Nav.Rebuild"); // 重新构建导航网格
                _navPathfind = pi.GetIpcSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>>($"{Name}.Nav.Pathfind"); // 计算路径并移动到目标位置（组合了寻路和移动功能）
                _navIsAutoLoad = pi.GetIpcSubscriber<bool>($"{Name}.Nav.IsAutoLoad"); // 检查是否启用了自动加载导航网格
                _navSetAutoLoad = pi.GetIpcSubscriber<bool, object>($"{Name}.Nav.SetAutoLoad"); // 设置是否自动加载导航网格

                _queryMeshNearestPoint = pi.GetIpcSubscriber<Vector3, float, float, Vector3?>($"{Name}.Query.Mesh.NearestPoint"); // 查询给定位置附近的最近导航网格点
                _queryMeshPointOnFloor = pi.GetIpcSubscriber<Vector3, bool, float, Vector3?>($"{Name}.Query.Mesh.PointOnFloor"); // 查询给定位置下方的地面点
                _queryMeshFurthestPoint = pi.GetIpcSubscriber<Vector3, float, float, float, Vector3?>($"{Name}.Query.Mesh.FurthestPoint"); // 查询给定位置的最远可达点

                _pathMoveTo = pi.GetIpcSubscriber<List<Vector3>, bool, object>($"{Name}.Path.MoveTo"); // 沿指定路径点移动角色
                _pathStop = pi.GetIpcSubscriber<object>($"{Name}.Path.Stop"); // 停止当前移动
                _pathIsRunning = pi.GetIpcSubscriber<bool>($"{Name}.Path.IsRunning"); // 检查是否正在移动中
                _pathNumWaypoints = pi.GetIpcSubscriber<int>($"{Name}.Path.NumWaypoints"); // 获取当前路径中的路径点数量
                _pathGetMovementAllowed = pi.GetIpcSubscriber<bool>($"{Name}.Path.GetMovementAllowed"); // 获取是否允许移动
                _pathSetMovementAllowed = pi.GetIpcSubscriber<bool, object>($"{Name}.Path.SetMovementAllowed"); // 设置是否允许移动
                _pathGetAlignCamera = pi.GetIpcSubscriber<bool>($"{Name}.Path.GetAlignCamera"); // 获取是否在移动时对齐相机
                _pathSetAlignCamera = pi.GetIpcSubscriber<bool, object>($"{Name}.Path.SetAlignCamera"); // 设置是否在移动时对齐相机
                _pathGetTolerance = pi.GetIpcSubscriber<float>($"{Name}.Path.GetTolerance"); // 获取路径点到达容差
                _pathSetTolerance = pi.GetIpcSubscriber<float, object>($"{Name}.Path.SetTolerance"); // 设置路径点到达容差

                _pathfindAndMoveTo = pi.GetIpcSubscriber<Vector3, bool, bool>($"{Name}.SimpleMove.PathfindAndMoveTo"); // 计算路径并移动到目标位置（组合了寻路和移动功能）
                _pathfindInProgress = pi.GetIpcSubscriber<bool>($"{Name}.SimpleMove.PathfindInProgress"); // 检查寻路过程是否正在进行中

                // 使用静态变量跟踪是否已经输出过初始化成功的日志，避免重复输出
                if (_hasLoggedInitSuccess) return;
                Plugin.Log.Information("NavmeshIPC初始化成功");
                _hasLoggedInitSuccess = true;
            }
            catch (Exception ex) {
                Plugin.Log.Error($"NavmeshIPC初始化失败: {ex}");
            }
        }
        else {
            // 使用静态变量跟踪是否已经输出过未找到插件的警告，避免重复输出
            if (_hasLoggedPluginNotFound) return;
            Plugin.Log.Warning($"未找到 {Name} 插件，导航功能不可用");
            _hasLoggedPluginNotFound = true;
        }
    }

    /// <summary>
    ///     执行IPC函数并返回结果，处理可能的异常
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="func">要执行的函数</param>
    /// <returns>函数执行结果，如果执行失败则返回默认值</returns>
    internal static T? Execute<T>(Func<T> func) {
        if (IsPluginLoaded()) {
            try {
                return func();
            }
            catch (Exception ex) {
                Plugin.Log.Error($"IPC执行错误: {ex}");
            }
        }

        return default;
    }

    /// <summary>
    ///     执行无返回值的IPC方法，处理可能的异常
    /// </summary>
    /// <param name="action">要执行的方法</param>
    internal static void Execute(Action action) {
        if (IsPluginLoaded()) {
            try {
                action();
            }
            catch (Exception ex) {
                Plugin.Log.Error($"IPC执行错误: {ex}");
            }
        }
    }

    /// <summary>
    ///     执行带一个参数的无返回值IPC方法
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="action">要执行的方法</param>
    /// <param name="param">方法参数</param>
    internal static void Execute<T>(Action<T> action, T param) {
        if (IsPluginLoaded()) {
            try {
                action(param);
            }
            catch (Exception ex) {
                Plugin.Log.Error($"IPC执行错误: {ex}");
            }
        }
    }

    /// <summary>
    ///     执行带两个参数的无返回值IPC方法
    /// </summary>
    /// <typeparam name="T1">第一个参数类型</typeparam>
    /// <typeparam name="T2">第二个参数类型</typeparam>
    /// <param name="action">要执行的方法</param>
    /// <param name="p1">第一个参数</param>
    /// <param name="p2">第二个参数</param>
    internal static void Execute<T1, T2>(Action<T1, T2> action, T1 p1, T2 p2) {
        if (IsPluginLoaded()) {
            try {
                action(p1, p2);
            }
            catch (Exception ex) {
                Plugin.Log.Error($"IPC执行错误: {ex}");
            }
        }
    }

    /// <summary>检查导航网格是否已准备就绪</summary>
    /// <returns>如果导航网格已准备就绪，则返回true</returns>
    internal static bool IsReady() {
        // 先检查IPC实例是否已初始化
        if (_navIsReady == null)
            return false;

        bool? result = Execute(() => _navIsReady.InvokeFunc());
        return result.HasValue && result.Value;
    }

    /// <summary>获取导航网格构建进度</summary>
    /// <returns>构建进度（0.0-1.0）</returns>
    internal static float BuildProgress() {
        if (_navBuildProgress == null) return 0f;
        float? result = Execute(() => _navBuildProgress.InvokeFunc());
        return result ?? 0f;
    }

    /// <summary>重新加载导航网格</summary>
    internal static void Reload() {
        if (_navReload == null) return;
        Execute(() => _navReload.InvokeFunc());
    }

    /// <summary>重新构建导航网格</summary>
    internal static void Rebuild() {
        if (_navRebuild == null) return;
        Execute(() => _navRebuild.InvokeFunc());
    }

    /// <summary>计算从起点到终点的路径</summary>
    /// <param name="from">起点坐标</param>
    /// <param name="to">终点坐标</param>
    /// <param name="fly">是否允许飞行（忽略高度限制）</param>
    /// <returns>路径点列表</returns>
    internal static Task<List<Vector3>>? Pathfind(Vector3 from, Vector3 to, bool fly = false) {
        if (_navPathfind == null) return Task.FromResult(new List<Vector3>());
        return Execute(() => _navPathfind.InvokeFunc(from, to, fly));
    }

    /// <summary>检查是否启用了自动加载导航网格</summary>
    /// <returns>如果启用了自动加载，则返回true</returns>
    internal static bool IsAutoLoad() {
        if (_navIsAutoLoad == null) return false;
        bool? result = Execute(() => _navIsAutoLoad.InvokeFunc());
        return result.HasValue && result.Value;
    }

    /// <summary>设置是否自动加载导航网格</summary>
    /// <param name="value">是否启用自动加载</param>
    internal static void SetAutoLoad(bool value) {
        if (_navSetAutoLoad == null) return;
        Execute(() => _navSetAutoLoad.InvokeAction(value));
    }

    /// <summary>查询给定位置附近的最近导航网格点</summary>
    /// <param name="pos">查询位置</param>
    /// <param name="halfExtentXz">XZ平面上的搜索半径</param>
    /// <param name="halfExtentY">Y轴（高度）上的搜索半径</param>
    /// <returns>最近的导航网格点，如果未找到则返回null</returns>
    internal static Vector3? QueryMeshNearestPoint(Vector3 pos, float halfExtentXz, float halfExtentY) {
        if (_queryMeshNearestPoint == null) return null;
        return Execute(() => _queryMeshNearestPoint.InvokeFunc(pos, halfExtentXz, halfExtentY));
    }

    /// <summary>查询给定位置下方的地面点</summary>
    /// <param name="pos">查询位置</param>
    /// <param name="allowUnlandable">是否允许返回不可着陆的点</param>
    /// <param name="halfExtentXz">XZ平面上的搜索半径</param>
    /// <returns>地面点，如果未找到则返回null</returns>
    internal static Vector3? QueryMeshPointOnFloor(Vector3 pos, bool allowUnlandable, float halfExtentXz) {
        if (_queryMeshPointOnFloor == null) return null;
        return Execute(() => _queryMeshPointOnFloor.InvokeFunc(pos, allowUnlandable, halfExtentXz));
    }

    /// <summary>沿指定路径点移动角色</summary>
    /// <param name="waypoints">路径点列表</param>
    /// <param name="fly">是否允许飞行（忽略高度限制）</param>
    internal static void MoveTo(List<Vector3> waypoints, bool fly) {
        if (_pathMoveTo == null) return;
        Execute(() => _pathMoveTo.InvokeAction(waypoints, fly));
    }

    /// <summary>停止当前移动</summary>
    internal static void Stop() {
        if (_pathStop == null) return;
        Execute(() => _pathStop.InvokeAction());
    }

    /// <summary>检查是否正在移动中</summary>
    /// <returns>如果正在移动，则返回true</returns>
    internal static bool IsRunning() {
        if (_pathIsRunning == null) return false;
        bool? result = Execute(() => _pathIsRunning.InvokeFunc());
        return result.HasValue && result.Value;
    }

    /// <summary>检查是否正在移动中 (别名，供Plugin.cs使用)</summary>
    /// <returns>如果正在移动，则返回true</returns>
    internal static bool PathIsRunning() => IsRunning();

    /// <summary>停止当前移动 (别名，供Plugin.cs使用)</summary>
    internal static void PathStop() => Stop();

    /// <summary>沿指定路径点移动角色 (别名，供Plugin.cs使用)</summary>
    /// <param name="waypoints">路径点列表</param>
    /// <param name="fly">是否允许飞行（忽略高度限制）</param>
    internal static void PathMoveTo(List<Vector3> waypoints, bool fly) => MoveTo(waypoints, fly);

    /// <summary>获取当前路径中的路径点数量</summary>
    /// <returns>路径点数量</returns>
    internal static int NumWaypoints() {
        if (_pathNumWaypoints == null) return 0;
        int? result = Execute(() => _pathNumWaypoints.InvokeFunc());
        return result ?? 0;
    }

    /// <summary>获取是否允许移动</summary>
    /// <returns>如果允许移动，则返回true</returns>
    internal static bool GetMovementAllowed() {
        if (_pathGetMovementAllowed == null) return false;
        bool? result = Execute(() => _pathGetMovementAllowed.InvokeFunc());
        return result.HasValue && result.Value;
    }

    /// <summary>设置是否允许移动</summary>
    /// <param name="value">是否允许移动</param>
    internal static void SetMovementAllowed(bool value) {
        if (_pathSetMovementAllowed == null) return;
        Execute(() => _pathSetMovementAllowed.InvokeAction(value));
    }

    /// <summary>获取是否在移动时对齐相机</summary>
    /// <returns>如果在移动时对齐相机，则返回true</returns>
    internal static bool GetAlignCamera() {
        if (_pathGetAlignCamera == null) return false;
        bool? result = Execute(() => _pathGetAlignCamera.InvokeFunc());
        return result.HasValue && result.Value;
    }

    /// <summary>设置是否在移动时对齐相机</summary>
    /// <param name="value">是否对齐相机</param>
    internal static void SetAlignCamera(bool value) {
        if (_pathSetAlignCamera == null) return;
        Execute(() => _pathSetAlignCamera.InvokeAction(value));
    }

    /// <summary>获取路径点到达容差</summary>
    /// <returns>到达容差值</returns>
    internal static float GetTolerance() {
        if (_pathGetTolerance == null) return 0f;
        float? result = Execute(() => _pathGetTolerance.InvokeFunc());
        return result ?? 0f;
    }

    /// <summary>设置路径点到达容差</summary>
    /// <param name="tolerance">到达容差值</param>
    internal static void SetTolerance(float tolerance) {
        if (_pathSetTolerance == null) return;
        Execute(() => _pathSetTolerance.InvokeAction(tolerance));
    }

    /// <summary>计算路径并移动到目标位置（组合了寻路和移动功能）</summary>
    /// <param name="pos">目标位置</param>
    /// <param name="fly">是否允许飞行（忽略高度限制）</param>
    /// <returns>如果成功开始移动，则返回true</returns>
    internal static bool PathfindAndMoveTo(Vector3 pos, bool fly) {
        if (_pathfindAndMoveTo == null) return false;
        bool? result = Execute(() => _pathfindAndMoveTo.InvokeFunc(pos, fly));
        return result.HasValue && result.Value;
    }

    /// <summary>检查寻路过程是否正在进行中</summary>
    /// <returns>如果寻路过程正在进行中，则返回true</returns>
    internal static bool PathfindInProgress() {
        if (_pathfindInProgress == null) return false;
        bool? result = Execute(() => _pathfindInProgress.InvokeFunc());
        return result.HasValue && result.Value;
    }
}