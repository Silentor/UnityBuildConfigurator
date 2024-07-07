using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Silentor.UnityBuildConfigurator
{
    public class TestMultiplatformProject : MonoBehaviour
    {
        void Start()
        {
            Debug.Log( $"Application.platform: {Application.platform}" );
            Debug.Log( $"Debug.isDebugBuild: {Debug.isDebugBuild}" );
            PrintLogTypes();
            PrintScenes( );
            PrintDefines();
        }

        private static void PrintLogTypes( )
        {
            var logTypeString = String.Join( ", ", Enum.GetValues( typeof(LogType) ).Cast<LogType>()
                                                       .Select( lt => $"{lt} => {Application.GetStackTraceLogType( lt )}"  ));
            Debug.Log( $"Application.GetStackTraceLogType: {logTypeString}" );
        }

        private static void PrintScenes( )
        {
            var scenesString = String.Join( ", ", Enumerable.Range( 0, SceneManager.sceneCountInBuildSettings )
                                                            .Select( i => SceneManager.GetSceneByBuildIndex( i ).name ) );
            Debug.Log( $"Scenes in build: {scenesString}" );
        }

        private static void PrintDefines( )
        {
            var definesString = String.Empty;

            #if DEVELOPMENT_BUILD
                definesString += "DEVELOPMENT_BUILD, ";
            #endif

            #if UNITY_STANDALONE_WIN
                definesString += "UNITY_STANDALONE_WIN, ";
            #endif

            #if UNITY_ANDROID
                definesString += "UNITY_ANDROID, ";
            #endif

            #if UNITY_ANALYTICS
                definesString += "UNITY_ANALYTICS, ";
            #endif

            #if UNITY_ASSERTIONS
                definesString += "UNITY_ASSERTIONS, ";
            #endif

            #if UNITY_64
            definesString += "UNITY_64, ";
            #endif

            #if UNITY_2022
            definesString += "UNITY_2022, ";
            #endif

            #if ENABLE_MONO
            definesString += "ENABLE_MONO, ";
            #endif

            #if ENABLE_IL2CPP
            definesString += "ENABLE_IL2CPP, ";
            #endif

            #if NET_STANDARD
            definesString += "NET_STANDARD, ";
            #endif

            #if DEVELOPMENT_BUILD
            definesString += "DEVELOPMENT_BUILD, ";
            #endif

            #if CUSTOM_DEFINE
            definesString += "CUSTOM_DEFINE, ";
            #endif

            Debug.Log( $"Defines: {definesString}" );
        }

    }
}
