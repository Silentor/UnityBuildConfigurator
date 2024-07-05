using System;
using Newtonsoft.Json.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Silentor.UnityBuildConfigurator.Editor.Configs
{
    public class StackTraces : BuildConfigBase
    {
        public EStackTraceType Error        = EStackTraceType.ScriptOnly;
        public EStackTraceType Assert       = EStackTraceType.ScriptOnly;
        public EStackTraceType Warning      = EStackTraceType.ScriptOnly;
        public EStackTraceType Log          = EStackTraceType.ScriptOnly;
        public EStackTraceType Exception    = EStackTraceType.ScriptOnly;

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

        public enum EStackTraceType
        {
            None,
            ScriptOnly,
            Full
        }

        
    }
}