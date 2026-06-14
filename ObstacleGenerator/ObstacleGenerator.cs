#if UNITY_EDITOR
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.EditorTools.ObstacleGenerator
{
	[ExecuteAlways]
	public class ObstacleGenerator : MonoBehaviour
	{
		[HideLabel] [SerializeField] private ObstacleGeneratorEditorSettings _editorSettings;

		[PropertySpace(10)] [Title("Obstacle Generator", titleAlignment: TitleAlignments.Centered)] [SerializeField]
		private ObstacleGeneratorTool _obstacleGeneratorTool;

		private ObstacleGeneratorTool _previousObstacleTool;

		[HideLabel, ShowIf("_obstacleGeneratorTool", ObstacleGeneratorTool.CreateNewObstacle)] [SerializeField]
		private ObstacleGenerator_CreateNewObstacle _createNewObstacle;

		[HideLabel, ShowIf("_obstacleGeneratorTool", ObstacleGeneratorTool.CreateNewView)] [SerializeField]
		private ObstacleGenerator_CreateNewView _createNewView;

		[HideLabel, ShowIf("_obstacleGeneratorTool", ObstacleGeneratorTool.CreateObstaclePreset)] [SerializeField]
		private ObstacleGenerator_CreateObstaclePreset _createObstaclePreset;

		private Dictionary<ObstacleGeneratorTool, ObstacleGenerator_BaseTool> _tools = null;
		
		private Dictionary<ObstacleGeneratorTool, ObstacleGenerator_BaseTool> Tools => _tools ??= new()
		{
			{ ObstacleGeneratorTool.CreateNewObstacle, _createNewObstacle },
			{ ObstacleGeneratorTool.CreateNewView, _createNewView },
			{ ObstacleGeneratorTool.CreateObstaclePreset, _createObstaclePreset },
		};

		private void Update()
		{
			if (_previousObstacleTool != _obstacleGeneratorTool)
			{
				Tools[_previousObstacleTool].DisableTool();
				_previousObstacleTool = _obstacleGeneratorTool;
				Tools[_obstacleGeneratorTool].EnableTool();
			}

			Tools[_obstacleGeneratorTool].UpdateTool();
		}

		private void OnDestroy()
		{
			if (this == null)
				return;

			_createNewObstacle.DisableTool();
			_createNewView.DisableTool();
			_createObstaclePreset.DisableTool();
		}

		private void OnValidate()
		{
			_createNewObstacle.SetSettings(_editorSettings, transform);
			_createNewView.SetSettings(_editorSettings, transform);
			_createObstaclePreset.SetSettings(_editorSettings, transform);
		}
	}
}
#endif