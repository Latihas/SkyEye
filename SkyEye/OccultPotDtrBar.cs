using System;
using System.Collections.Generic;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using static SkyEye.Plugin;

namespace SkyEye;

// Adapted from EurekaTrackerAutoPopper's MIT-licensed PotDtrBar behavior.
internal sealed class OccultPotDtrBar : IDisposable {
	private const string EntryTitle = "SkyEye-Pot";
	private const int OccultRespawnSeconds = 1800;
	private readonly OccultTracker _tracker;
	private readonly IDtrBarEntry entry;
	private DateTime LastDtrBarUpdate = DateTime.MinValue;

	internal OccultPotDtrBar(OccultTracker tracker) {
		_tracker = tracker;
		entry = DtrBar.Get(EntryTitle);
		entry.OnClick += OnClick;
		entry.Tooltip = "点击放置Flag";
	}

	internal void Update(IFramework _) {
		var now = DateTime.Now;
		if ((now - LastDtrBarUpdate).TotalSeconds < 1) return;
		LastDtrBarUpdate = now;
		if (!Configuration.PluginEnabled || !Configuration.EnableOccultCrowd ||
		    !Configuration.ShowOccultPotDtrBar || !InOccult() || !_tracker.TryGetPotInfo(ClientState.TerritoryType, out var potInfo)) {
			entry.Shown = false;
			return;
		}

		if (OccultTracker.IsActive(potInfo.DisplayFate)) {
			entry.Text = $"罐子: 存活{OccultPotDefinitions.GetPosition(potInfo.DisplayFate.FateId)}";
			entry.Shown = true;
			return;
		}

		var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		var respawnTime = TimeSpan.FromSeconds(potInfo.LastFate.SpawnTime + OccultRespawnSeconds - currentTime);
		if (respawnTime.TotalSeconds < 0) respawnTime = TimeSpan.Zero;
		entry.Text = $"下个罐子: {FormatClock(respawnTime)}{OccultPotDefinitions.GetPosition(potInfo.DisplayFate.FateId)}";
		entry.Shown = true;
	}

	public void Dispose() {
		entry.OnClick -= OnClick;
		entry.Shown = false;
		entry.Remove();
	}

	private void OnClick(DtrInteractionEvent _) {
		if (!_tracker.TryGetPotInfo(ClientState.TerritoryType, out var potInfo) ||
		    !OccultPotDefinitions.TryGetWorldPosition(potInfo.DisplayFate.FateId, out var position)) return;
		Framework.RunOnTick(() => {
			unsafe {
				AgentMap.Instance()->SetFlagMapMarker(ClientState.TerritoryType, ClientState.MapId, position);
				AgentMap.Instance()->OpenMapByMapId(ClientState.MapId);
			}
		});
	}

	private static string FormatClock(TimeSpan span) =>
		span.Hours == 0 ? $"{span.Minutes:00}:{span.Seconds:00}" : $"{span.Hours:00}:{span.Minutes:00}:{span.Seconds:00}";
}

internal static class OccultPotDefinitions {
	private static readonly Dictionary<uint, (string Position, System.Numerics.Vector3 WorldPosition)> Definitions = new() {
		[1976] = (" (北)", new(204.66835f, 111.81729f, -204.96242f)),
		[1977] = (" (南)", new(-479.8395f, 75f, 524.78894f)),
		[2072] = (" (北)", new(233f, 7.729229f, -470f)),
		[2073] = (" (南)", new(-505.2822f, 53.14409f, 244.041f))
	};

	internal static string GetPosition(uint fateId) => Definitions.TryGetValue(fateId, out var definition) ? definition.Position : "";

	internal static bool TryGetWorldPosition(uint fateId, out System.Numerics.Vector3 position) {
		if (Definitions.TryGetValue(fateId, out var definition)) {
			position = definition.WorldPosition;
			return true;
		}
		position = default;
		return false;
	}
}