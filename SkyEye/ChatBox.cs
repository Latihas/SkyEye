using System.Text;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace SkyEye;

public static class ChatBox {
	public static unsafe void SendMessage(string message) {
		if (Plugin.ObjectTable.LocalPlayer is null) return;
		if (message.StartsWith("/e ")) message = "/e [SkyEye]" + message[2..];
		Plugin.Framework.RunOnTick(() => {
			fixed (byte* ptr = Encoding.UTF8.GetBytes(message)) {
				UIModule.MemberFunctionPointers.ProcessChatBoxEntry(UIModule.Instance(), Utf8String.FromSequence(ptr), 0, false);
			}
		});
	}
}