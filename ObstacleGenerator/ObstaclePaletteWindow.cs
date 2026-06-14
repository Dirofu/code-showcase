#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using System.IO;
using Core.Scripts.Game.Battle.Obstacles;
using Sirenix.Utilities.Editor;
using Sirenix.Utilities;

public class ObstaclePaletteWindow : OdinEditorWindow
{
	public static void Open()
	{
		GetWindow<ObstaclePaletteWindow>("Obstacle Palette").Show();
	}

	[FolderPath(ParentFolder = "Assets")] [LabelText("Folder with obstacles")] [OnValueChanged("RefreshPrefabs")]
	public string folderPath = "Core/Prefabs/Game/Battle/Obstacles/ObstacleBlockers";

	[Range(60, 200)] [LabelText("Element size")]
	public float iconSize = 100;

	[Range(.6f, 3f)] [LabelText("Element scale")]
	public float iconScale = 1f;
	
	[Space] [ShowInInspector, HideInEditorMode] [Searchable]
	private List<PrefabItem> prefabs = new List<PrefabItem>();

	public int IconDefaultSize => 100;
	public int TextDefaultSize => 15;
	public int ElementDefaultScale => 17;

	protected override void OnEnable()
	{
		base.OnEnable();
		RefreshPrefabs();
	}

	[Button("Refresh", ButtonSizes.Medium)]
	private void RefreshPrefabs()
	{
		prefabs.Clear();
		string fullPath = "Assets/" + folderPath;

		if (Directory.Exists(fullPath) == false) return;

		string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { fullPath });
		foreach (var guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			BattleObstacle obstacle = AssetDatabase.LoadAssetAtPath<BattleObstacle>(path);

			if (obstacle != null)
				prefabs.Add(new PrefabItem(obstacle, this));
		}
	}

	[OnInspectorGUI]
	private void DrawPrefabsGrid()
	{
		if (prefabs == null || prefabs.Count == 0)
			return;

		float viewWidth = EditorGUIUtility.currentViewWidth - 20;
		int columnCount = Mathf.Max(1, Mathf.FloorToInt(viewWidth / (iconSize + 5)));

		GUILayout.BeginVertical();
		int currentColumn = 0;

		foreach (var item in prefabs)
		{
			if (currentColumn == 0)
				GUILayout.BeginHorizontal();

			item.DrawItem();

			currentColumn++;
			if (currentColumn >= columnCount)
			{
				GUILayout.EndHorizontal();
				currentColumn = 0;
			}
		}

		if (currentColumn != 0)
			GUILayout.EndHorizontal();

		GUILayout.EndVertical();
	}

	public class PrefabItem
	{
		[HideInInspector] public BattleObstacle Prefab;
		private string Name;
		private ObstaclePaletteWindow parent;

		public PrefabItem(BattleObstacle prefab, ObstaclePaletteWindow parentWindow)
		{
			Prefab = prefab;
			Name = prefab.name;
			parent = parentWindow;
		}

		[OnInspectorGUI]
		public void DrawItem()
		{
			float size = parent.iconSize;
			Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(size), GUILayout.Height(size + 40));
			Rect iconRect = rect.AlignTop(size);
			Rect labelRect = rect.AlignBottom(40);

			EditorGUI.DrawRect(iconRect, new Color(0.2f, 0.2f, 0.2f, 1.0f));
			SirenixEditorGUI.DrawBorders(iconRect, 1, Color.black);

			GUI.BeginGroup(iconRect);

			Handles.color = Color.orange;
			Handles.DrawSolidDisc(new Vector2(size / 2, size / 2), Vector3.forward, 5f);

			Handles.color = new Color(1, 1, 1, 0.1f);
			Handles.DrawLine(new Vector3(0, size / 2), new Vector3(size, size / 2));
			Handles.DrawLine(new Vector3(size / 2, 0), new Vector3(size / 2, size));

			float currentVisualScale = (parent.ElementDefaultScale * (size / parent.IconDefaultSize)) / parent.iconScale;
			float polygonRadius = currentVisualScale * 1f; 
			float thickness = Mathf.Max(1f, 3f / parent.iconScale);

			foreach (var point in Prefab.BoundsPoints)
			{
				Vector2 screenCenter = WorldToIconPos(point.position, size, currentVisualScale);
				DrawPolygon(screenCenter, polygonRadius, 6, Color.red, thickness);
			}

			GUI.EndGroup();

			GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
			style.alignment = TextAnchor.MiddleCenter;
			style.fontSize = (int)(parent.TextDefaultSize * (parent.iconSize / parent.IconDefaultSize));
			string name = Name.Split("_")[^1];
			GUI.Label(labelRect, name, style);

			Event evt = Event.current;
			if (iconRect.Contains(evt.mousePosition))
			{
				EditorGUI.DrawRect(iconRect, new Color(1, 1, 1, 0.05f));

				if (evt.type == EventType.MouseDrag)
				{
					DragAndDrop.PrepareStartDrag();
					DragAndDrop.objectReferences = new Object[] { Prefab.gameObject };
					DragAndDrop.StartDrag(Name);
					evt.Use();
				}

				if (evt.type == EventType.MouseDown)
				{
					Selection.activeObject = Prefab.gameObject;
					evt.Use();
				}
			}
		}

		private Vector2 WorldToIconPos(Vector3 worldPos, float iconSize, float scale)
		{
			float u = (iconSize / 2f) + (worldPos.x * scale);
			float v = (iconSize / 2f) - (worldPos.z * scale);

			return new Vector2(u, v);
		}
		
		private void DrawPolygon(Vector2 center, float radius, int sides, Color color, float thickness = 2f)
		{
			if (sides < 3)
				return;

			Vector3[] points = new Vector3[sides + 1];

			float angleStep = 360f / sides;
			float offset = (sides == 6) ? -30f : (360f / sides / 2f);

			for (int i = 0; i <= sides; i++)
			{
				float angleRadius = Mathf.Deg2Rad * (angleStep * i - offset);

				points[i] = new Vector3(
					center.x + radius * Mathf.Cos(angleRadius),
					center.y + radius * Mathf.Sin(angleRadius),
					0
				);
			}

			Handles.color = color;
			Handles.DrawAAPolyLine(thickness, points);
		}
	}
}
#endif