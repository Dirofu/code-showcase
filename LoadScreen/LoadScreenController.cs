using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using CommonTools.Logging;
using Object = UnityEngine.Object;

namespace Core.Scripts.UI
{
	public readonly struct DebugCaller
	{
		public readonly string Class;
		public readonly string Method;
		public readonly int Line;

		public DebugCaller(string file, string method, int line)
		{
			Class = System.IO.Path.GetFileNameWithoutExtension(file);
			Method = method;
			Line = line;
		}

		public override string ToString()
			=> $"{Class}.{Method}:{Line}";
	}
	
	public static class LoadScreenController
	{
		private static Task<Guid> _loadingTask;
		private static LoadScreenMenu _instance;
		private static AsyncOperationHandle<GameObject> _handle;

		private class ProgressEntry
		{
			public float Progress;
			public long DownloadedBytes;
			public long TotalBytes;
			public bool HasBytes;
			public bool IsDone;
		}

		private static readonly Dictionary<int, ProgressEntry> _progressEntries = new();
		private static float _aggregateProgress;

		public static float AggregateProgress => _aggregateProgress;

		private const string LoadScreen = "Prefabs_LoadScreen";

		public static bool IsHandleTasksComplete
		{
			get
			{
				foreach (var entry in _progressEntries.Values)
				{
					if (entry.IsDone == false)
						return false;
				}
				return true;
			}
		}
		
		public static bool IsLoading { get; private set; } = false;

		public static async UniTask<Guid> ShowAsync(
			[CallerMemberName] string callerMember = "",
			[CallerFilePath] string callerFile = "",
			[CallerLineNumber] int callerLine = 0)
		{
			DebugCaller caller = new(callerFile, callerMember, callerLine);
			IsLoading = true;

			if (_instance != null)
			{
				return await _instance.IncreaseLoadScreenTask(caller);
			}

			if (_loadingTask != null)
			{
				await _loadingTask;
				return await _instance.IncreaseLoadScreenTask(caller);
			}

			_loadingTask = LoadAndShowAsync(caller);
			await _loadingTask;
			var result = _loadingTask.Result;
			_loadingTask = null;
			return result;
		}

		public static void Hide(ref Guid taskID)
		{
			if (taskID == Guid.Empty)
				return;
			
			_instance?.DecreaseLoadScreenTask(taskID);
			taskID = Guid.Empty;
			
			if (_instance?.IsLoading == false)
				Unload().Forget();
		}

		public static void ForceReset()
		{
			if (_instance == null)
				return;

			_instance.ResetLoadScreenTask();
			ResetProgressState();
			Unload().Forget();
		}

		public static void ResetProgressState()
		{
			_progressEntries.Clear();
			_aggregateProgress = 0f;
		}

		public static async UniTask Unload()
		{
			await UniTask.WaitWhile(() => _instance?.IsAnimationComplete == false);
			
			if (_instance == null)
				return;

			Object.Destroy(_instance.gameObject);

			if (_handle.IsValid() == true)
				Addressables.Release(_handle);

			IsLoading = false;
			_instance = null;
			ResetProgressState();
		}

		private static async Task<Guid> LoadAndShowAsync(DebugCaller caller)
		{
			_handle = Addressables.LoadAssetAsync<GameObject>(LoadScreen);
			var prefab = _handle.WaitForCompletion();
			var go = Object.Instantiate(prefab);
			_instance = go.GetComponent<LoadScreenMenu>();
			Object.DontDestroyOnLoad(go);
			return await _instance.IncreaseLoadScreenTask(caller);
		}

		public static async UniTask UpdateLoadSliderView<T>(AsyncOperationHandle<T>? handle)
		{
			await UniTask.WaitWhile(() => handle.HasValue == false);
			await UniTask.WaitWhile(() => _instance == null);

			var opHandle = handle.Value;

			if (_instance.IsClosing)
			{
				await opHandle.ToUniTask();
				return;
			}

			int key = opHandle.GetHashCode();

			if (_progressEntries.TryGetValue(key, out var entry) == false)
			{
				entry = new ProgressEntry();
				_progressEntries[key] = entry;
			}

			while (!opHandle.IsDone)
			{
				DownloadStatus status = opHandle.GetDownloadStatus();
				entry.HasBytes = status.TotalBytes > 0;
				entry.DownloadedBytes = status.DownloadedBytes;
				entry.TotalBytes = status.TotalBytes;
				entry.Progress = entry.HasBytes
					? (float)status.DownloadedBytes / status.TotalBytes
					: opHandle.PercentComplete;

				if (entry.HasBytes)
				{
					float downloadedMB = status.DownloadedBytes / 1024f / 1024f;
					float totalMB = status.TotalBytes / 1024f / 1024f;
					Informer.Log($"[Download Bundle] {opHandle.DebugName} {downloadedMB:F2} / {totalMB:F2} MB | {opHandle.PercentComplete}");
				}
				else
				{
					Informer.Log($"[Download Bundle] {opHandle.DebugName} {opHandle.PercentComplete}");
				}

				RefreshAggregateView();
				await UniTask.DelayFrame(1);
			}

			entry.Progress = 1f;
			if (entry.HasBytes)
				entry.DownloadedBytes = entry.TotalBytes;
			entry.IsDone = true;
			RefreshAggregateView();
		}

		private static void RefreshAggregateView()
		{
			if (_instance == null || _progressEntries.Count == 0)
				return;

			float sumProgress = 0f;
			long sumDownloaded = 0;
			long sumTotal = 0;
			bool anyBytes = false;

			foreach (var entry in _progressEntries.Values)
			{
				sumProgress += entry.Progress;
				if (entry.HasBytes)
				{
					anyBytes = true;
					sumDownloaded += entry.DownloadedBytes;
					sumTotal += entry.TotalBytes;
				}
			}

			_aggregateProgress = sumProgress / _progressEntries.Count;

			if (anyBytes && sumTotal > 0)
			{
				float downloadedMB = sumDownloaded / 1024f / 1024f;
				float totalMB = sumTotal / 1024f / 1024f;
				_instance.UpdateStatusText($"{downloadedMB:F2} / {totalMB:F2} MB");
			}
			else
			{
				_instance.UpdateStatusText(string.Empty);
			}
		}
	}
}
