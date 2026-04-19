using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using static SkyEye.Plugin;

namespace SkyEye;

public partial class ConfigWindow {
	private static string ReplaceNameplate(string str) => GetChangedName(str);

	private static string GetChangedName(string str) {
		if (string.IsNullOrEmpty(str))
			return str;
		foreach (var entry in Configuration.NameReplacementDict.Where(entry => entry.Item1 == str)) return entry.Item2;
		var lt = str.Split(' ');
		return lt.Length != 2 ? str : string.Join(" . ", lt.Select(s => s.ToUpper().FirstOrDefault()));
	}

	internal static void DisableNameplate() {
		_atkTextNodeSetTextHook?.Disable();
		_atkTextNodeSetTextHook?.Dispose();
		NamePlate.OnDataUpdate -= NamePlate_OnDataUpdate;
	}

	internal static void EnableNameplate() {
		_atkTextNodeSetTextHook = GameInteropProvider.HookFromAddress(SigScanner.ScanText("E8 ?? ?? ?? ?? 33 F6 0F B7 D6"), (AtkTextNodeSetTextDelegate)AtkTextNodeSetTextDetour);
		_atkTextNodeSetTextHook.Enable();
		NamePlate.OnDataUpdate += NamePlate_OnDataUpdate;
	}

	private static void DrawNameReplacement() {
		if (ImGui.Checkbox("启用", ref Configuration.NameReplacement)) {
			Configuration.Save();
			if (Configuration.NameReplacement) EnableNameplate();
			else DisableNameplate();
		}
		if (!Configuration.NameReplacement) return;
		if (ImGui.Button("添加")) {
			Configuration.NameReplacementDict.Add(new ValueTuple<string, string>("原始", "替换"));
			Configuration.Save();
		}
		try {
			for (var index = 0; index < Configuration.NameReplacementDict.Count; index++) {
				var p1 = Configuration.NameReplacementDict[index].Item1;
				if (ImGui.InputText($"##Ori{index}", ref p1)) {
					Configuration.NameReplacementDict[index] = new(p1, Configuration.NameReplacementDict[index].Item2);
					if (string.IsNullOrEmpty(Configuration.NameReplacementDict[index].Item1) && string.IsNullOrEmpty(Configuration.NameReplacementDict[index].Item2))
						Configuration.NameReplacementDict.RemoveAt(index);
					Configuration.Save();
				}
				ImGui.SameLine();
				ImGui.Text("->");
				ImGui.SameLine();
				var p2 = Configuration.NameReplacementDict[index].Item2;
				if (ImGui.InputText($"##Rep{index}", ref p2)) {
					Configuration.NameReplacementDict[index] = new(Configuration.NameReplacementDict[index].Item1, p2);
					if (string.IsNullOrEmpty(Configuration.NameReplacementDict[index].Item1) && string.IsNullOrEmpty(Configuration.NameReplacementDict[index].Item2))
						Configuration.NameReplacementDict.RemoveAt(index);
					Configuration.Save();
				}
			}
		} catch {
			//
		}
	}

	private static void AtkTextNodeSetTextDetour(IntPtr node, IntPtr text) {
		_atkTextNodeSetTextHook?.Original(node, Configuration.NameReplacement ? ChangeName(text) : text);
	}

	private static unsafe SeString GetSeStringFromPtr(IntPtr seStringPtr) {
		int offset;
		for (offset = 0; *(bool*)(seStringPtr + offset); offset++) {
		}
		var bytes = new byte[offset];
		Marshal.Copy(seStringPtr, bytes, 0, offset);
		return SeString.Parse(bytes);
	}

	private static IntPtr ChangeName(IntPtr seStringPtr) {
		if (seStringPtr == IntPtr.Zero) return seStringPtr;
		var str = GetSeStringFromPtr(seStringPtr);
		if (ChangeSeString(str)) GetPtrFromSeString(str, seStringPtr);
		return seStringPtr;
	}

	private static void GetPtrFromSeString(SeString str, IntPtr ptr) {
		var bytes = str.Encode();
		Marshal.Copy(bytes, 0, ptr, bytes.Length);
		Marshal.WriteByte(ptr, bytes.Length, 0);
	}


	private static bool ChangeSeString(SeString seString) => seString.Payloads.Any(payload => payload.Type == PayloadType.RawText)
	                                                         && Configuration.NameReplacementDict.Select(i => ((string[])[i.Item1], i.Item2)).Any(pair => ReplacePlayerName(seString, pair.Item1, pair.Item2));

	private static bool ReplacePlayerName(SeString text, IEnumerable<string> names, string replacement) => names.Any(name => {
		if (string.IsNullOrEmpty(name)) return false;
		var result = false;
		foreach (var payload in text.Payloads) {
			if (payload is not TextPayload load || string.IsNullOrEmpty(load.Text)) continue;
			var t = load.Text.Replace(name, replacement);
			if (t != load.Text) {
				load.Text = t;
				result = true;
			}
		}
		return result;
	});

	private static void NamePlate_OnDataUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers) {
		foreach (var handler in handlers) {
			var namePlateKind = handler.NamePlateKind;
			var str = handler.Name.TextValue;
			if (namePlateKind != NamePlateKind.PlayerCharacter) {
				if (namePlateKind != NamePlateKind.EventNpcCompanion) continue;
				if (handler.GameObject?.ObjectKind == ObjectKind.Companion) {
					str = handler.Title.ToString();
					if (!string.IsNullOrEmpty(str) && str.Length >= 3) {
						var start = str[0];
						var end = str[^1];
						handler.Title = string.Concat(new ReadOnlySpan<char>(ref start), ReplaceNameplate(str[1..^1]), new ReadOnlySpan<char>(ref end));
					}
				}
				continue;
			}
			if (!string.IsNullOrEmpty(str))
				handler.Name = ReplaceNameplate(str);
		}
	}

	private delegate void AtkTextNodeSetTextDelegate(IntPtr node, IntPtr text);

	private static Hook<AtkTextNodeSetTextDelegate>? _atkTextNodeSetTextHook;
}