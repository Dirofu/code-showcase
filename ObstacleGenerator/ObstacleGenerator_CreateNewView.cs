#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Core.Scripts.Game.Battle.Obstacles;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Core.EditorTools.ObstacleGenerator
{
	[Serializable]
	public class ObstacleGenerator_CreateNewView : ObstacleGenerator_BaseTool
	{
		private BattleObstacle _spawnedObstacle;
		private BattleObstacleView _spawnedView;
		private List<GameObject> _tempHexGrid = new();
		
		[ValueDropdown(nameof(GetPrefabs))]
		[SerializeField]
		private GameObject _selectedPrefab;

		public override ObstacleGeneratorTool GetToolType() => ObstacleGeneratorTool.CreateNewView;
		
		[Button(ButtonSizes.Large)]
		public void SpawnObstacleToCreateVisual() => SpawnObstacleToCreateView_Implementation();

		public override void EnableTool()
		{
			if (editorSettings.HexGridRenderer.activeInHierarchy == true)
				editorSettings.HexGridRenderer.SetActive(false);
		}

		public override void UpdateTool()
		{
			if (_spawnedObstacle != null)
			{
				_spawnedObstacle.transform.position = transform.position;
				_spawnedObstacle.transform.rotation = transform.rotation;
			}
			
			if (_spawnedView != null)
			{
				_spawnedView.transform.position = transform.position;
				_spawnedView.transform.rotation = transform.rotation;
			}
			
			if (editorSettings.HexGridRenderer == null)
				return;

			if (_spawnedObstacle != null)
			{
				if (_spawnedView == null)
				{
					Undo.DestroyObjectImmediate(_spawnedObstacle.gameObject);
					return;
				}
				
				if (_spawnedObstacle.BoundsPoints.Count == _tempHexGrid.Count)
					return;

				foreach (var bound in _spawnedObstacle.BoundsPoints)
				{
					var hexTile = Instantiate(editorSettings.HexTile, transform);
					hexTile.transform.position = bound.transform.position;
					_tempHexGrid.Add(hexTile);
				}
			}
			else
			{
				if (_tempHexGrid.Count == 0)
					return;

				foreach (var hex in _tempHexGrid)
					Undo.DestroyObjectImmediate(hex);

				_tempHexGrid.Clear();
			}
		}
				
		public override void DisableTool()
		{
			if (_spawnedObstacle != null)
				Undo.DestroyObjectImmediate(_spawnedObstacle.gameObject);

			if (_spawnedView != null)
				Undo.DestroyObjectImmediate(_spawnedView.gameObject);
			
			if (_tempHexGrid.Count > 0)
			{
				foreach (var hex in _tempHexGrid)
					Undo.DestroyObjectImmediate(hex);

				_tempHexGrid.Clear();
			}
		}

		private void SpawnObstacleToCreateView_Implementation()
		{
			if (_selectedPrefab == null)
			{
				EditorUtility.DisplayDialog(
					"Ошибка",
					"Сначала выберете готовый Obstacle",
					"OK"
				);

				return;
			}

			if (_spawnedObstacle != null)
				Undo.DestroyObjectImmediate(_spawnedObstacle.gameObject);

			if (_spawnedObstacle != null)
				Undo.DestroyObjectImmediate(_spawnedView.gameObject);

			if (_tempHexGrid.Count > 0)
			{
				foreach (var hex in _tempHexGrid)
					Undo.DestroyObjectImmediate(hex);

				_tempHexGrid.Clear();
			}
			
			_spawnedObstacle = Instantiate(_selectedPrefab, transform).GetComponent<BattleObstacle>();
			_spawnedObstacle.name = _selectedPrefab.name; 
			
			string selectedPrefabName = _selectedPrefab.name.Split('_')[2];
			_spawnedView = Instantiate(editorSettings.ObstacleBaseViewPrefab);
			_spawnedView.name = $"{editorSettings.ObstacleBaseViewPrefab.name}_{selectedPrefabName}";
			EditorUtility.SetDefaultParentObject(_spawnedView.gameObject); 
		}

		private static IEnumerable<GameObject> GetPrefabs()
		{
			var guids = AssetDatabase.FindAssets("t:Prefab",
				new[] { "Assets/Core/Prefabs/Game/Battle/Obstacles/ObstacleBlockers" });
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				yield return AssetDatabase.LoadAssetAtPath<GameObject>(path);
			}
		}
	}
}
#endif