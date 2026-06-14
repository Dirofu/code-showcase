#if UNITY_EDITOR
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.EditorTools.ObstacleGenerator
{
	[Serializable]
	public abstract class ObstacleGenerator_BaseTool
	{
		protected ObstacleGeneratorEditorSettings editorSettings;
		protected Transform transform;

		public abstract ObstacleGeneratorTool GetToolType();
		
		public void SetSettings(ObstacleGeneratorEditorSettings editorSettingsIn, Transform transformIn)
		{
			editorSettings = editorSettingsIn;
			transform = transformIn;
		}
	
		public abstract void EnableTool();
		public abstract void UpdateTool();
		public abstract void DisableTool();

		protected T Instantiate<T>(T original, Transform parent = null) where T : Object
		{
			return Object.Instantiate<T>(original, parent, false);
		}
	}
}
#endif