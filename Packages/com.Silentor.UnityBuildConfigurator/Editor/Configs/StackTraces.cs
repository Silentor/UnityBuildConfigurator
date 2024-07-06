using System;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace Silentor.UnityBuildConfigurator.Editor.Configs
{
    public class StackTraces : BuildConfigItemBase
    {
        public StackTraceLogType Error     = StackTraceLogType.ScriptOnly;
        public StackTraceLogType Assert    = StackTraceLogType.ScriptOnly;
        public StackTraceLogType Warning   = StackTraceLogType.ScriptOnly;
        public StackTraceLogType Log       = StackTraceLogType.ScriptOnly;
        public StackTraceLogType Exception = StackTraceLogType.ScriptOnly;

        private StackTraceLogType _oldError;
        private StackTraceLogType _oldAssert;
        private StackTraceLogType _oldWarning;
        private StackTraceLogType _oldLog;
        private StackTraceLogType _oldException;

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
            //Backup old values
            _oldError     = PlayerSettings.GetStackTraceLogType( LogType.Error );
            _oldAssert    = PlayerSettings.GetStackTraceLogType( LogType.Assert );
            _oldWarning   = PlayerSettings.GetStackTraceLogType( LogType.Warning );
            _oldLog       = PlayerSettings.GetStackTraceLogType( LogType.Log );
            _oldException = PlayerSettings.GetStackTraceLogType( LogType.Exception );

            //Set new values
            PlayerSettings.SetStackTraceLogType( LogType.Error,     Error );
            PlayerSettings.SetStackTraceLogType( LogType.Assert,    Assert );
            PlayerSettings.SetStackTraceLogType( LogType.Warning,   Warning );
            PlayerSettings.SetStackTraceLogType( LogType.Log,       Log );
            PlayerSettings.SetStackTraceLogType( LogType.Exception, Exception );
        }

        public override void RevertConfig( )
        {
            //Revert to old values
            PlayerSettings.SetStackTraceLogType( LogType.Error,     _oldError );
            PlayerSettings.SetStackTraceLogType( LogType.Assert,    _oldAssert );
            PlayerSettings.SetStackTraceLogType( LogType.Warning,   _oldWarning );
            PlayerSettings.SetStackTraceLogType( LogType.Log,       _oldLog );
            PlayerSettings.SetStackTraceLogType( LogType.Exception, _oldException );
        }
    }
}