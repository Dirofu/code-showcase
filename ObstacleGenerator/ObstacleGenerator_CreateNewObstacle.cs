#if UNITY_EDITOR
using System;
using Core.Scripts.Game.Battle.Obstacles;
using Sirenix.OdinInspector;
using UnityEditor;

namespace Core.EditorTools.ObstacleGenerator
{
	[Serializable]
	public class ObstacleGenerator_CreateNewObstacle : ObstacleGenerator_BaseTool
	{
		private BattleObstacle _spawnedObstacle;

		public override ObstacleGeneratorTool GetToolType() => ObstacleGeneratorTool.CreateNewObstacle;

		public override void EnableTool()
		{
		}

		public override void UpdateTool()
		{
			if (_spawnedObstacle != null)
				_spawnedObstacle.transform.position = transform.position;

			if (editorSettings.HexGridRenderer == null)
				return;

			if (_spawnedObstacle != null)
			{
				if (editorSettings.HexGridRenderer.activeInHierarchy == false)
					editorSettings.HexGridRenderer.SetActive(true);
			}
			else
			{
				if (editorSettings.HexGridRenderer.activeInHierarchy)
					editorSettings.HexGridRenderer.SetActive(false);
			}
		}

		public override void DisableTool()
		{
			if (_spawnedObstacle != null)
				Undo.DestroyObjectImmediate(_spawnedObstacle.gameObject);
		}

		[Button(ButtonSizes.Large)]
		public void SpawnNewObstacle() => SpawnObstacle_Implementation();

		private void SpawnObstacle_Implementation()
		{
			if (_spawnedObstacle != null)
			{
				EditorUtility.DisplayDialog(
					"Ошибка",
					"Сначала сохраните и удалите текущий Obstacle",
					"OK"
				);

				return;
			}

			_spawnedObstacle = UnityEngine.GameObject.Instantiate(editorSettings.ObstacleBasePrefab);
			_spawnedObstacle.name = editorSettings.ObstacleBasePrefab.name;
		}
	}
}
#endif