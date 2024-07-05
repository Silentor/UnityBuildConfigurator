using System;
using Newtonsoft.Json.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Silentor.UnityBuildConfigurator.Editor.Configs
{
    public class UnityDebugLog : BuildConfigBase
    {
        public String LogString;

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

        

        
    }
}