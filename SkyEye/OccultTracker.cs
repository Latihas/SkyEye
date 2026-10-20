using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Threading;
using Dalamud.Plugin.Services;
using SkyEye.Data;
using static SkyEye.Plugin;

namespace SkyEye;

internal sealed class OccultTracker : IDisposable {
	private const string TableName = "OccultTrackerV3";
	private const string Endpoint = "https://infi.ovh/api";
	private const string ApiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyb2xlIjoiYW5vbiJ9.Ur6wgi_rD4dr3uLLvbLoaEvfLCu4QFWdrF-uHRtbl_s";
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
		PropertyNameCaseInsensitive = true
	};

	private readonly HttpClient _client = new() {
		Timeout = TimeSpan.FromSeconds(30)
	};
	private readonly Lock _sync = new();
	private OccultTrackerSnapshot _snapshot = OccultTrackerSnapshot.Empty;
	private CancellationTokenSource? _pollCts;
	private Task? _pollTask;
	private bool _disposed;
	private string? _lastError;

	internal OccultTracker() {
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
		_client.DefaultRequestHeaders.Add("Prefer", "return=representation");
		_client.DefaultRequestHeaders.UserAgent.ParseAdd("OccultTracker/1.0");
	}

	internal OccultTrackerSnapshot Snapshot => _snapshot;

	internal void Update(IFramework _) {
		if (_disposed) return;
		if (!Configuration.PluginEnabled || !Configuration.EnableOccultCrowd) {
			StopPolling();
			ClearCache();
			return;
		}
		StartPolling();
	}

	internal void RequestRefresh() {
		lock (_sync) _pollCts?.Cancel();
	}

	internal void ClearCache() => _snapshot = OccultTrackerSnapshot.Empty;

	internal IReadOnlyList<OccultSharedFate> GetEvents(uint territory, OccultEventCategory category) {
		var source = Snapshot.Records
			.Where(record => record.Territory == territory)
			.SelectMany(record => category switch {
				OccultEventCategory.Encounter => record.Encounters,
				OccultEventCategory.Fate => record.Fates,
				OccultEventCategory.Bunny => record.Bunnies,
				_ => []
			});
		return source
			.GroupBy(fate => fate.FateId)
			.Select(group => group
				.OrderByDescending(IsActive)
				.ThenByDescending(fate => fate.LastSeen)
				.ThenByDescending(fate => fate.SpawnTime)
				.First())
			.OrderByDescending(IsActive)
			.ThenByDescending(fate => fate.LastSeen)
			.ThenBy(fate => fate.FateId)
			.ToArray();
	}

	internal bool TryGetPotInfo(uint territory, out OccultPotInfo info) {
		var bunnies = GetEvents(territory, OccultEventCategory.Bunny);
		if (bunnies.Count == 0) {
			info = default;
			return false;
		}

		var sorted = bunnies.OrderBy(fate => fate.LastSeen).ToArray();
		var nextSpawn = sorted[0];
		var lastSeen = sorted[^1];
		if (nextSpawn.LastSeen == -1 && lastSeen.LastSeen == -1) {
			info = default;
			return false;
		}

		if (IsActive(lastSeen)) {
			info = new(lastSeen, lastSeen);
			return true;
		}

		info = new(nextSpawn, lastSeen);
		return true;
	}

	internal static bool IsActive(OccultSharedFate fate) => fate.DeathTime == 0 && fate.SpawnTime > 0;

	internal static string FormatEpoch(long epoch) {
		return epoch <= 0
			? "-"
			: DateTimeOffset.FromUnixTimeSeconds(epoch).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
	}

	public void Dispose() {
		Task? task;
		lock (_sync) {
			if (_disposed) return;
			_disposed = true;
			_pollCts?.Cancel();
			task = _pollTask;
		}

		try {
			task?.Wait(TimeSpan.FromSeconds(2));
		} catch (AggregateException) {
			// The polling task reports its own errors and is being stopped.
		}
		_client.Dispose();
		Volatile.Write(ref _snapshot, OccultTrackerSnapshot.Empty);
	}

	private void StartPolling() {
		lock (_sync) {
			if (_disposed || _pollTask is { IsCompleted: false }) return;
			var cts = new CancellationTokenSource();
			_pollCts = cts;
			_pollTask = Task.Run(() => PollLoopAsync(cts));
		}
	}

	private void StopPolling() {
		lock (_sync) _pollCts?.Cancel();
	}

	private async Task PollLoopAsync(CancellationTokenSource polling) {
		try {
			while (!polling.IsCancellationRequested) {
				try {
					await RefreshAsync(polling.Token);
				} catch (OperationCanceledException) when (polling.IsCancellationRequested) {
					break;
				} catch (Exception ex) {
					PublishError(ex);
				}

				await Task.Delay(
					TimeSpan.FromSeconds(Math.Clamp(Configuration.OccultTrackerIntervalSeconds, 1, 60)),
					polling.Token);
			}
		} catch (OperationCanceledException) when (polling.IsCancellationRequested) {
			// Expected when the plugin is disabled or unloaded.
		} finally {
			lock (_sync) {
				if (ReferenceEquals(_pollCts, polling)) {
					_pollCts = null;
					_pollTask = null;
				}
			}
			polling.Dispose();
		}
	}

	private async Task RefreshAsync(CancellationToken cancellationToken) {
		var requestUri = $"{Endpoint}/{TableName}?order=last_update.desc&limit=100&territory=in.({(uint)Territory.SouthHorn},{(uint)Territory.NorthHorn})";
		using var response = await _client.GetAsync(requestUri, cancellationToken);
		response.EnsureSuccessStatusCode();
		await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
		var records = await JsonSerializer.DeserializeAsync<List<OccultTrackerRecord>>(stream, JsonOptions, cancellationToken) ?? [];
		cancellationToken.ThrowIfCancellationRequested();
		if (_disposed) return;
		foreach (var record in records) record.InitializeHistories();
		Volatile.Write(ref _snapshot, new(DateTimeOffset.UtcNow, records, null));
		_lastError = null;
	}

	private void PublishError(Exception exception) {
		if (_disposed) return;
		var message = exception.Message;
		var current = Snapshot;
		Volatile.Write(ref _snapshot, current with { Error = message });
		if (string.Equals(_lastError, message, StringComparison.Ordinal)) return;
		_lastError = message;
		Log.Warning(exception, "[OccultTracker] 获取众包数据失败");
	}
}

internal enum OccultEventCategory {
	Encounter,
	Fate,
	Bunny
}

internal sealed record OccultTrackerSnapshot(
	DateTimeOffset RetrievedAt,
	IReadOnlyList<OccultTrackerRecord> Records,
	string? Error) {
	internal static readonly OccultTrackerSnapshot Empty = new(DateTimeOffset.MinValue, [], null);
}

internal sealed class OccultTrackerRecord {
	private static readonly JsonSerializerOptions HistoryJsonOptions = new(JsonSerializerDefaults.Web) {
		PropertyNameCaseInsensitive = true
	};

	[JsonPropertyName("id")]
	public long Id { get; set; }

	[JsonPropertyName("last_update")]
	public long LastUpdate { get; set; }

	[JsonPropertyName("tracker_id")]
	public string TrackerId { get; set; } = string.Empty;

	[JsonPropertyName("territory")]
	public uint Territory { get; set; }

	[JsonPropertyName("encounter_history")]
	public string EncounterHistory { get; set; } = "[]";

	[JsonPropertyName("fate_history")]
	public string FateHistory { get; set; } = "[]";

	[JsonPropertyName("pot_history")]
	public string PotHistory { get; set; } = "[]";

	[JsonIgnore]
	public IReadOnlyList<OccultSharedFate> Encounters { get; private set; } = [];

	[JsonIgnore]
	public IReadOnlyList<OccultSharedFate> Fates { get; private set; } = [];

	[JsonIgnore]
	public IReadOnlyList<OccultSharedFate> Bunnies { get; private set; } = [];

	internal void InitializeHistories() {
		Encounters = ParseHistory(EncounterHistory);
		Fates = ParseHistory(FateHistory);
		Bunnies = ParseHistory(PotHistory);
	}

	private static IReadOnlyList<OccultSharedFate> ParseHistory(string history) {
		if (string.IsNullOrWhiteSpace(history)) return [];
		try {
			return JsonSerializer.Deserialize<OccultSharedFate[]>(
				history,
				HistoryJsonOptions) ?? [];
		} catch (JsonException) {
			return [];
		}
	}
}

internal sealed class OccultSharedFate {
	[JsonPropertyName("fate_id")]
	public uint FateId { get; set; }

	[JsonPropertyName("spawn_time")]
	public long SpawnTime { get; set; }

	[JsonPropertyName("death_time")]
	public long DeathTime { get; set; }

	[JsonPropertyName("last_seen")]
	public long LastSeen { get; set; }

	[JsonPropertyName("killed_fates")]
	public int KilledFates { get; set; }

	[JsonPropertyName("killed_ces")]
	public int KilledCes { get; set; }

	[JsonPropertyName("state")]
	public byte State { get; set; }
}

internal readonly record struct OccultPotInfo(OccultSharedFate DisplayFate, OccultSharedFate LastFate);