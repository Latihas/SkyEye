using System;
using System.Runtime.InteropServices;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace SkyEye.SkyEye;

public class ChatBox {
	private readonly ProcessChatBoxDelegate _processChatBox;

	internal ChatBox() {
		_processChatBox = Marshal.GetDelegateForFunctionPointer<ProcessChatBoxDelegate>(Plugin.SigScanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F2 48 8B F9 45 84 C9"));
	}


	private unsafe void SendMessageUnsafe(ReadOnlySpan<byte> message) {
		fixed (byte* ptr = message) {
			var ptr2 = Utf8String.FromSequence(ptr);
			_processChatBox(UIModule.Instance(), ptr2, IntPtr.Zero, 0);
		}
	}

	public void SendMessage(string message) =>
		SendMessageUnsafe(Encoding.UTF8.GetBytes(message));


	private unsafe delegate void ProcessChatBoxDelegate(UIModule* module, Utf8String* message, IntPtr a3, byte a4);
}