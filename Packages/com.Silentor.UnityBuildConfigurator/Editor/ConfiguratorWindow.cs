using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Silentor.UnityBuildConfigurator.Editor.Configs;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace Silentor.UnityBuildConfigurator.Editor
{
    public class ConfiguratorWindow : UnityEditor.EditorWindow
    {
        private ListView              _itemsList;
        private List<BuildConfigBase> _items = new();
        private ToolbarMenu           _addItemMn;
        private Type[]                _itemTypes;
        private Button                _saveBtn;

        private String _fileName;
        private Button _loadBtn;
        private Button _buildBtn;

        [UnityEditor.MenuItem( "Test/Build Configurator" )]
        private static void ShowWindow( )
        {
            var window = GetWindow<ConfiguratorWindow>();
            window.titleContent = new UnityEngine.GUIContent( "Build configurator" );
            window.Show();
        }

        private void CreateGUI( )
        {
              var root              = rootVisualElement;
              var contentAsset      = Resources.MainWindow;
              var content = contentAsset.Instantiate();

              _itemsList = content.Q<ListView>( "ItemsList" );
              _itemsList.makeItem = ItemsListMakeItem;
              _itemsList.bindItem = ItemsListBindItem;
              _itemsList.itemsSource = _items;

              _saveBtn         =  content.Q<Button>( "SaveBtn" );
              _saveBtn.clicked += SaveBtnClicked;

              _loadBtn         =  content.Q<Button>( "LoadBtn" );
              _loadBtn.clicked += LoadBtnClicked;

              _addItemMn = content.Q<ToolbarMenu>( "AddItemMn" );
              _itemTypes = TypeCache.GetTypesDerivedFrom<BuildConfigBase>(  ).Where( t => !t.IsAbstract ).OrderBy( t => t.Name ).ToArray();
              foreach ( var itemType in _itemTypes )
              {
                  _addItemMn.menu.AppendAction( itemType.Name, AddItemSelected, (_) => DropdownMenuAction.Status.Normal, itemType );
              }

              _buildBtn = content.Q<Button>( "BuildBtn" );
              _buildBtn.clicked += BuildBtnClicked;

              root.Add( content );
        }

        private void BuildBtnClicked( )
        {
            
        }

        private void LoadBtnClicked( )
        {
            if ( String.IsNullOrEmpty( _fileName ) )
            {
                _fileName = EditorUtility.OpenFilePanel( "Open file", "Assets", "json" );
            }

            if( String.IsNullOrEmpty( _fileName ) )
                return;

            var fileName = _fileName;
            var str      = File.ReadAllText( fileName );
            var storage  = JObject.Parse( str );
            var items    = storage[ "items" ];

            var loadedItems = new List<BuildConfigBase>();
            foreach ( JObject jItem in items )
            {
                var type     = Type.GetType( jItem[ "__type" ].ToString() );
                var instance = (BuildConfigBase)ScriptableObject.CreateInstance( type );
                instance.LoadFromJson( jItem );
                loadedItems.Add( instance );
            }

            _items.Clear();
            _items.AddRange( loadedItems );
            _itemsList.Rebuild();
        }

        private void SaveBtnClicked( )
        {
            var storage = new JObject();
            var items   = new JArray();
            storage["items"] = items;
            foreach ( var item in _items )
            {
                var itemWrapper = new JObject();
                itemWrapper[ "__type" ] = item.GetType().AssemblyQualifiedName;
                item.SaveToJson( itemWrapper );
                items.Add( itemWrapper );
            }

            if ( String.IsNullOrEmpty( _fileName ) )
            {
                _fileName = EditorUtility.SaveFilePanelInProject( "Save file", "MyBuildConfig", "json", "Save build config" );
            }

            if( String.IsNullOrEmpty( _fileName ) )
                return;

            var path = Path.GetDirectoryName( _fileName );
            if ( !Directory.Exists( path ) )
                Directory.CreateDirectory( path );
            File.WriteAllText( _fileName, storage.ToString() );

            Debug.Log( storage.ToString() );
        }

        private void AddItemSelected(DropdownMenuAction a )
        {
            var type     = (Type)a.userData;
            //var instance = (BuildConfigBase)Activator.CreateInstance( type );
            var instance = (BuildConfigBase)ScriptableObject.CreateInstance( type );
            _items.Add( instance );
            _itemsList.Rebuild();
        }

        private void ItemsListBindItem( VisualElement elem, Int32 index )
        {
            var content = elem.Q<VisualElement>( "Content" );
            var title   = elem.Q<GroupBox>(  );

            var item    = _items[index];
            item.CreateGUI( content );
            title.text = item.Name;
        }

        private VisualElement ItemsListMakeItem( )
        {
            var item = Resources.ItemAsset.Instantiate();
            return item;
        }

        private static class Resources
        {
            public static readonly VisualTreeAsset ItemAsset = UnityEngine.Resources.Load<VisualTreeAsset>( "BuildItem" ); 
            public static readonly VisualTreeAsset MainWindow = UnityEngine.Resources.Load<VisualTreeAsset>( "UBCWindow" ); 
        }
    }
}