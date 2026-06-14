using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Core.Scripts.Scene;
using Core.Scripts.Language;
using Sirenix.OdinInspector;
using Cysharp.Threading.Tasks;
using Core.Scripts.UI.Universal;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading;
using System.Linq;
using AYellowpaper.SerializedCollections;
using CommonTools.Logging;
using Shared.Extensions;
using Random = UnityEngine.Random;

namespace Core.Scripts.UI
{
	public class LoadScreenMenu : MonoBehaviour
	{
		[SerializeField] private TMP_Text _loadingStatus;
		[SerializeField] private TMP_Text _tip;

		[SerializeField] private Image _currentImage;
		[SerializeField] private Image _loadImage;
		[SerializeField] private CanvasGroup _group;

		[SerializeField] private float _speedRotatingLoadImage = 1.0f;
		[SerializeField] private float _speedAlphaColorDecrease = 1.0f;

		[SerializeField] private SliderView _slider;

		[Header("Slider settings")]
		[SerializeField] private float _sliderSpeed = 0.5f;
		[SerializeField, Range(0f, 1f)] private float _softCeiling = 0.95f;

		[Header("Show image settings")]
		[SerializeField] private float _timeToShowImage = 1f;
		[SerializeField] private int _showImageDelay = 5;

		[Header("Show tips settings")]
		[SerializeField] private int _showTipDelay = 20;
		
		#if UNITY_EDITOR
		[SerializeField] private SerializedDictionary<string, string> _loadingTasks;
		#endif
		
		public bool IsLoading => _tasks.Count > 0;

		public const float TimeBeforeDisableLoadScreen = 1.5f;

		public bool IsAnimationComplete { get; private set; } = false;
		public bool IsClosing { get; private set; } = false;

		private Coroutine _loadCoroutine;

		private string _loadingStatusFormat = "{0} <color=#CDEAEF80> {1}";
		private string _baseText => LanguageManager.Instance != null ? LanguageManager.Instance?.GetLanguageText(AliasConstants.LoadingTitle) : string.Empty;
		private ProjectContext _context => ProjectContext.Instance;

		private readonly HashSet<Guid> _tasks = new();

		private float _displayedProgress;
		private bool _sliderTaskRunning;
		
		[Button]
		public async UniTask<Guid> IncreaseLoadScreenTask(DebugCaller caller)
		{
			bool isNewSession = _tasks.Count == 0;

			Guid taskId = Guid.NewGuid();
			_tasks.Add(taskId);

			if (isNewSession)
			{
				LoadScreenController.ResetProgressState();
				ResetSliderDisplay();
				IsClosing = false;
			}
#if !UNITY_EDITOR
			Informer.LogContext(this, $"LoadScreen task increased. Tasks id: {taskId}");
#elif UNITY_EDITOR
			_loadingTasks[taskId.ToString()] = caller.ToString();
			Informer.LogContext(this, $"LoadScreen task increased. Tasks id: {taskId} | Caller: {caller}");
#endif
			
			if (_loadCoroutine != null)
				return taskId;

			_loadCoroutine = StartCoroutine(ProcessLoadImageAnimation());
			_currentImage.color = new(1, 1, 1, 0);

			gameObject.SetActive(true);
			EnsureSliderTaskRunning();
			await SetRandomBackground();
			return taskId;
		}

		private async UniTask SetRandomBackground()
		{
			CancellationToken token = this.GetCancellationTokenOnDestroy();

			await UniTask.WaitUntil(() => _context != null, cancellationToken: token);
			
			if (_context.PathHandle.IsDone == false)
				LoadScreenController.UpdateLoadSliderView<LoadScreenAddressablePath>(_context.PathHandle).Forget();

			await UniTask.WaitUntil(() => _context.AddressablesPath != null, cancellationToken: token);

			List<Sprite> sprites = await GetLoadingSprites();

			if (sprites.Count <= 0)
			{
				Informer.LogErrorContext(this, $"{SceneLoader.Instance.CurrentScene} dont have any sprites in LoadScreenAddressablePath");
				return;
			}
			
			_currentImage.sprite = sprites.OrderBy(_ => Random.value)
				.FirstOrDefault();
			
			_tip.text = GetTip(TipType.Any);
			await ChangeAlphaCurrentImage(0, 1, token);

			ShowNextRandomSprite(TipType.Any, sprites, token).Forget();
		}

		private async UniTask<List<Sprite>> GetLoadingSprites()
		{
			List<Sprite> sprites = new();
			var currentScene = SceneLoader.Instance != null ? SceneLoader.Instance.CurrentScene : LocationSceneType.S_Menu;

			if (_context.AddressablesPath.Connector.TryGetValue(currentScene, out var connector) == false)
				return sprites;
			
			IReadOnlyList<AssetReference> spriteRefs = connector.Backgrounds;

			foreach (var spriteRef in spriteRefs)
			{
				if (spriteRef.OperationHandle.IsValid() && spriteRef.OperationHandle.Status == AsyncOperationStatus.Succeeded)
				{
					sprites.Add(spriteRef.OperationHandle.Result as Sprite);
				}
				else
				{
					var handle = spriteRef.LoadAssetAsync<Sprite>();
					LoadScreenController.UpdateLoadSliderView<Sprite>(handle).Forget();
					await handle.ToUniTask();
					sprites.Add(handle.Result);
				}
			}
			return sprites;
		}


		private async UniTask ShowNextRandomSprite(TipType type, List<Sprite> sprites, CancellationToken token)
		{
			while (token.IsCancellationRequested == false)
			{
				Sprite sprite = sprites.Where(s => s != _currentImage.sprite)
					   .OrderBy(_ => Random.value)
					   .FirstOrDefault();

				if (sprite == null)
					sprite = _currentImage.sprite;

				await UniTask.Delay(_showImageDelay * 1000, cancellationToken: token);
				await ChangeAlphaCurrentImage(1, 0, token);
				_currentImage.sprite = sprite;
				_tip.text = GetTip(type);
				await ChangeAlphaCurrentImage(0, 1, token);
			}
		}

		private async UniTask ChangeAlphaCurrentImage(float start, float target, CancellationToken token)
		{
			Color color = new(1, 1, 1, start);
			float currentTime = 0;

			while (currentTime < _timeToShowImage)
			{
				await UniTask.DelayFrame(1, cancellationToken: token);
				color.a = Mathf.Lerp(start, target, currentTime / _timeToShowImage);
				currentTime += Time.deltaTime;
				_currentImage.color = color;
			}
		}

		private string GetTip(TipType type)
		{
			if (type == TipType.Any)
			{
				int randomTipType = Random.Range(1, System.Enum.GetValues(typeof(TipType)).Length);
				int randomTipIndex = Random.Range(0, _context.AddressablesTips.Tips[(TipType)randomTipType].Tips.Count);

				return _context.AddressablesTips.Tips[(TipType)randomTipType].Tips[randomTipIndex].GetTextByLanguage;
			}
			else
			{
				int randomTipIndex = Random.Range(0, _context.AddressablesTips.Tips[type].Tips.Count);
				return _context.AddressablesTips.Tips[type].Tips[randomTipIndex].GetTextByLanguage;
			}
		}

		[Button]
		public void DecreaseLoadScreenTask(Guid taskID)
		{
			_tasks.Remove(taskID);
			#if !UNITY_EDITOR
			Informer.LogContext(this, $"LoadScreen task decreased. Removed task: {taskID}");
			#elif UNITY_EDITOR
			Informer.LogContext(this, $"LoadScreen task decreased. Removed task: {taskID} | {_loadingTasks[taskID.ToString()]}");
			_loadingTasks.Remove(taskID.ToString());
			#endif
		}

		public void ResetLoadScreenTask()
		{
			if (_loadCoroutine != null)
			{
				StopCoroutine(_loadCoroutine);
				_loadCoroutine = null;
			}

#if UNITY_EDITOR
			_loadingTasks = new();
#endif
			_tasks.Clear();
			IsClosing = false;
			IsAnimationComplete = true;
		}

		public void ResetSliderDisplay()
		{
			_displayedProgress = 0f;
			_slider.SetSliderValue(0f, text:"0%");
			EnsureSliderTaskRunning();
		}

		private void EnsureSliderTaskRunning()
		{
			if (_sliderTaskRunning)
				return;

			_sliderTaskRunning = true;
			RunSliderTask(this.GetCancellationTokenOnDestroy()).Forget();
		}

		private async UniTaskVoid RunSliderTask(CancellationToken token)
		{
			try
			{
				while (token.IsCancellationRequested == false)
				{
					float aggregate = LoadScreenController.AggregateProgress;
					bool fullyDone = IsLoading == false && LoadScreenController.IsHandleTasksComplete;
					float target = fullyDone ? 1f : Mathf.Min(aggregate, _softCeiling);

					if (target > _displayedProgress)
					{
						_displayedProgress = Mathf.MoveTowards(_displayedProgress, target, _sliderSpeed * Time.deltaTime);
						int percent = Mathf.Clamp((int)(_displayedProgress * 100f), 0, 100);
						_slider.SetSliderValue(_displayedProgress, text: $"{percent}%");
					}

					if (fullyDone && _displayedProgress >= 1f)
						break;

					await UniTask.Yield(PlayerLoopTiming.Update, token);
				}
			}
			finally
			{
				_sliderTaskRunning = false;
			}
		}

		public void UpdateStatusText(string loadingProcess)
		{
			loadingProcess = string.IsNullOrEmpty(loadingProcess) ? string.Empty : $"( {loadingProcess} )";
			_loadingStatus.text = string.Format(_loadingStatusFormat, _baseText, loadingProcess);
		}

		private void DecreaseAlphaColor()
		{
			_group.alpha = _group.alpha - _speedAlphaColorDecrease * Time.deltaTime;
		}

		private void ResetAlphaColor()
		{
			_group.alpha = 1;
		}

		private IEnumerator ProcessLoadImageAnimation()
		{
			var waitForEndOfFrame = new WaitForEndOfFrame();
			var waitForSeconds = new WaitForSeconds(TimeBeforeDisableLoadScreen);
			IsAnimationComplete = false;
			StartCoroutine(LoadImageAnimation());
			
			while (IsLoading || LoadScreenController.IsHandleTasksComplete == false)
			{
				yield return waitForSeconds;
			}

			IsClosing = true;

			yield return new WaitUntil(() => _displayedProgress >= 1f);
			yield return new WaitForSeconds(1f);

			while (_group.alpha > 0)
			{
				DecreaseAlphaColor();
				yield return waitForEndOfFrame;
			}

			ResetAlphaColor();
			ResetLoadScreenTask();

			_loadCoroutine = null;
			IsAnimationComplete = true;
		}

		private IEnumerator LoadImageAnimation()
		{
			var waitForEndOfFrame = new WaitForEndOfFrame();

			while (IsAnimationComplete == false)
			{
				_loadImage.rectTransform.RotateAround(_loadImage.transform.position, Vector3.forward, _speedRotatingLoadImage * Time.deltaTime);
				yield return waitForEndOfFrame;
			}
		}
	}
}
