using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Silentor.UnityBuildConfigurator.Editor.Configs;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Object = System.Object;

namespace Silentor.UnityBuildConfigurator.Editor
{
    public class ConfiguratorWindow : EditorWindow
    {
        private ListView    _itemsList;
        private Button      _saveBtn;
        private ToolbarMenu _addItemMn;
        private Button      _loadBtn;
        private Button      _buildBtn;
        private ToolbarMenu _configsListMn;


        private Config       _config;
        private Type[]       _itemTypes;
        private List<String> _configsList = new();

        private String      _fileName;
        
        private Settings _settings;
        private string   _activeConfig;
        private Button   _cloneBtn;


        [MenuItem( "Test/Build Configurator" )]
        private static void ShowWindow( )
        {
            var window = GetWindow<ConfiguratorWindow>();
            window.titleContent = new GUIContent( "Build configurator", Resources.MainWindowIcon );
            window.Show();
        }

        private void OnEnable( )
        {
            Debug.Log( $"OnEnable, items {_config?.Items.Count}" );

            _itemTypes = TypeCache.GetTypesDerivedFrom<BuildConfigItemBase>(  ).Where( t => !t.IsAbstract ).OrderBy( t => t.Name )
                                  .ToArray();
            _settings = new Settings();
            _config   = new Config( null, Array.Empty<BuildConfigItemBase>() );

            //Sanitize configs list
            var configs = _settings.ConfigsList.ToList();
            foreach ( var config in configs.ToArray() )
            {
                if(!IsConfigExists( config ))
                    configs.Remove( config );
            }
            _settings.ConfigsList = configs;

            //Sanitize active config
            var activeConfig = _settings.ActiveConfig;
            if ( !configs.Contains( activeConfig ) )
            {
                activeConfig = null;
                if( configs.Any() )
                    activeConfig = configs[ 0 ];
            }
            _settings.ActiveConfig = activeConfig;

            //Load last config
            if( !String.IsNullOrEmpty( activeConfig ) )
                LoadConfig( activeConfig );


            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload  += OnAfterAssemblyReload;
        }

        private void OnDisable( )
        {
            Debug.Log( "OnDisable" );

            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload  -= OnAfterAssemblyReload;
        }

        public void OnBeforeAssemblyReload( )
        {
            Debug.Log( "Before Assembly Reload" );
        }

        public void OnAfterAssemblyReload( )
        {
            Debug.Log( "After Assembly Reload" );
        }

        private void CreateGUI( )
        {
            Debug.Log( "CreateGUI" );

            var root         = rootVisualElement;
            var contentAsset = Resources.MainWindow;
            var content      = contentAsset.Instantiate();

            _itemsList             = content.Q<ListView>( "ItemsList" );
            _itemsList.makeItem    = ItemsListMakeItem;
            _itemsList.bindItem    = ItemsListBindItem;

            _saveBtn         =  content.Q<Button>( "SaveBtn" );
            _saveBtn.clicked += SaveBtnClicked;

            _loadBtn         =  content.Q<Button>( "LoadBtn" );
            _loadBtn.clicked += LoadBtnClicked;

            _cloneBtn         =  content.Q<Button>( "CloneBtn" );
            _cloneBtn.clicked += CloneBtnClicked;

            _addItemMn = content.Q<ToolbarMenu>( "AddItemMn" );

            foreach ( var itemType in _itemTypes )
                _addItemMn.menu.AppendAction( itemType.Name, AddItemSelected, (_) => DropdownMenuAction.Status.Normal, itemType );

            _buildBtn         =  content.Q<Button>( "BuildBtn" );
            _buildBtn.clicked += BuildBtnClicked;

            _configsListMn = content.Q<ToolbarMenu>( "ConfigsList" );

            root.Add( content );

            RefreshWidgets();
        }

        private void CloneBtnClicked( )
        {
            if( _config.Path == null )
                return;

            SaveCurrentConfig();
            _config = new Config( null, _config.Items ); 

            RefreshWidgets();
        }

        private void BuildBtnClicked( )
        {
            if( _config.Items.Count == 0 )
            {
                return;
            }

            var options = new BuildPlayerOptions();

            foreach ( var configItem in _config.Items ) 
                configItem.ApplyConfig( ref options );

            var report = BuildPipeline.BuildPlayer( options );
            Debug.Log( $"Build result {report.summary.result}, items {String.Join( ", ", _config.Items )}" );
            //_itemsList.RefreshItems();


            //var report = BuildPipeline.BuildPlayer( defaultOptions );
            //Debug.Log( report );
        }

        private void LoadBtnClicked( )
        {
            var filePath = EditorUtility.OpenFilePanel( "Open file", "Assets", "json" );
            if ( String.IsNullOrEmpty( filePath ) )
                return;

            LoadConfig( filePath );
            if( !_settings.ConfigsList.Contains( filePath ) )
                _settings.ConfigsList = _settings.ConfigsList.Append( filePath ).ToList();

            RefreshWidgets();
        }

        private void SaveBtnClicked( )
        {
            SaveCurrentConfig(  );

            RefreshWidgets();
        }

        private void AddItemSelected(DropdownMenuAction a )
        {
            var type     = (Type)a.userData;
            //var instance = (BuildConfigBase)Activator.CreateInstance( type );
            var instance = (BuildConfigItemBase)CreateInstance( type );
            _config.Items.Add( instance );
            _itemsList.Rebuild();
        }

        private void ItemsListBindItem( VisualElement elem, Int32 index )
        {
            var content = elem.Q<VisualElement>( "Content" );
            var title   = elem.Q<GroupBox>(  );

            var item    = _config.Items[ index ];

            Debug.Log( $"Binding item {item}..." );

            item.CreateGUI( content );
            title.text = item.DisplayName;
        }

        private VisualElement ItemsListMakeItem( )
        {
            var item = Resources.ItemAsset.Instantiate();
            return item;
        }

        private void SwitchConfig( String path )
        {
            SaveCurrentConfig();

            LoadConfig( path );
        }

        private void LoadConfig( String path )
        {
            var str     = File.ReadAllText( path );
            var storage = JObject.Parse( str );
            var items   = storage[ "items" ];

            var loadedItems = new List<BuildConfigItemBase>();
            foreach ( JObject jItem in items )
            {
                var type     = Type.GetType( jItem[ "__type" ].ToString() );
                var instance = (BuildConfigItemBase)CreateInstance( type );
                instance.LoadFromJson( jItem );
                loadedItems.Add( instance );
            }

            _config = new  Config( path, loadedItems );

            if ( !_settings.ConfigsList.Contains( path ) )
                _settings.ConfigsList = _settings.ConfigsList.Append( path ).ToList();
            _settings.ActiveConfig = path;
        }

        private void SaveConfig( Config config )
        {
            Assert.IsFalse( String.IsNullOrEmpty( config.Path ) );

            var storage = new JObject();
            var items   = new JArray();
            storage[ "items" ] = items;
            foreach ( var item in config.Items )
            {
                var itemWrapper = new JObject();
                itemWrapper[ "__type" ] = item.GetType().AssemblyQualifiedName;
                item.SaveToJson( itemWrapper );
                items.Add( itemWrapper );
            }

            var dir = Path.GetDirectoryName( config.Path );
            if ( !Directory.Exists( dir ) )
                Directory.CreateDirectory( dir );
            File.WriteAllText( config.Path, storage.ToString() );

            if ( !_settings.ConfigsList.Contains( config.Path ) )
                _settings.ConfigsList = _settings.ConfigsList.Append( config.Path ).ToList();
            _settings.ActiveConfig = config.Path;

            Debug.Log( storage.ToString() );
        }

        private void SaveCurrentConfig( )
        {
            if ( _config.Path == null )
            {
                var pathToSave = EditorUtility.SaveFilePanelInProject( "Save file", "UnsavedConfig", "json", "Save build config" );
                if ( !String.IsNullOrEmpty( pathToSave ) )                
                    _config = new Config( pathToSave, _config.Items );
                else
                    return;
            }

            SaveConfig( _config );
        }

        private bool IsConfigExists( String path )
        {
            return File.Exists( path );
        }

        private void RefreshWidgets( )
        {
            _buildBtn.SetEnabled( _config.Items.Count > 0 );
            _cloneBtn.SetEnabled( _config.Path != null );

            if ( _itemsList.itemsSource != _config.Items )
            {
                _itemsList.itemsSource = _config.Items;
                _itemsList.Rebuild();
            }
            else
            {
                _itemsList.RefreshItems();
            }

            _configsListMn.menu.ClearItems();
            if ( _config.Path == null )
            {
                _configsListMn.menu.AppendAction( "Unsaved config", null, DropdownMenuAction.Status.Checked );
                _configsListMn.text = "Unsaved config";
            }
            foreach ( var configData in _settings.ConfigsList )
            {
                var configName = Path.GetFileNameWithoutExtension( configData );
                var isChecked  = configData == _config.Path;
                if ( isChecked )
                {
                    _configsListMn.text = configName;
                    _configsListMn.menu.AppendAction( configName, null, DropdownMenuAction.Status.Checked );
                }
                else
                    _configsListMn.menu.AppendAction( configName, (_) =>
                    {
                        SwitchConfig( configData );
                        RefreshWidgets();
                    } );
            }
           
        }

        [InitializeOnLoadMethod]
        private static void OnInitialized( )
        {
            //AssemblyReloadEvents.afterAssemblyReload += CreatedNew;
            Debug.Log( "InitializeOnLoadMethod" );
        }

        private class Config
        {
            public readonly String                    Path;
            public readonly List<BuildConfigItemBase> Items;

            public Config(String path, IReadOnlyList<BuildConfigItemBase> items )
            {
                Path  = path;
                Items = new List<BuildConfigItemBase>( items );
            }
        }

        private static class Resources
        {
            public static readonly VisualTreeAsset ItemAsset  = UnityEngine.Resources.Load<VisualTreeAsset>( "BuildItem" );
            public static readonly VisualTreeAsset MainWindow = UnityEngine.Resources.Load<VisualTreeAsset>( "UBCWindow" );

            public static readonly Texture2D MainWindowIcon = UnityEngine.Resources.Load<Texture2D>( "manufacturing" );
        }
    }
}