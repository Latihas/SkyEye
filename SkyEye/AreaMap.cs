using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace SkyEye.SkyEye;

internal static class AreaMap {
	internal static unsafe AtkUnitBase* AreaMapAddon => (AtkUnitBase*)Plugin.Gui.GetAddonByName("AreaMap");


	internal static unsafe bool MapVisible => AreaMapAddon != (AtkUnitBase*)IntPtr.Zero && AreaMapAddon->IsVisible;

	internal static unsafe ref float MapScale => ref *(float*)((byte*)AreaMapAddon + 980);
}