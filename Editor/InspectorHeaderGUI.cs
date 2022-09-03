using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Kogane.Internal
{
    [InitializeOnLoad]
    internal static class InspectorHeaderGUI
    {
        private static readonly Type PROPERTY_EDITOR_TYPE = typeof( Editor ).Assembly.GetType( "UnityEditor.PropertyEditor" );

        static InspectorHeaderGUI()
        {
            Editor.finishedDefaultHeaderGUI -= OnGUI;
            Editor.finishedDefaultHeaderGUI += OnGUI;
        }

        private static void OnGUI( Editor editor )
        {
            using ( new EditorGUILayout.HorizontalScope() )
            {
                DrawLockButton();
                DrawDebugButton();
                DrawPropertiesButton();
                DrawPasteComponentAsNew( editor );
                DrawRevealInFinderButton( editor );
            }

            var oldEnabled = GUI.enabled;
            GUI.enabled = editor.targets.All( x => !EditorUtility.IsPersistent( x ) );

            try
            {
                using ( new EditorGUILayout.HorizontalScope() )
                {
                    DrawExpandAllComponentsButton();
                    DrawCollapseAllComponentsButton();
                }
            }
            finally
            {
                GUI.enabled = oldEnabled;
            }
        }

        private static void DrawLockButton()
        {
            var oldEnabled = GUI.enabled;
            GUI.enabled = true;

            try
            {
                var tracker = ActiveEditorTracker.sharedTracker;

                if ( !GUILayout.Button( tracker.isLocked ? "Lock" : "Unlock", EditorStyles.miniButtonLeft ) ) return;

                tracker.isLocked = !tracker.isLocked;
                tracker.ForceRebuild();
            }
            finally
            {
                GUI.enabled = oldEnabled;
            }
        }

        private static void DrawDebugButton()
        {
            var oldEnabled = GUI.enabled;
            GUI.enabled = true;

            try
            {
                var tracker  = ActiveEditorTracker.sharedTracker;
                var isNormal = tracker.inspectorMode == InspectorMode.Normal;

                if ( !GUILayout.Button( isNormal ? "Normal" : "Debug", EditorStyles.miniButtonMid ) ) return;

                var editorWindowArray = Resources.FindObjectsOfTypeAll<EditorWindow>();
                var inspectorWindow   = ArrayUtility.Find( editorWindowArray, x => x.GetType().Name == "InspectorWindow" );

                if ( inspectorWindow == null ) return;

                var inspectorWindowType = inspectorWindow.GetType();
                var propertyEditorType  = inspectorWindowType.BaseType;

                Debug.Assert( propertyEditorType != null, nameof( propertyEditorType ) + " != null" );

                var propertyInfo = propertyEditorType.GetProperty
                (
                    name: "inspectorMode",
                    bindingAttr: BindingFlags.Public | BindingFlags.Instance
                );

                Debug.Assert( propertyInfo != null, nameof( propertyInfo ) + " != null" );

                // 1 フレーム遅らせないと以下のエラーが発生する
                // EndLayoutGroup: BeginLayoutGroup must be called first.
                EditorApplication.delayCall += () =>
                {
                    propertyInfo.SetValue( inspectorWindow, isNormal ? InspectorMode.Debug : InspectorMode.Normal );
                    tracker.ForceRebuild();
                };
            }
            finally
            {
                GUI.enabled = oldEnabled;
            }
        }

        private static void DrawPropertiesButton()
        {
            var oldEnabled = GUI.enabled;
            GUI.enabled = true;

            try
            {
                if ( GUILayout.Button( "Properties...", EditorStyles.miniButtonMid ) )
                {
                    EditorApplication.ExecuteMenuItem( "Assets/Properties..." );
                }
            }
            finally
            {
                GUI.enabled = oldEnabled;
            }
        }

        private static void DrawPasteComponentAsNew( Editor editor )
        {
            var gameObject   = editor.target as GameObject;
            var isGameObject = gameObject != null;
            var oldEnabled   = GUI.enabled;

            GUI.enabled = isGameObject;

            try
            {
                if ( GUILayout.Button( "Paste Component As New", EditorStyles.miniButtonMid ) )
                {
                    ComponentUtility.PasteComponentAsNew( gameObject );
                }
            }
            finally
            {
                GUI.enabled = oldEnabled;
            }
        }

        private static void DrawRevealInFinderButton( Editor editor )
        {
            var oldEnabled = GUI.enabled;
            GUI.enabled = editor.targets.All( x => EditorUtility.IsPersistent( x ) );

            try
            {
                const string text =
#if UNITY_EDITOR_WIN
                        "Show In Explorer"
#else
                        "Reveal In Finder"
#endif
                    ;

                if ( GUILayout.Button( text, EditorStyles.miniButtonRight ) )
                {
                    foreach ( var target in editor.targets )
                    {
                        var assetPath = AssetDatabase.GetAssetPath( target );
                        EditorUtility.RevealInFinder( assetPath );
                    }
                }
            }
            finally
            {
                GUI.enabled = oldEnabled;
            }
        }


        private static void DrawExpandAllComponentsButton()
        {
            if ( GUILayout.Button( "Expand All Components", EditorStyles.miniButtonLeft ) )
            {
                PropertyEditorInternal.ExpandAllComponents( GetPropertyEditor() );
            }
        }

        private static void DrawCollapseAllComponentsButton()
        {
            if ( GUILayout.Button( "Collapse All Components", EditorStyles.miniButtonRight ) )
            {
                PropertyEditorInternal.CollapseAllComponents( GetPropertyEditor() );
            }
        }

        private static EditorWindow GetPropertyEditor()
        {
            return Resources
                    .FindObjectsOfTypeAll( PROPERTY_EDITOR_TYPE )
                    .FirstOrDefault() as EditorWindow
                ;
        }
    }
}