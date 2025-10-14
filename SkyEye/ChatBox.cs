using System;
using System.Runtime.InteropServices;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace SkyEye.SkyEye;

public static class ChatBox {
    private static ProcessChatBoxDelegate? _processChatBox;

    public static unsafe void SendMessage(string message) {
        _processChatBox ??= Marshal.GetDelegateForFunctionPointer<ProcessChatBoxDelegate>(Plugin.SigScanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F2 48 8B F9 45 84 C9"));
        fixed (byte* ptr = Encoding.UTF8.GetBytes(message)) _processChatBox(UIModule.Instance(), Utf8String.FromSequence(ptr), 0, 0);
    }

    private unsafe delegate void ProcessChatBoxDelegate(UIModule* module, Utf8String* message, IntPtr a3, byte a4);
}