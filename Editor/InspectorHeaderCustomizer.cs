using System;
using System.Diagnostics;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UniInspectorHeaderCustomizer
{
	[InitializeOnLoad]
	internal static class InspectorHeaderCustomizer
	{
		private const int REMOVE_DATA_PATH_COUNT = 6;

		private static readonly Type MONO_SCRIPT_TYPE = typeof( MonoScript );

		static InspectorHeaderCustomizer()
		{
			Editor.finishedDefaultHeaderGUI += HeaderGUI;
		}

		private static void HeaderGUI( Editor editor )
		{
			var type = editor.target.GetType();

			if ( type == MONO_SCRIPT_TYPE ) return;

			using ( new EditorGUILayout.HorizontalScope() )
			{
				var path       = AssetDatabase.GetAssetPath( editor.target );
				var oldEnabled = GUI.enabled;

				GUI.enabled = !string.IsNullOrWhiteSpace( path );

				if ( GUILayout.Button( "Explorer" ) )
				{
					if ( AssetDatabase.IsValidFolder( path ) )
					{
						var dataPath   = Application.dataPath;
						var startIndex = dataPath.Length - REMOVE_DATA_PATH_COUNT;
						dataPath = dataPath.Remove( startIndex, REMOVE_DATA_PATH_COUNT );
						Process.Start( dataPath + path );
					}
					else
					{
						EditorUtility.RevealInFinder( path );
					}
				}

				GUI.enabled = oldEnabled;

				if ( GUILayout.Button( "Lock" ) )
				{
					var tracker = ActiveEditorTracker.sharedTracker;
					tracker.isLocked = !tracker.isLocked;
					tracker.ForceRebuild();
				}

				if ( GUILayout.Button( "Debug" ) )
				{
					var window          = Resources.FindObjectsOfTypeAll<EditorWindow>();
					var inspectorWindow = ArrayUtility.Find( window, c => c.GetType().Name == "InspectorWindow" );

					if ( inspectorWindow == null ) return;

					var inspectorType = inspectorWindow.GetType();
					var tracker       = ActiveEditorTracker.sharedTracker;
					var isNormal      = tracker.inspectorMode == InspectorMode.Normal;
					var methodName    = isNormal ? "SetDebug" : "SetNormal";
					var attr          = BindingFlags.NonPublic | BindingFlags.Instance;
					var methodInfo    = inspectorType.GetMethod( methodName, attr );

					methodInfo?.Invoke( inspectorWindow, null );
					tracker.ForceRebuild();
				}
			}
		}
	}
}