﻿using System;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Silentor.UnityBuildConfigurator.Editor.Configs
{
    public class UnityDebugLog : BuildConfigItemBase
    {
        public String LogString = "Some log during build";

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
            Debug.Log( LogString );
        }

        public override void RevertConfig( )
        {
            
        }
    }
}