using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Silentor.UnityBuildConfigurator.Editor
{
    internal class Settings
    {
        public String ActiveConfig
        {
            get => EditorPrefs.GetString( ActiveConfigKey, "" );
            set
            {
                EditorPrefs.SetString( ActiveConfigKey, value );
                Debug.Log( $"[Settings]-[ActiveConfig] Setted to: {value}" );
            }
        }

        public IReadOnlyList<String> ConfigsList
        {
            get => LoadConfigsList();
            set => SaveConfigsList( value );
        }

        private void SaveConfigsList( IReadOnlyList<String> paths )
        {
            var storage = new List<String>( paths );
            var str = String.Join( ", ", storage );
            EditorPrefs.SetString( SaveConfigsListKey, str );

            Debug.Log( $"[Settings]-[SaveConfigsList] Updated configs list: {str}" );
        }

        private IReadOnlyList<String> LoadConfigsList( )
        {
            var str = EditorPrefs.GetString( SaveConfigsListKey, "" );
            return new List<String>( str.Split( new[] { ", " }, StringSplitOptions.RemoveEmptyEntries ) );
        }

        private static String ProjectWideKeyPrefix => $"{Application.companyName}.{Application.productName}.UBC."; 
        private static String SaveConfigsListKey      => ProjectWideKeyPrefix + "ConfigsList"; 
        private static String ActiveConfigKey      => ProjectWideKeyPrefix + "ActiveConfig"; 
    }
}