﻿////////////////////////////////////////////////////////////////////////////////

using Markdig;
using Markdig.Syntax;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MG.MDV
{
    // TODO: variants - rich text, powerUI (or other HTML renderer), "side" browser (chromium)
    // TODO: test what happens when we have multiple TextAsset editors ...
    //          https://github.com/roydejong/chromium-unity-server
    //          https://ultralig.ht/


    [CustomEditor( typeof( TextAsset ) )]
    public class MarkdownViewer : Editor
    {
        public GUISkin Skin;


        //------------------------------------------------------------------------------

        static string GetFilePath( string filename )
        {
            var path = AssetDatabase.GetAssetPath( Selection.activeObject );

            if( string.IsNullOrEmpty( path ) )
            {
                path = "Assets";
            }
            else if( AssetDatabase.IsValidFolder( path ) == false )
            {
                path = Path.GetDirectoryName( path );
            }

            return AssetDatabase.GenerateUniqueAssetPath( path + "/" + filename );
        }

        [MenuItem( "Assets/Create/Markdown" )]
        static void CreateMarkdown()
        {
            var filepath = GetFilePath( "NewMarkdown.md" );

            // TODO: custom markdown template ...

            var writer = File.CreateText( filepath );
            writer.Write( "# Markdown\n" );
            writer.Close();

            AssetDatabase.ImportAsset( filepath );

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>( filepath );
        }


        //------------------------------------------------------------------------------

        private Editor mDefaultEditor;

        private void DrawDefault()
        {
            if( mDefaultEditor == null )
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
                var textInspectorType = assemblies.Select( asm => asm.GetType( "UnityEditor.TextAssetInspector" ) ).First( t => t != null );

                mDefaultEditor = textInspectorType != null ? Editor.CreateEditor( target, textInspectorType ) : null;
            }

            if( mDefaultEditor != null )
            {
                mDefaultEditor.OnInspectorGUI();
            }
        }


        //------------------------------------------------------------------------------

        MarkdownDocument mDoc;
        RendererMarkdown mRenderer;
        MarkdownPipeline mPipeline;

        public override void OnInspectorGUI()
        {
            GUI.DrawTexture( new Rect( 0, 46, EditorGUIUtility.currentViewWidth, Screen.height ), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill );

            // has target?

            var asset = target as TextAsset;

            if( asset == null )
            {
                DrawDefault();
                return;
            }

            // TODO: configurable extensions?
            // file extension is .md

            var path = AssetDatabase.GetAssetPath( target );
            var ext  = Path.GetExtension( path );

            if( ".md".Equals( ext, StringComparison.OrdinalIgnoreCase ) == false )
            {
                DrawDefault();
                return;
            }

            // parse

            if( mDoc == null )
            {
                // TODO: look at pipeline options ...
                mPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                mRenderer = new RendererMarkdown();
                mPipeline.Setup( mRenderer );

                mDoc = Markdown.Parse( asset.text, mPipeline );
            }

            // render

            GUI.skin = Skin;
            GUI.backgroundColor = Color.white;

            mRenderer.Render( mDoc );
        }
    }
}
