using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Silentor.UnityBuildConfigurator.Editor.Configs
{
    public abstract class BuildConfigItemBase : ScriptableObject
    {
        public virtual String DisplayName => ObjectNames.NicifyVariableName( GetType().Name );

        public abstract void    CreateGUI ( VisualElement root );
        public abstract void SaveToJson( JObject       storage);
        public abstract void    LoadFromJson( JObject     storage );

        public abstract void ApplyConfig( ref BuildPlayerOptions options );
        public abstract void RevertConfig( );

        protected void SerializeMeToJObject( JObject storage )
        {
            var settings = new JsonSerializerSettings()
                           {
                                   ContractResolver = new UnityStyleSerializationResolver()
                           };
            var result = JsonConvert.SerializeObject( this, settings );
            storage[ "__content" ] = JObject.Parse( result );
        }

        protected void DeserializeMeFromJObject( JObject storage )
        {
            var str = storage[ "__content" ].ToString();
            JsonConvert.PopulateObject( str, this );
        }

        private class UnityStyleSerializationResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);
                property.ShouldSerialize = _ => ShouldSerialize(member);
                return property;
            }

            private static bool ShouldSerialize(MemberInfo memberInfo)
            {
                //Serialize only fields, not properties
                return memberInfo is FieldInfo;
            }
        }
    }
}