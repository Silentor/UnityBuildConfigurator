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
        private ListView      _itemsList;
        private Button        _saveBtn;
        private ToolbarMenu   _addItemMn;
        private Button        _loadBtn;
        private Button        _buildBtn;
        private ToolbarMenu   _configsListMn;
        private Button        _cloneBtn;
        private Button        _deleteBtn;
        private Button        _newBtn;
        private ToolbarButton _removeItemBtn;

        private Config       _config;
        private Type[]       _itemTypes;
        private Settings     _settings;

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
            _config   = new Config( null, new List<BuildConfigItemBase> { CreateInstance<BuildSettings>() } );

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

            if( String.IsNullOrEmpty( activeConfig ) && configs.Any() )
                activeConfig = configs[ 0 ];
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
            _itemsList.unbindItem  = ItemsListUnbindItem;
            _itemsList.selectedIndicesChanged += ( _ ) => RefreshWidgets();

            _saveBtn         =  content.Q<Button>( "SaveBtn" );
            _saveBtn.clicked += SaveBtnClicked;

            _loadBtn         =  content.Q<Button>( "LoadBtn" );
            _loadBtn.clicked += LoadBtnClicked;

            _cloneBtn         =  content.Q<Button>( "CloneBtn" );
            _cloneBtn.clicked += CloneBtnClicked;

            _addItemMn = content.Q<ToolbarMenu>( "AddItemMn" );

            foreach ( var itemType in _itemTypes )
                _addItemMn.menu.AppendAction( itemType.Name, AddItemSelected, (_) => DropdownMenuAction.Status.Normal, itemType );

            _removeItemBtn = content.Q<ToolbarButton>( "RemoveItemBtn" );
            _removeItemBtn.clicked += RemoveItemBtnClicked;

            _buildBtn         =  content.Q<Button>( "BuildBtn" );
            _buildBtn.clicked += BuildBtnClicked;

            _deleteBtn = content.Q<Button>( "DeleteBtn" );
            _deleteBtn.clicked += DeleteBtnClicked;

            _newBtn = content.Q<Button>( "NewBtn" );
            _newBtn.clicked += NewBtnClicked;

            _configsListMn = content.Q<ToolbarMenu>( "ConfigsList" );

            root.Add( content );

            RefreshWidgets();
        }

        


        private void NewBtnClicked( )
        {
            if( _config.Path == null )
                return;

            SaveCurrentConfig();

            _config = new Config( null, new List<BuildConfigItemBase> { CreateInstance<BuildSettings>() } );

            RefreshWidgets();
        }

        private void DeleteBtnClicked( )
        {
            if( _config.Path == null )
                return;

            if ( EditorUtility.DisplayDialog( "Delete config", $"Do you want to delete config {_config.Path}?", "Yes", "Cancel" ) )
            {
                var currentConfigIndex = _settings.ConfigsList.ToList().IndexOf( _config.Path );
                if( currentConfigIndex < 0 )
                    return;

                var deletedConfig     = _config;
                var deletedConfigPath = deletedConfig.Path;
                var configsList       = _settings.ConfigsList.ToList();
                if ( _settings.ConfigsList.Count > 1 )
                {
                    if ( _config.Path == _settings.ConfigsList.Last() )
                        SwitchConfig( _settings.ConfigsList[ currentConfigIndex - 1 ] );
                    else
                        SwitchConfig( _settings.ConfigsList[ currentConfigIndex + 1 ] );
                }

                File.Delete( deletedConfigPath );
                AssetDatabase.Refresh();
                configsList.RemoveAt( currentConfigIndex );
                _settings.ConfigsList = configsList;

                RefreshWidgets();
            }
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

            SaveCurrentConfig();
            if( _config.Path == null )
            {
                EditorUtility.DisplayDialog( "Error", "Please save config before build", "Ok" );
                return;
            }

            var options = new BuildPlayerOptions();

            foreach ( var configItem in _config.Items ) 
                configItem.ApplyConfig( ref options );

            try
            {
                var report = BuildPipeline.BuildPlayer( options );
                var result = report.summary.result;
                var outputPath = report.summary.outputPath;
                var builtTime = report.summary.totalTime;
                var platform = report.summary.platform;
                var buildSize = report.summary.totalSize;
                var errors = (report.summary.totalErrors, report.summary.totalWarnings);
                Debug.Log( $"Build result {result}, path {outputPath}, platform {platform}, errors/warnings {errors.totalErrors}/{errors.totalWarnings}, size {EditorUtility.FormatBytes((long)buildSize)}, build time {builtTime.Minutes} m {builtTime.Seconds} s " );
            }
            finally
            {
                var reverseItems = _config.Items.ToList();
                reverseItems.Reverse();
                foreach ( var configItem in reverseItems ) 
                    configItem.RevertConfig( );
            }

            LoadConfig( _config.Path );
            RefreshWidgets( true );
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
            var instance = (BuildConfigItemBase)CreateInstance( type );
            _config.Items.Add( instance );

            RefreshWidgets( true );
        }

        private void RemoveItemBtnClicked( )
        {
            if( _itemsList.selectedIndex < 0 )
                return;

            _config.Items.RemoveAt( _itemsList.selectedIndex );

            RefreshWidgets( true );
        }

        private void ItemsListBindItem( VisualElement elem, Int32 index )
        {
            var content = elem.Q<VisualElement>( "Content" );
            var title   = elem.Q<GroupBox>(  );

            var item    = _config.Items[ index ];

            item.CreateGUI( content );
            title.text = item.DisplayName;
        }       

        private void ItemsListUnbindItem(VisualElement elem, Int32 index )
        {
            var content = elem.Q<VisualElement>( "Content" );
            
            foreach ( var child in content.Children().ToArray() )
            {
                //Some internal knowledge about GroupBox
                if ( !child.ClassListContains( GroupBox.labelUssClassName ) )
                {
                    content.Remove( child );
                } 
            }
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

            AssetDatabase.Refresh();

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

        private void RefreshWidgets( Boolean rebuildItemsList = false )
        {
            _buildBtn.SetEnabled( _config.Items.Count > 0 && _config.Path != null );
            _cloneBtn.SetEnabled( _config.Path != null );
            _newBtn.SetEnabled( _config.Path != null );
            _deleteBtn.SetEnabled( _config.Path != null );
            _removeItemBtn.SetEnabled( _itemsList.selectedIndex >= 0 );

            if ( _itemsList.itemsSource != _config.Items || rebuildItemsList )
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