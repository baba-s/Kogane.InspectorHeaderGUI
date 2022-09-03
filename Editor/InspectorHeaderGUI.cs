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
        private sealed class TextureData
        {
            private readonly string m_guid;

            private GUIContent m_guiContentCache;

            public GUIContent GuiContent
            {
                get
                {
                    if ( m_guiContentCache != null ) return m_guiContentCache;

                    var path    = AssetDatabase.GUIDToAssetPath( m_guid );
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>( path );

                    m_guiContentCache = new GUIContent( texture )
                    {
                        text = string.Empty,
                    };

                    return m_guiContentCache;
                }
            }

            public TextureData( string guid )
            {
                m_guid = guid;
            }
        }

        private static readonly Type PROPERTY_EDITOR_TYPE = typeof( Editor ).Assembly.GetType( "UnityEditor.PropertyEditor" );

        private static readonly TextureData LOCK_TEXTURE                    = new( "e46fc48e73c498149be679eb9513bef1" );
        private static readonly TextureData UNLOCK_TEXTURE                  = new( "3bc24d4385a34454cb8e67f037925c93" );
        private static readonly TextureData DEBUG_TEXTURE                   = new( "bad1644c13e0c694382f5b3d51bbeba7" );
        private static readonly TextureData PROPERTIES_TEXTURE              = new( "f06580a50a2eeae4f990a75bd4ff9f2c" );
        private static readonly TextureData PASTE_COMPONENT_AS_NEW_TEXTURE  = new( "45d577067ad153d41ba8159fda1dac09" );
        private static readonly TextureData REVEAL_IN_FINDER_TEXTURE        = new( "3a4a8645203241445804d6b3e0614b39" );
        private static readonly TextureData EXPAND_ALL_COMPONENTS_TEXTURE   = new( "ff490dff6b933244c862ec4ffb5f0966" );
        private static readonly TextureData COLLAPSE_ALL_COMPONENTS_TEXTURE = new( "9c38d887be2f708418388db6a513aa78" );

        static InspectorHeaderGUI()
        {
            Editor.finishedDefaultHeaderGUI -= OnGUI;
            Editor.finishedDefaultHeaderGUI += OnGUI;
        }

        private static void OnGUI( Editor editor )
        {
            var oldContentColor = GUI.contentColor;

            GUI.contentColor = EditorGUIUtility.isProSkin
                    ? new Color32( 188, 188, 188, 255 )
                    : new Color32( 20, 20, 20, 255 )
                ;

            try
            {
                using ( new EditorGUILayout.HorizontalScope() )
                {
                    DrawLockButton();
                    DrawDebugButton();
                    DrawPropertiesButton();

                    var oldEnabled = GUI.enabled;
                    GUI.enabled = editor.targets.All( x => !EditorUtility.IsPersistent( x ) );

                    try
                    {
                        DrawExpandAllComponentsButton();
                        DrawCollapseAllComponentsButton();
                    }
                    finally
                    {
                        GUI.enabled = oldEnabled;
                    }

                    DrawPasteComponentAsNew( editor );
                    DrawRevealInFinderButton( editor );
                }
            }
            finally
            {
                GUI.contentColor = oldContentColor;
            }
        }

        private static void DrawLockButton()
        {
            var oldEnabled = GUI.enabled;
            GUI.enabled = true;

            try
            {
                var tracker = ActiveEditorTracker.sharedTracker;

                if ( !GUILayout.Button( tracker.isLocked ? LOCK_TEXTURE.GuiContent : UNLOCK_TEXTURE.GuiContent, EditorStyles.miniButtonLeft ) ) return;

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

                if ( !GUILayout.Button( DEBUG_TEXTURE.GuiContent, EditorStyles.miniButtonMid ) ) return;

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
                if ( GUILayout.Button( PROPERTIES_TEXTURE.GuiContent, EditorStyles.miniButtonMid ) )
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
                if ( GUILayout.Button( PASTE_COMPONENT_AS_NEW_TEXTURE.GuiContent, EditorStyles.miniButtonMid ) )
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

                if ( GUILayout.Button( REVEAL_IN_FINDER_TEXTURE.GuiContent, EditorStyles.miniButtonRight ) )
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
            if ( GUILayout.Button( EXPAND_ALL_COMPONENTS_TEXTURE.GuiContent, EditorStyles.miniButtonMid ) )
            {
                PropertyEditorInternal.ExpandAllComponents( GetPropertyEditor() );
            }
        }

        private static void DrawCollapseAllComponentsButton()
        {
            if ( GUILayout.Button( COLLAPSE_ALL_COMPONENTS_TEXTURE.GuiContent, EditorStyles.miniButtonMid ) )
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