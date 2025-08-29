using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Dalamud;

namespace SkyEye.SkyEye;

internal class SpeedHackPlugin {
	private readonly IntPtr _flySpeedPtr = IntPtr.Zero, _newFuncMem = IntPtr.Zero, _speedPtr = IntPtr.Zero;
	private readonly byte[] _originalFlyBytes = new byte[5], _originalSpeedBytes = new byte[5];

	public SpeedHackPlugin() {
		try {
			var processHandle = Process.GetCurrentProcess().Handle;
			var baseAddress = Process.GetCurrentProcess().MainModule!.BaseAddress;
			if (!Plugin.SigScanner.TryScanText("0F 2E ?? 0F 28 ?? ?? ?? F3 0F 11 ?? ?? 0F", out var speedPtrOffset))
				if (!Plugin.SigScanner.TryScanText("0F 2E ?? 0F 28 ?? ?? ?? E9", out speedPtrOffset))
					throw new Exception("Failed to find signature for Speed.");
			if (!Plugin.SigScanner.TryScanText("0F 2F ?? F3 0F 11 ?? ?? F3 0F 59", out var flySpeedPtrOffset))
				if (!Plugin.SigScanner.TryScanText("0F 2F ?? E9 ?? ?? ?? ?? F3", out flySpeedPtrOffset))
					throw new Exception("Failed to find signature for FlySpeed.");
			_speedPtr = speedPtrOffset + 8;
			_flySpeedPtr = flySpeedPtrOffset + 3;
			SafeMemory.ReadBytes(_speedPtr, 5, out _originalSpeedBytes);
			SafeMemory.ReadBytes(_flySpeedPtr, 5, out _originalFlyBytes);
			SafeMemory.Read<byte>(_speedPtr, out var speedPtrInitialByte);
			if (speedPtrInitialByte == 0xF3) {
				_newFuncMem = AllocateNear(processHandle, baseAddress, 0x100);
				if (_newFuncMem == IntPtr.Zero) return;
				SafeMemory.ReadBytes(_speedPtr, 5, out var speedCode);
				byte[] speedCodeReplace = [0xE9, 0x00, 0x00, 0x00, 0x00];
				SafeMemory.ReadBytes(_flySpeedPtr, 5, out var flyCode);
				byte[] flyCodeReplace = [0xE9, 0x00, 0x00, 0x00, 0x00];
				byte[] newFuncCode = [
					0xF3, 0x0F, 0x59, 0x00, 0x1C, 0x00, 0x00, 0x00,
					speedCode[0], speedCode[1], speedCode[2], speedCode[3], speedCode[4],
					0xE9, 0x00, 0x00, 0x00, 0x00,
					0xF3, 0x0F, 0x59, 0x00, 0x0A, 0x00, 0x00, 0x00,
					flyCode[0], flyCode[1], flyCode[2], flyCode[3], flyCode[4],
					0xE9, 0x00, 0x00, 0x00, 0x00
				];
				var speedXmmRegister = speedCode[3] >> 3 & 0x07;
				newFuncCode[3] = (byte)(speedXmmRegister << 3 | 0x05);
				Array.Copy(BitConverter.GetBytes((uint)(_newFuncMem - (long)_speedPtr - 5)),
					0, speedCodeReplace, 1, 4);
				Array.Copy(BitConverter.GetBytes((uint)(_speedPtr - (long)_newFuncMem - 13)),
					0, newFuncCode, 14, 4);
				var flyXmmRegister = flyCode[3] >> 3 & 0x07;
				newFuncCode[21] = (byte)(flyXmmRegister << 3 | 0x05);
				Array.Copy(BitConverter.GetBytes((uint)(_newFuncMem - (long)_flySpeedPtr + 13)),
					0, flyCodeReplace, 1, 4);
				Array.Copy(BitConverter.GetBytes((uint)(_flySpeedPtr - (long)_newFuncMem - 31)),
					0, newFuncCode, 32, 4);
				SafeMemory.Write(_speedPtr, speedCodeReplace);
				SafeMemory.Write(_flySpeedPtr, flyCodeReplace);
				SafeMemory.Write(_newFuncMem, newFuncCode);
			}
			else if (speedPtrInitialByte == 0xE9) {
				SafeMemory.Read<int>(IntPtr.Add(_speedPtr, 1), out var newFuncOffset);
				_newFuncMem = IntPtr.Add(_speedPtr, newFuncOffset + 5);
			}
		}
		catch (Exception ex) {
			Plugin.Log.Error($"初始化失败: {ex.Message}");
		}
	}

	public void Dispose() {
		try {
			if (_speedPtr != IntPtr.Zero && _originalSpeedBytes.Length > 0)
				SafeMemory.Write(_speedPtr, _originalSpeedBytes);
			if (_flySpeedPtr != IntPtr.Zero && _originalFlyBytes.Length > 0)
				SafeMemory.Write(_flySpeedPtr, _originalFlyBytes);
			if (_newFuncMem != IntPtr.Zero)
				VirtualFreeEx(Process.GetCurrentProcess().Handle, _newFuncMem, 0, 0x8000);
		}
		catch (Exception ex) {
			Plugin.Log.Error($"插件释放资源失败: {ex.Message}");
		}
	}


	public void SetSpeedMultiplier(float value) {
		if (_newFuncMem != IntPtr.Zero) {
			Plugin.Log.Info("Speed " + value);
			SafeMemory.Write(IntPtr.Add(_newFuncMem, 0x24), value);
		}
		else Plugin.Log.Error("newFuncMem Failed.");
	}


	private static IntPtr AllocateNear(IntPtr hProcess, IntPtr desiredAddress, uint size) {
		if (hProcess == IntPtr.Zero)
			throw new ArgumentNullException(nameof(hProcess));
		if (desiredAddress == IntPtr.Zero)
			throw new ArgumentNullException(nameof(desiredAddress));
		if (size == 0)
			throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than zero.");
		const int offsetStep = 0x1000, maxOffset = 0x200000; // 4KB 对齐，最大偏移量 2MB 
		for (var offset = 0; offset <= maxOffset; offset += offsetStep) {
			var targetAddress = IntPtr.Subtract(desiredAddress, offset);
			if (TryAllocateMemory(hProcess, targetAddress, size, out var allocatedAddress))
				return allocatedAddress;
		}
		return IntPtr.Zero;
	}

	private static bool TryAllocateMemory(IntPtr hProcess, IntPtr targetAddress, uint size, out IntPtr allocatedAddress) {
		const uint memCommit = 0x00001000, memReserve = 0x00002000, pageExecuteReadwrite = 0x40, memFree = 0x10000;
		allocatedAddress = IntPtr.Zero;
		if (VirtualQueryEx(hProcess, targetAddress, out var memoryInfo, (uint)Marshal.SizeOf(typeof(MemoryBasicInformation))) == 0 ||
		    memoryInfo.State != memFree || (ulong)memoryInfo.RegionSize < size) return false;
		allocatedAddress = VirtualAllocEx(hProcess, targetAddress, size, memCommit | memReserve, pageExecuteReadwrite);
		return allocatedAddress != IntPtr.Zero;
	}

	[DllImport("kernel32.dll")]
	private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

	[DllImport("kernel32.dll")]
	private static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MemoryBasicInformation lpBuffer, uint dwLength);

	[DllImport("kernel32.dll")]
	private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

	[StructLayout(LayoutKind.Sequential)]
	private struct MemoryBasicInformation {
		public IntPtr BaseAddress;
		public IntPtr AllocationBase;
		public uint AllocationProtect;
		public IntPtr RegionSize;
		public uint State;
		public uint Protect;
		public uint Type;
	}
}