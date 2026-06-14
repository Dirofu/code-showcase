#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonTools.ExternalTypes;
using CommonTools.Plugins.HexAgony;
using CommonTools.Plugins.HexAgony.HexGrid;
using CommonTools.Plugins.HexAgony.UnityComponents;
using Core.Scripts.Game.Battle.Obstacles;
using Core.Scripts.NetworkConnection.Package;
using Core.Scripts.NetworkConnection.Packages;
using Core.Scripts.Scene;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Networking;
using Material = UnityEngine.Material;
using Object = UnityEngine.Object;

namespace Core.EditorTools.ObstacleGenerator
{
	[Serializable]
	public class ObstacleGenerator_CreateObstaclePreset : ObstacleGenerator_BaseTool
	{
		[SerializeField] private bool _showFieldsSettings;

		[ShowIf("_showFieldsSettings")] [SerializeField]
		private HexGridController _hexGridControllerPrefab;

		[ShowIf("_showFieldsSettings")] [SerializeField]
		private Material _availableHexGridMaterial;
		
		[ShowIf("_showFieldsSettings")] [SerializeField]
		private Material _closedHexGridMaterial;

		[ShowIf("_showFieldsSettings")] [SerializeField]
		private Material _obstaclePlacedHexGridMaterial;
		
		[ShowIf("_showFieldsSettings")] [SerializeField]
		private int _countColumnsToClose = 3;		
		
		[ShowIf("_showFieldsSettings")] [SerializeField]
		private GameObject _camera;

		[ShowIf("_showFieldsSettings")] [SerializeField]
		private HexGridRendererConfig _config;
		
		[ShowIf("_showFieldsSettings"), FolderPath(ParentFolder = "Assets"), LabelText("Presets folder")]
		public string _presetFolder = "Core\\Prefabs\\Game\\Battle\\Obstacles\\ObstaclePresets";

		[ShowIf("_showFieldsSettings"), LabelText("Battle locations endpoint")]
		[SerializeField]
		private string _battleLocationsEndpoint;

		[SerializeField, ValueDropdown(nameof(GetAvailableLocations))]
		private List<LocationSceneType> _locationsPresetActive;

		private Vector3 _previousPosition;
		private float _previousRotation;
		private HexGridController _hexGridController;
		
		private List<BattleObstacle> _obstacles = new();
		private List<HexTileData> _tiles = new();
		private List<HexTileData> _tilesToClose = new();
		
		private List<ToolBattleSceneDTO> _scenes = new();
		
		public override ObstacleGeneratorTool GetToolType() => ObstacleGeneratorTool.CreateObstaclePreset;
		
		public async override void EnableTool()
		{
			_hexGridController = Instantiate(_hexGridControllerPrefab);
			_hexGridController.transform.position = transform.position;
			_hexGridController.transform.rotation = Quaternion.identity;
			
			editorSettings.BaseToolCamera.SetActive(false);
			_camera.SetActive(true);

			_hexGridController.GetComponentInChildren<HexGridRenderer>().InitializeGrid(_config, true);

			_tiles = _hexGridController.Tiles.ToList();
			_tilesToClose = new();
			
			HexTileData lastTile = _tiles[^1];
			
			foreach (var tile in _tiles)
			{
				if (tile.Tile.GridPosition.X < _countColumnsToClose ||
				    lastTile.Tile.GridPosition.X - tile.Tile.GridPosition.X < _countColumnsToClose)
				{
					_tilesToClose.Add(tile);
				}
			}

			foreach (var tile in _tilesToClose)
			{
				tile.Renderer.MeshRenderer.material = _closedHexGridMaterial;
			}
			
			SceneView.duringSceneGui += OnSceneGUI;

			Debug.Log($"<color=#CD5C5C>Start matching scenes from server</color>");
			string json = await GetScenesData();
			_scenes = JsonConvert.DeserializeObject<ToolBattleSceneAnswerDTO>(json).data;
			Debug.Log($"<color=#ADFF2F>Complete matching scenes from server</color>");
		}
		
		private async UniTask<string> GetScenesData()
		{
			if (string.IsNullOrWhiteSpace(_battleLocationsEndpoint))
				return "{\"data\":[]}";
			
			using UnityWebRequest request = UnityWebRequest.Get(_battleLocationsEndpoint);
			
			await request.SendWebRequest();

			if (request.result != UnityWebRequest.Result.Success)
			{
				Debug.LogError($"REST error: {request.error}");
				return string.Empty;
			}

			return request.downloadHandler.text;
		}
		
		public override void UpdateTool()
		{
			if (_hexGridController == null || _hexGridController.Tiles.Count == 0)
			{
				_hexGridController = (HexGridController)GameObject.FindObjectOfType(typeof(HexGridController));
				
				if (_hexGridController)
					GameObject.DestroyImmediate(_hexGridController.gameObject);
	
				EnableTool();
			}
		}
		
		public override void DisableTool()
		{
			editorSettings.BaseToolCamera.SetActive(true);
			_camera.SetActive(false);
			
			if (_hexGridController != null)
				Undo.DestroyObjectImmediate(_hexGridController.gameObject);
			
			foreach (var obstacle in _obstacles)
				if (obstacle != null)
					Undo.DestroyObjectImmediate(obstacle.gameObject);
			
			_obstacles.Clear();
			
			SceneView.duringSceneGui -= OnSceneGUI;
		}

		[Button(ButtonSizes.Gigantic)]
		private void OpenPrefabsWindow()
		{
			ObstaclePaletteWindow.Open();
		}
		
		[PropertySpace(20)]
		[Tooltip("New presets created after 21.01.2026")]
		[HorizontalGroup("LoadButtons")]
		[Button(ButtonSizes.Large), GUIColor(1f, 1f, 0f)]
		private void LoadPreset()
		{
			string json = SelectFileToLoad();
		
			if (string.IsNullOrEmpty(json))
				return;
			
			WSPackageObstaclePreset wsPackageObstaclePreset = null;
			wsPackageObstaclePreset = JsonConvert.DeserializeObject<WSPackageObstaclePreset>(json);
			
			foreach (var sceneID in wsPackageObstaclePreset.locationIDs)
			{
				if (_scenes.Select(scene => scene.locationId == sceneID.id).FirstOrDefault() == false)
				{
					Debug.LogError($"Scene with type: {sceneID.type} is not found on server");
					continue;
				}

				if (Enum.TryParse(sceneID.type, out LocationSceneType type) == false)
				{
					Debug.LogError($"Scene with type: {sceneID.type} cant parse to {nameof(LocationSceneType)}");
					continue;
				}
				
				_locationsPresetActive.Add(type);
			}

			var preset = wsPackageObstaclePreset;
			
			foreach (var obstacle in preset.obstacles)
			{
				GameObject prefab = LoadAsset(obstacle.obstacleBundleName);
				
				var spawnedObstacle = ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<BattleObstacle>();

				if (_hexGridController.TryGetTileDataByPosition(obstacle.position.ToHexGridOffsetVector(), out _) == false)
				{
					Debug.LogError($"Obstacle cant be spawned. Cell[{obstacle.position.x} | {obstacle.position.y}] not find. Another side battlefield");
					GameObject.DestroyImmediate(spawnedObstacle.gameObject);
					continue;
				}
				
				spawnedObstacle.transform.position = _hexGridController.TileGridPositionToWorldPoint(obstacle.position.ToHexGridOffsetVector());
				spawnedObstacle.transform.position = new Vector3(spawnedObstacle.transform.position.x, -.4f, spawnedObstacle.transform.position.z);
				spawnedObstacle.transform.eulerAngles = new Vector3(0, obstacle.rotation, 0);
				spawnedObstacle.SetInfo(obstacle.mAttackIsPossibleThrough, obstacle.pAttackIsPossibletThrough, obstacle.skillAttackIsPossibleThrough);
				UpdateObstacleTiles(ref spawnedObstacle);
				
				if (string.IsNullOrEmpty(obstacle.obstacleViewOverride) == false)
				{
					GameObject prefabView = LoadAsset(obstacle.obstacleViewOverride);
					spawnedObstacle.SetViewEditor(prefabView);
				}

				_obstacles.Add(spawnedObstacle);
			}
			
			UpdateHexGrid();
		}

		[PropertySpace(10)]
		[Button(ButtonSizes.Large), GUIColor(1f, 0f, 0f)]
		private void ClearPreset()
		{
			foreach (var obstacle in _obstacles)
			{
				if (obstacle != null)
					GameObject.DestroyImmediate(obstacle.gameObject);
			}
			
			_obstacles.Clear();
			_locationsPresetActive.Clear();
			UpdateHexGrid();
		}
		
		[PropertySpace(20)]
		[Button(ButtonSizes.Gigantic), GUIColor(0.4f, 1f, 0.4f)]
		private void SavePreset()
		{
			string fullPath = "Assets\\" + _presetFolder;

			if (Directory.Exists(fullPath) == false) 
				return;

			List<WSPackageBattleObstacle> battleObstacles = new();

			_obstacles = _obstacles.Where(obstacle => obstacle != null).ToList();
			
			if (_obstacles.Count == 0)
				return;
			
			foreach (var obstacle in _obstacles)
			{
				if (obstacle == null)
					continue;
				
				List<Vector2IntDTO> cellPositions = new();
				HexGridOffsetVector gridPos = _hexGridController.WorldVector2ToHexOffsetVector(obstacle.transform.position.x, obstacle.transform.position.z);

				foreach (var point in obstacle.BoundsPoints)
				{
					HexGridOffsetVector pointPosition = _hexGridController.WorldVector2ToHexOffsetVector(point.position.x, point.position.z);
					cellPositions.Add(new Vector2IntDTO(pointPosition.X, pointPosition.Y));
				}

				var newObstacle = new WSPackageBattleObstacle()
				{
					mAttackIsPossibleThrough = obstacle.MAttackIsPossibleThrough,
					pAttackIsPossibletThrough = obstacle.PAttackIsPossibleThrough,
					skillAttackIsPossibleThrough = obstacle.SkillAttackIsPossibleThrough,
					position = new Vector2IntDTO(gridPos.X, gridPos.Y),
					rotation = (int)obstacle.transform.eulerAngles.y,
					type = obstacle.name.Split("_")[2],
					obstacleBundleName = GetAddress(obstacle),
					cells = cellPositions
				};
				
				if (obstacle.View != null)
					newObstacle.obstacleViewOverride = GetAddress(obstacle.View);
				
				battleObstacles.Add(newObstacle);
			}

			var dto = new WSPackageObstaclePreset();
			dto.obstacles = battleObstacles;
			dto.locationIDs = new();
			
			foreach (var sceneType in _locationsPresetActive)
			{
				var location = new WSPackageGetLocationIDs()
				{
					id = (int)sceneType,
					type = sceneType.ToString()
				};
				
				dto.locationIDs.Add(location);
			}
			
			string jsonResult = dto.ToString();
			
			string path = EditorUtility.SaveFilePanel(
				"Save ObstaclePreset in JSON",
				fullPath,
				"ObstaclePreset",
				"json"
			);

			if (string.IsNullOrEmpty(path))
				return;

			File.WriteAllText(path, jsonResult);
			AssetDatabase.Refresh();
		}
		
		private IEnumerable<LocationSceneType> GetAvailableLocations()
		{
			foreach (var scene in _scenes)
			{
				if (Enum.TryParse<LocationSceneType>(scene.type, ignoreCase: true, out var type))
					yield return type;
			}
		}
		
		private string SelectFileToLoad()
		{
			string fullPath = "Assets\\" + _presetFolder;

			if (Directory.Exists(fullPath) == false) 
				return string.Empty;
			
			string path = EditorUtility.OpenFilePanel(
				"Select JSON Preset file",
				fullPath,
				"json"
			);
			
			if (string.IsNullOrEmpty(path))
				return string.Empty;
			
			return File.ReadAllText(path);
		}
		
		private void UpdateObstacleTiles(ref BattleObstacle obstacle)
		{
			obstacle.Tiles.Clear();
			
			foreach (var point in obstacle.BoundsPoints)
			{
				HexGridOffsetVector position = _hexGridController.WorldVector2ToHexOffsetVector(point.position.x, point.position.z);
					
				if (_hexGridController.TryGetTileDataByPosition(position, out HexTileData data))
					obstacle.Tiles.Add(data);
			}
		}

		private GameObject LoadAsset(string address)
		{
			foreach (var entry in AddressableAssetSettingsDefaultObject
				         .Settings.groups
				         .SelectMany(g => g.entries))
			{
				if (entry.address == address)
				{
					string path = entry.AssetPath;
					return AssetDatabase.LoadAssetAtPath<GameObject>(path);
				}
			}

			Debug.LogError($"Address not found: {address}");
			return null;
		}
		
		private void OnSceneGUI(SceneView sceneView)
		{
			Event e = Event.current;

			if (DragAndDrop.objectReferences.Length > 0)
			{
				HandlePaletteDrag(e, sceneView);
				return;
			}

			HandleExistingObjectSnapping(e, sceneView);
		}

		private void HandlePaletteDrag(Event e, SceneView sceneView)
		{
			Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
			Plane plane = new Plane(Vector3.up, Vector3.zero);

			if (plane.Raycast(ray, out float distance))
			{
				Vector3 worldHitPos = ray.GetPoint(distance);

				if (e.type == EventType.DragPerform)
				{
					DragAndDrop.AcceptDrag();
					GameObject draggedObject = DragAndDrop.objectReferences[0] as GameObject;

					if (draggedObject != null)
					{
						HandlePrefabDrop(draggedObject, worldHitPos);
					}
					e.Use();
				}
			}
		}
		
		private void HandleExistingObjectSnapping(Event e, SceneView sceneView)
		{
			if (Selection.activeGameObject == null) 
				return;

			var obstacle = Selection.activeGameObject.GetComponent<BattleObstacle>();
			
			if (obstacle == null) 
				return;

			Transform targetTf = Selection.activeTransform;
			
			if (targetTf == null)
				return;
			
			if (e.type == EventType.MouseUp || e.type == EventType.MouseDown)
			{
				foreach (var point in obstacle.BoundsPoints)
				{
					HexGridOffsetVector gridPos = _hexGridController.WorldVector2ToHexOffsetVector(point.position.x, point.position.z);
					
					if (_hexGridController.TryGetTileDataByPosition(gridPos, out HexTileData tileData))
					{
						Material currentMaterial = tileData.Renderer.MeshRenderer.sharedMaterial;

						if (obstacle.Tiles.Contains(tileData) == false && (currentMaterial == _closedHexGridMaterial || currentMaterial == _obstaclePlacedHexGridMaterial)) 
						{
							targetTf.position = _previousPosition;
							targetTf.eulerAngles = new Vector3(0, _previousRotation, 0);
							return;
						}
					}
				}
				
				_previousPosition = targetTf.position;
				_previousRotation = targetTf.eulerAngles.y;
				UpdateHexGrid();
				UpdateObstacleTiles(ref obstacle);
			}

			if (e.type == EventType.MouseUp)
			{
				float currentRotation = targetTf.eulerAngles.y;
				float snappedRotation = Mathf.Round(currentRotation / 60f) * 60f;
				Undo.RecordObject(targetTf, "Obstacle Rotate");
				targetTf.eulerAngles = new Vector3(0, snappedRotation, 0);
				_previousRotation = targetTf.eulerAngles.y;
				UpdateHexGrid();
				EditorApplication.QueuePlayerLoopUpdate();
			}
			
			if (e.type == EventType.MouseDrag || e.type == EventType.MouseUp)
			{
				HexGridOffsetVector gridPos = _hexGridController.WorldVector2ToHexOffsetVector(targetTf.position.x, targetTf.position.z);
				Vector3 snappedPreviewPos = _hexGridController.TileGridPositionToWorldPoint(gridPos);
				snappedPreviewPos.y += .5f;
				
				if (Vector3.Distance(targetTf.position, snappedPreviewPos) > 0.05f)
				{
					Undo.RecordObject(targetTf, "Obstacle Snap");
					targetTf.position = snappedPreviewPos;

					if (e.type == EventType.MouseUp)
					{
						_previousPosition = targetTf.position;
						UpdateHexGrid();
					}
					
					EditorApplication.QueuePlayerLoopUpdate();
				}
			}
		}

		private void UpdateHexGrid()
		{
			foreach (var tile in _tiles)
			{
				if (_tilesToClose.Contains(tile))
					tile.Renderer.MeshRenderer.sharedMaterial = _closedHexGridMaterial;
				else
					tile.Renderer.MeshRenderer.sharedMaterial = _availableHexGridMaterial;
			}

			_obstacles = _obstacles.Where(obstacle => obstacle != null).ToList();

			foreach (var obstacle in _obstacles)
			{
				foreach (var point in obstacle.BoundsPoints)
				{
					HexGridOffsetVector pointPosition = _hexGridController.WorldVector2ToHexOffsetVector(point.position.x, point.position.z);
					if (_hexGridController.TryGetTileDataByPosition(pointPosition, out HexTileData tileData))
					{
						tileData.Renderer.MeshRenderer.sharedMaterial = _obstaclePlacedHexGridMaterial;
					}
				}
				
			}
		}

		private void HandlePrefabDrop(GameObject prefab, Vector3 position)
		{
			HexGridOffsetVector gridPos = _hexGridController.WorldVector2ToHexOffsetVector(position.x, position.z);
			var prefabObstacle = prefab.GetComponent<BattleObstacle>();

			if (prefabObstacle == null)
				return;
			
			foreach (var point in prefabObstacle.BoundsPoints)
			{
				HexGridOffsetVector pointPosition = _hexGridController.WorldVector2ToHexOffsetVector(position.x + point.position.x, position.z + point.position.z);
				if (_hexGridController.TryGetTileDataByPosition(pointPosition, out HexTileData tileData))
				{
					Material material = tileData.Renderer.MeshRenderer.sharedMaterial;
				
					if (material == _closedHexGridMaterial ||  material == _obstaclePlacedHexGridMaterial)
						return;
				}
			}
			
			GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
			position = _hexGridController.TileGridPositionToWorldPoint(gridPos);
			instance.transform.position = position;
			_previousPosition = position;
			var obstacle = instance.GetComponent<BattleObstacle>();
			UpdateObstacleTiles(ref obstacle);
			_obstacles.Add(obstacle);
			Undo.RegisterCreatedObjectUndo(instance, "Spawn Obstacle");
			UpdateHexGrid();
			Selection.activeObject = instance;
		}

		public static string GetAddress(Object obj)
		{
			Object prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(obj);
			string assetPath = AssetDatabase.GetAssetPath(prefabSource);
			string guid = AssetDatabase.AssetPathToGUID(assetPath);
			
			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        
			if (settings != null)
			{
				AddressableAssetEntry entry = settings.FindAssetEntry(guid);
            
				if (entry != null)
				{
					return entry.address;
				}
			}
        
			return "Not Addressable";
		}
	}
}
#endif
