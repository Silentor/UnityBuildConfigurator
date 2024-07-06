using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Object = System.Object;

namespace Silentor.UnityBuildConfigurator.Editor.Configs
{
    public class BuildSettings : BuildConfigItemBase
    {
        public BuildTarget BuildTarget ;
        public String      BuildPath;
        public String[]    Scenes;

        private void OnEnable( )
        {
            BuildTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildPath   = EditorUserBuildSettings.GetBuildLocation( BuildTarget );
            Scenes = EditorBuildSettings.scenes.Select( s => s.path ).ToArray();
        }

        public override void CreateGUI(VisualElement root )
        {
            var inspector = new InspectorElement( this );
            root.Add( inspector );
        }

        public override void SaveToJson( JObject storage )
        {
            SerializeMeToJObject( storage ) ;
        }

        public override void LoadFromJson( JObject storage )
        {
            DeserializeMeFromJObject( storage );
        }

        public override void ApplyConfig( ref BuildPlayerOptions options )
        {
             options.target           = BuildTarget;
             options.locationPathName = BuildPath;
             options.scenes           = Scenes;
        }

        public override void RevertConfig( )
        {
            
        }
    }
}