using FFXIVClientStructs.FFXIV.Client.UI;

namespace SkyEye;

internal static class AreaMap {
	internal static unsafe AddonAreaMap* AreaMapAddon => Plugin.Gui.GetAddonByName<AddonAreaMap>("AreaMap");
	internal static unsafe bool MapVisible => AreaMapAddon->IsVisible;
	internal static unsafe float MapScale => AreaMapAddon->AreaMap.MapScale;
}