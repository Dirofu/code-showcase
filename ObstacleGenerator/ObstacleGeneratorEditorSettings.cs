#if UNITY_EDITOR
using System;
using Core.Scripts.Game.Battle.Obstacles;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.EditorTools.ObstacleGenerator
{
	[Serializable]
	public class ObstacleGeneratorEditorSettings
	{
		[SerializeField] private bool _showEditorSettings;

		[Title("Editor Settings", titleAlignment: TitleAlignments.Centered), ShowIf("_showEditorSettings")]
		[Required]
		[SerializeField]
		private BattleObstacle _obstacleBasePrefab;

		[ShowIf("_showEditorSettings")] [Required] [SerializeField]
		private BattleObstacleView _obstacleBaseViewPrefab;

		[ShowIf("_showEditorSettings")] [Required] [SerializeField]
		private GameObject _hexTile;

		[ShowIf("_showEditorSettings")] [Required] [SerializeField]
		private GameObject _hexGridRenderer;
		
		[ShowIf("_showEditorSettings")] [Required] [SerializeField]
		private GameObject _baseToolCamera;

		public BattleObstacle ObstacleBasePrefab => _obstacleBasePrefab;
		public BattleObstacleView ObstacleBaseViewPrefab => _obstacleBaseViewPrefab;
		public GameObject HexTile => _hexTile;
		public GameObject HexGridRenderer => _hexGridRenderer;
		public GameObject BaseToolCamera => _baseToolCamera;
	}
}
#endif