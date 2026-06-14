using System;
using UnityEngine;
using System.Linq;
using Core.Scripts.Scene;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

namespace Core.Scripts.UI
{
	//[CreateAssetMenu(fileName = "new LoadScreenAddressablePath", menuName = "Configs/Create LoadScreenAddressablePath")]
	public class LoadScreenAddressablePath : ScriptableObject
	{
		[SerializeField] private List<ImageToSceneConnector> _connector;

		private Dictionary<LocationSceneType, ImageToSceneConnector> _cachedDictionary;

		public IReadOnlyDictionary<LocationSceneType, ImageToSceneConnector> Connector
		{
			get
			{
				if (_cachedDictionary == null)
				{
					_cachedDictionary = _connector
						.Where(c => c != null)
						.ToDictionary(c => c.SceneType, c => c);
				}
				return _cachedDictionary;
			}
		}
	}

	[Serializable]
	public class ImageToSceneConnector
	{
		[SerializeField] private LocationSceneType _sceneType;
		[SerializeField] private List<AssetReference> _backgrounds;

		public LocationSceneType SceneType => _sceneType;
		public IReadOnlyList<AssetReference> Backgrounds => _backgrounds;
	}
}