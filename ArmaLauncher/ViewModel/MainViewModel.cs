using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using ArmaLauncher.Controls;
using ArmaLauncher.Helpers;
using ArmaLauncher.Managers;
using ArmaLauncher.Models;
using ArmaLauncher.Properties;
using GalaSoft.MvvmLight;
using Microsoft.Expression.Interactivity.Core;
using SteamLib;
using SteamLib.MasterServer;
using Player = ArmaLauncher.Models.Player;

namespace ArmaLauncher.ViewModel
{

    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private ICollectionView MyData;

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                OnPropertyChanged("SearchText");
                //SearchTextBoxTextChangedCommand.Execute(null);
            }
        }

        private ClientPlayer _dataGridSelectedPlayerItem;
        private Server _dataGridSelectedItem;
        private bool _hasFirstSearchInitiated;
        private bool _isGlobalPing;
        private bool _isPinging;
        private bool _isSearching;
        private static int _pingCurrentIndex;
        private int _pingRefreshGridIndex;
        private bool _pingRefreshGridShouldUpdate;
        private bool _shouldPing;
        private ArmaLauncher.Models.GameType _gameType;
        private string _searchText;

        public SteamProviderManager SteamProviderManager { get; set; }

        //This will bind to the DataGrid instead of the TestEntities
        public CollectionViewSource ViewSource { get; set; }
        
        public MainViewModel(string pageType)
        {
            this.PageType = pageType;

            //must init 1st
            ResetCoreObjects();

            //Initialize the view source and set the source to your observable collection
            this.ViewSource = new CollectionViewSource();

            // init after knowledge of page
            InitializeObjects();

            Globals.Current.ViewModel = this;

            //reset sources
            ResetViewSourceAssociations(pageType);

            StopSearching();
        }

        private void ResetViewSourceAssociations(string pageType)
        {
            switch (pageType)
            {
                case "ArmaLauncher.Servers":
                    ViewSource.Source = Globals.Current.ArmaServers;
                    break;
                case "ArmaLauncher.Favorites":
                    ViewSource.Source = Globals.Current.ArmaFavoriteServers;
                    break;
                case "ArmaLauncher.Recent":
                    ViewSource.Source = Globals.Current.ArmaRecentServers;
                    break;
                case "ArmaLauncher.Notes":
                    ViewSource.Source = Globals.Current.ArmaNotesServers;
                    break;
                case "ArmaLauncher.Players":
                    //todo:aaron-change this when new page happens
                    ViewSource.Source = Globals.Current.ArmaPlayers;
                    break;
            }

            ViewSource.View.Refresh();
        }

        public static void ResetViewSourceAssociations()
        {
            if (Globals.Current.PageServers != null && Globals.Current.PageServers.ViewModel != null)
            {
                var selectedItem = Globals.Current.PageServers.MainDataGrid.SelectedCells.FirstOrDefault().Item;
                Globals.Current.PageServers.ViewModel.ViewSource.Source = Globals.Current.ArmaServers;
                Globals.Current.PageServers.ViewModel.ViewSource.View.Refresh();
                Globals.Current.PageServers.MainDataGrid.SelectedItem = selectedItem;
            }

            if (Globals.Current.PageFavorites != null && Globals.Current.PageFavorites.ViewModel != null)
            {
                var selectedItem = Globals.Current.PageFavorites.MainDataGrid.SelectedCells.FirstOrDefault().Item;
                Globals.Current.PageFavorites.ViewModel.ViewSource.Source = Globals.Current.ArmaFavoriteServers;
                Globals.Current.PageFavorites.ViewModel.ViewSource.View.Refresh();
                Globals.Current.PageFavorites.MainDataGrid.SelectedItem = selectedItem;
            }

            if (Globals.Current.PageRecent != null && Globals.Current.PageRecent.ViewModel != null)
            {
                var selectedItem = Globals.Current.PageRecent.MainDataGrid.SelectedCells.FirstOrDefault().Item;
                Globals.Current.PageRecent.ViewModel.ViewSource.Source = Globals.Current.ArmaRecentServers;
                Globals.Current.PageRecent.ViewModel.ViewSource.View.Refresh();
                Globals.Current.PageRecent.MainDataGrid.SelectedItem = selectedItem;
            }

            if (Globals.Current.PageNotes != null && Globals.Current.PageNotes.ViewModel != null)
            {
                var selectedItem = Globals.Current.PageNotes.MainDataGrid.SelectedCells.FirstOrDefault().Item;
                Globals.Current.PageNotes.ViewModel.ViewSource.Source = Globals.Current.ArmaNotesServers;
                Globals.Current.PageNotes.ViewModel.ViewSource.View.Refresh();
                Globals.Current.PageNotes.MainDataGrid.SelectedItem = selectedItem;
            }

            if (Globals.Current.PagePlayers != null && Globals.Current.PagePlayers.ViewModel != null)
            {
                var selectedItem = Globals.Current.PagePlayers.PlayersDataGrid.SelectedCells.FirstOrDefault().Item;
                Globals.Current.PagePlayers.ViewModel.ViewSource.Source = Globals.Current.ArmaPlayers;
                Globals.Current.PagePlayers.ViewModel.ViewSource.View.Refresh();
                Globals.Current.PagePlayers.PlayersDataGrid.SelectedItem = selectedItem;
            }
        }

        public string PageType { get; set; }

        //public GameType GameType
        //{
        //    get { return _gameType; }
        //    set
        //    {
        //        _gameType = value;
        //        Globals.Current.ArmaGameType = value;
        //        DdGameTypeSelectionChangedCommand.Execute(null);
        //        OnPropertyChanged("GameType");
        //    }
        //}

        public Button StopSearchingButton { get; set; }
        public Button BtnGetArmaList { get; set; }

        private void InitializeObjects()
        {
            //other controls
            StopSearchingButton = new Button();
            BtnGetArmaList = new Button();

            //reset grid
            switch (PageType)
            {
                case "ArmaLauncher.Servers":
                    if (Globals.Current.PageServers != null)
                    {
                        Globals.Current.PageServers.MainDataGrid.AutoGenerateColumns = true;
                        Globals.Current.PageServers.MainDataGrid.AutoGenerateColumns = false;
                        //Globals.Current.PageServers.MainDataGrid.UpdateLayout();
                        ViewSource.View.Refresh();
                    }
                    break;
                case "ArmaLauncher.Favorites":
                    if (Globals.Current.PageFavorites != null)
                    {
                        Globals.Current.PageFavorites.MainDataGrid.AutoGenerateColumns = true;
                        Globals.Current.PageFavorites.MainDataGrid.AutoGenerateColumns = false;
                        //Globals.Current.PageFavorites.MainDataGrid.UpdateLayout();
                        ViewSource.View.Refresh();
                    }
                    break;
                case "ArmaLauncher.Recent":
                    if (Globals.Current.PageRecent != null)
                    {
                        Globals.Current.PageRecent.MainDataGrid.AutoGenerateColumns = true;
                        Globals.Current.PageRecent.MainDataGrid.AutoGenerateColumns = false;
                        //Globals.Current.PageRecent.MainDataGrid.UpdateLayout();
                        ViewSource.View.Refresh();
                    }
                    break;
                case "ArmaLauncher.Notes":
                    if (Globals.Current.PageNotes != null)
                    {
                        Globals.Current.PageNotes.MainDataGrid.AutoGenerateColumns = true;
                        Globals.Current.PageNotes.MainDataGrid.AutoGenerateColumns = false;
                        //Globals.Current.PageNotes.MainDataGrid.UpdateLayout();
                        ViewSource.View.Refresh();
                    }
                    break;
                case "ArmaLauncher.Players":
                    //todo:aaron-change this when new page happens
                    if (Globals.Current.PagePlayers != null)
                    {
                        Globals.Current.PagePlayers.PlayersDataGrid.AutoGenerateColumns = true;
                        Globals.Current.PagePlayers.PlayersDataGrid.AutoGenerateColumns = false;
                        //Globals.Current.PagePlayers.PlayersDataGrid.UpdateLayout();
                        ViewSource.View.Refresh();
                    }
                    break;
            }

            // local objects
            HasFirstSearchInitiated = false;
            IsSearching = false;
            _gridFirstTimeLoaded = true;
            StopSearchingButton.Visibility = Visibility.Collapsed;
            StopSearchingButton.IsEnabled = false;

            SteamProviderManager = new SteamProviderManager(SteamProviderType.SteamProvider);
        }

        public void ResetCoreObjects()
        {
            // do not reset objects if searching...
            if (Globals.Current.PageServers != null && Globals.Current.PageServers.ViewModel.IsSearching) return;

            //init objects
            Globals.Current.ArmaServers = new ObservableCollection<Server>();
            Globals.Current.ArmaFavoriteServers = new ObservableCollection<Server>();
            Globals.Current.ArmaRecentServers = new ObservableCollection<Server>();
            Globals.Current.ArmaNotesServers = new ObservableCollection<Server>();
            Globals.Current.ArmaPasswordServers = new ObservableCollection<Server>();

            //get this server data first
            Globals.Current.GetServers();

            //get servers data - and back fill: Globals.Current.ArmaServers
            Globals.Current.GetFavoriteServers();
            Globals.Current.GetRecentServers();
            Globals.Current.GetNotesServers();
            Globals.Current.GetPasswordServers();

            //get any players
            Globals.Current.ArmaPlayers = new ObservableCollection<ClientPlayer>();
            Globals.Current.GetPlayers();
        }

        public void MapNewServer(GameServer newServer, Server serverToUpdate)
        {
            serverToUpdate.Id = string.Format("{0}{1}", newServer.Host.Replace(".", ""), newServer.QueryPort);
            serverToUpdate.Passworded = newServer.Passworded;
            serverToUpdate.Mod = newServer.Mod != null ? newServer.Mod.Replace("\r", "").Replace("\n", "") : string.Empty;
            serverToUpdate.Name = newServer.Name != null
                ? newServer.Name.Replace("\r", "").Replace("\n", "")
                : string.Empty;
            serverToUpdate.Island = newServer.Map;
            serverToUpdate.NumPlayers = newServer.NumPlayers;
            serverToUpdate.MaxPlayers = newServer.MaxPlayers;
            serverToUpdate.DisplayNumPlayers = string.Format("{0} of {1}", serverToUpdate.NumPlayers, serverToUpdate.MaxPlayers);

            if(serverToUpdate.Game == null)
                serverToUpdate.Game = new Game();
            serverToUpdate.Game.Players = newServer.Players;
            serverToUpdate.Host = newServer.Host;
            serverToUpdate.QueryPort = newServer.QueryPort;
            serverToUpdate.Name = newServer.Name;

            // 1. build 2 separate parsers for signames and mods from:   newServer.Parameters
            // 2. parse each type to mods list and signames list
            // 3. parse/iterate over the mods list a 2nd time and build a dictionary with key/value
            //    and ONLY build key/value pair from characters greater than 1 character
            //      - "mods" is most important and will be used to automatch client to server mods
            // 4. change model names to just signames and mods
            // ..... signames value example: "0;epoch0302;epoch0303;epoch0304;epoch0310;epoch0320;
            // ..... mods value example: "Arma 3 Zeus;c6890f55;Arma 3 Karts;4bdcb8a9;

            var doNotMapList = new List<string>
                                    {
                                        "modhashes",
                                        "signames",
                                        "modnames",
                                        "params",
                                        "mods"
                                    };
            foreach (DictionaryEntry param in newServer.Parameters)
            {
                var parsedKeyName = param.Key.ToString().Split(':');
                if (parsedKeyName[0] != null && !doNotMapList.Contains(parsedKeyName[0], StringComparer.OrdinalIgnoreCase))
                {
                    switch (param.Key.ToString().ToLowerInvariant())
                    {
                        case "serverport":
                            serverToUpdate.GamePort = Convert.ToInt32(param.Value);
                            break;
                        case "timeleft":
                            serverToUpdate.Timeleft = param.Value.ToString();
                            break;
                        case "secureserver":
                            serverToUpdate.Secureserver = param.Value.ToString();
                            break;
                        case "botcount":
                            serverToUpdate.Botcount = param.Value.ToString();
                            break;
                        case "appid":
                            serverToUpdate.Appid = param.Value.ToString();
                            break;
                            //case "hostname":
                        case "gameversion":
                            serverToUpdate.Gameversion = param.Value.ToString();
                            break;
                            //case "mod":
                        case "hash":
                            serverToUpdate.Hash = param.Value.ToString();
                            break;
                            //case "maxplayers":
                            //case "numplayers":
                        case "mapname":
                            serverToUpdate.Mapname = param.Value.ToString();
                            break;
                        case "players":
                            serverToUpdate.Players = param.Value.ToString();
                            break;
                        case "modname":
                            serverToUpdate.Modname = param.Value.ToString();
                            break;
                        case "protocolver":
                            serverToUpdate.Protocolver = param.Value.ToString();
                            break;
                            //case "passworded":
                        case "serveros":
                            serverToUpdate.Serveros = param.Value.ToString();
                            break;
                        case "servertype":
                            serverToUpdate.Servertype = param.Value.ToString();
                            break;
                        //case "modhashes":
                        //    serverToUpdate.ModHashesDisplay = param.Value.ToString();
                        //    break;
                        //case "signames":
                        //    serverToUpdate.SigNamesDisplay = param.Value.ToString();
                        //    break;
                        //case "modnames":
                        //    serverToUpdate.ModNamesDisplay += param.Value.ToString();
                        //    break;
                        //case "params":
                        //    serverToUpdate.ParamsDisplay = param.Value.ToString();
                        //    break;
                        //case "mods":
                        //    serverToUpdate.ModsDisplay = param.Value.ToString();
                        //    break;
                    }
                }
                else
                {
                    //map dictionaries and display versions of dictionaries

                    //mods
                    if (param.Key.ToString().ToLowerInvariant().Contains("mods"))
                    {
                        var displayValue = param.Value.ToString().Replace(";", "; ");
                        if (serverToUpdate.ModsDisplay == null)
                            serverToUpdate.ModsDisplay = displayValue;
                        else if (serverToUpdate.ModsDisplay != null && !serverToUpdate.ModsDisplay.Contains(displayValue))
                            serverToUpdate.ModsDisplay += displayValue;

                        //if (!serverToUpdate.ModsList.ContainsKey(param.Key.ToString()))
                        //    serverToUpdate.ModsList.Add(param.Key.ToString(), param.Value.ToString());

                        serverToUpdate.ModsList = GetParamModel(param, serverToUpdate.ModsDisplay);
                    }

                    //modnames
                    if (param.Key.ToString().ToLowerInvariant().Contains("modnames"))
                    {
                        var displayValue = param.Value.ToString().Replace(";", "; ");
                        if (serverToUpdate.ModNamesDisplay == null)
                            serverToUpdate.ModNamesDisplay = displayValue;
                        else if (!serverToUpdate.ModNamesDisplay.Contains(displayValue))
                            serverToUpdate.ModNamesDisplay += displayValue;

                        //if (!serverToUpdate.ModNamesList.ContainsKey(param.Key.ToString()))
                        //    serverToUpdate.ModNamesList.Add(param.Key.ToString(), param.Value.ToString());

                        serverToUpdate.ModNamesList = GetParamModel(param, displayValue);
                    }

                    //modhashes
                    if (param.Key.ToString().ToLowerInvariant().Contains("modhashes"))
                    {
                        var displayValue = param.Value.ToString().Replace(";", "; ");
                        if (serverToUpdate.ModHashesDisplay == null)
                            serverToUpdate.ModHashesDisplay = displayValue;
                        else if (!serverToUpdate.ModHashesDisplay.Contains(displayValue))
                            serverToUpdate.ModHashesDisplay += displayValue;

                        //if (!serverToUpdate.ModHashesList.ContainsKey(param.Key.ToString()))
                        //    serverToUpdate.ModHashesList.Add(param.Key.ToString(), param.Value.ToString());

                        serverToUpdate.ModHashesList = GetParamModel(param, displayValue);
                    }

                    //signames
                    if (param.Key.ToString().ToLowerInvariant().Contains("signames"))
                    {
                        var displayValue = param.Value.ToString().Replace(";", "; ");
                        if (serverToUpdate.SigNamesDisplay == null)
                            serverToUpdate.SigNamesDisplay = displayValue;
                        else if (!serverToUpdate.SigNamesDisplay.Contains(displayValue))
                            serverToUpdate.SigNamesDisplay += displayValue;

                        //if (!serverToUpdate.SigNamesList.ContainsKey(param.Key.ToString()))
                        //    serverToUpdate.SigNamesList.Add(param.Key.ToString(), param.Value.ToString());

                        serverToUpdate.SigNamesList = GetParamModel(param, displayValue);
                    }

                    //params
                    if (param.Key.ToString().ToLowerInvariant().Contains("params"))
                    {
                        var displayValue = param.Value.ToString().Replace(";", "; ");
                        if (serverToUpdate.ParamsDisplay == null)
                            serverToUpdate.ParamsDisplay = displayValue;
                        else if (!serverToUpdate.ParamsDisplay.Contains(displayValue))
                            serverToUpdate.ParamsDisplay += displayValue;

                        //if (!serverToUpdate.ParamsList.ContainsKey(param.Key.ToString()))
                        //    serverToUpdate.ParamsList.Add(param.Key.ToString(), param.Value.ToString());

                        serverToUpdate.ParamsList = GetParamModel(param, displayValue);
                    }
                }
            }
        }

        private static List<Param> GetParamModel(DictionaryEntry param, string displayValue)
        {
            // use 'displayValue' instead of 'parm.Value' due to inconsistent parsing issue
            var unparsedParams = displayValue.Trim().Split(';');
            var paramModelList = new List<Param>();

            var parsedParams = new string[unparsedParams.Length];
            for (var i = 0; i < unparsedParams.Length; i++)
            {
                parsedParams[i] = unparsedParams[i].Trim();
            }

            //sort ids & names
            for (var i = 0; i < parsedParams.Length; i++)
            {
                if (parsedParams[i] == string.Empty)
                    continue;

                var paramModel = new Param();
                paramModel.Type = param.Key.ToString();

                //name
                paramModel.Name = parsedParams[i];

                if (i + 1 > parsedParams.Length - 1)
                {
                    paramModelList.Add(paramModel);
                    continue;
                }

                if (!string.IsNullOrEmpty(parsedParams[i + 1]) && IsHex(parsedParams[i + 1].ToLowerInvariant()))
                {
                    paramModel.Id = parsedParams[i + 1];
                }

                paramModelList.Add(paramModel);
                i = i + 1;
            }

            return paramModelList;
        }

        private static bool IsHex(IEnumerable<char> chars)
        {
            bool isHex;
            foreach (var c in chars)
            {
                isHex = ((c >= '0' && c <= '9') ||
                         (c >= 'a' && c <= 'f') ||
                         (c >= 'A' && c <= 'F'));

                if (!isHex)
                    return false;
            }
            return true;
        }

        public static void SaveServer(Server server)
        {
            if (server == null)
                return;

            //save across all types

            if (Globals.Current.ArmaServers != null)
            {
                //var serverRegular = Globals.Current.ArmaServers.FirstOrDefault(a => a.Host == server.Host && a.GamePort == server.GamePort);
                //if (serverRegular == null)
                    Globals.Current.SaveSingleServerToDisk(server);
            }

            if (Globals.Current.ArmaFavoriteServers != null)
            {
                //var serverFavorite = Globals.Current.ArmaFavoriteServers.FirstOrDefault(a => a.Host == server.Host && a.GamePort == server.GamePort);
                //if (serverFavorite == null)
                    Globals.Current.SaveFavoriteServerToDisk(server);
            }

            if (Globals.Current.ArmaRecentServers != null)
            {
                //var serverRecent = Globals.Current.ArmaRecentServers.FirstOrDefault(a => a.Host == server.Host && a.GamePort == server.GamePort);
                //if (serverRecent == null)
                    Globals.Current.SaveRecentServerToDisk(server);
            }

            if (Globals.Current.ArmaNotesServers != null)
            {
                //var serverNotes = Globals.Current.ArmaNotesServers.FirstOrDefault(a => a.Host == server.Host && a.GamePort == server.GamePort);
                //if (serverNotes == null)
                    Globals.Current.SaveNotesServerToDisk(server);
            }

            if (Globals.Current.ArmaPasswordServers != null)
            {
                //var serverPassword = Globals.Current.ArmaPasswordServers.FirstOrDefault(                        a => a.Host == server.Host && a.GamePort == server.GamePort);
                //if (serverPassword == null)
                    Globals.Current.SavePasswordServerToDisk(server);
            }
        }

        public static void SavePlayer(ClientPlayer player)
        {
            if (player == null)
                return;

            Globals.Current.SavePlayerToDisk(player);
        }

        public bool SelectedItemAutoMatch { get; set; }
        public List<string> SelectedItemClientMods { get; set; }

        public List<string> ListOfMods
        {
            get
            {
                if (DataGridSelectedItem == null)
                    return null;

                if (DataGridSelectedItem.Metadata.ArmaGameType == ArmaLauncher.Models.GameType.Arma2)
                    return Globals.Current.GetArma2ModList();

                return Globals.Current.GetArma3ModList();
            }
        }

        public double GridWidth
        {
            get { return Globals.Current.AppWidth; }
        }

        public int MaxPing
        {
            get
            {
                var def = Convert.ToInt32(ConfigurationManager.AppSettings["DefaultMaxPing"]);
                return string.IsNullOrEmpty(Settings.Default.MaxPing) ? def : Convert.ToInt32(Settings.Default.MaxPing);
            }
            set
            {
                Settings.Default.MaxPing = Convert.ToString(value);
                Settings.Default.Save();
                OnPropertyChanged("MaxPing");
                OnPropertyChanged("MaxPingDisplay");
            }
        }

        public string MaxPingDisplay
        {
            get { return MaxPing == 0 ? "off" : MaxPing.ToString(CultureInfo.InvariantCulture); }
        }

        private bool _gridFirstTimeLoaded { get; set; }

        public bool HasFirstSearchInitiated
        {
            get { return _hasFirstSearchInitiated; }
            set
            {
                _hasFirstSearchInitiated = value;
                OnPropertyChanged("HasFirstSearchInitiated");
                OnPropertyChanged("IsSearching");
            }
        }

        public bool IsSearching
        {
            get { return _isSearching; }
            set
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    StopSearchingButton.Visibility = Visibility.Visible;
                    StopSearchingButton.IsEnabled = true;
                });
                _isSearching = value;
                OnPropertyChanged("HasFirstSearchInitiated");
                OnPropertyChanged("IsSearching");
            }
        }

        public int PingCurrentIndex
        {
            get { return _pingCurrentIndex; }
            set
            {
                _pingCurrentIndex = value;
                OnPropertyChanged("PingCurrentIndex");
            }
        }

        public int PingRefreshGridIndex
        {
            get { return _pingRefreshGridIndex; }
            set
            {
                _pingRefreshGridIndex = value;
                OnPropertyChanged("PingRefreshGridIndex");
            }
        }

        public bool PingRefreshGridShouldUpdate
        {
            get { return _pingRefreshGridShouldUpdate; }
            set
            {
                _pingRefreshGridShouldUpdate = value;
                OnPropertyChanged("PingRefreshGridShouldUpdate");
            }
        }

        public bool IsPinging
        {
            get { return _isPinging; }
            set
            {
                _isPinging = value;
                OnPropertyChanged("IsPinging");
            }
        }

        public bool IsGlobalPing
        {
            get { return _isGlobalPing; }
            set
            {
                _isGlobalPing = value;
                OnPropertyChanged("IsGlobalPing");
            }
        }

        public bool ShouldPing
        {
            get { return _shouldPing; }
            set
            {
                _shouldPing = value;
                OnPropertyChanged("ShouldPing");
            }
        }

        public Server DataGridSelectedItem
        {
            get { return _dataGridSelectedItem; }
            set
            {
                _dataGridSelectedItem = value;

                switch (PageType)
                {
                    case "ArmaLauncher.Servers":
                        if (Globals.Current.PageServers != null)
                        {
                            Globals.Current.PageServers.MainDataGrid.UpdateLayout();
                            //ViewSource.View.Refresh();
                        }
                        break;
                    case "ArmaLauncher.Favorites":
                        if (Globals.Current.PageFavorites != null)
                        {
                            Globals.Current.PageFavorites.MainDataGrid.UpdateLayout();
                            //ViewSource.View.Refresh();
                        }
                        break;
                    case "ArmaLauncher.Recent":
                        if (Globals.Current.PageRecent != null)
                        {
                            Globals.Current.PageRecent.MainDataGrid.UpdateLayout();
                            //ViewSource.View.Refresh();
                        }
                        break;
                    case "ArmaLauncher.Notes":
                        if (Globals.Current.PageNotes != null)
                        {
                            Globals.Current.PageNotes.MainDataGrid.UpdateLayout();
                            //ViewSource.View.Refresh();
                        }
                        break;
                    case "ArmaLauncher.Players":
                        //todo:aaron-change this when new page happens
                        if (Globals.Current.PagePlayers != null)
                        {
                            Globals.Current.PagePlayers.PlayersDataGrid.UpdateLayout();
                            //ViewSource.View.Refresh();
                        }
                        break;
                }
                //todo:remove this as it does not look like it is required
                //OnPropertyChanged("DataGridSelectedItem");
                //OnPropertyChanged("ServerList");
                //OnPropertyChanged("SelectedItemAutoMatch");
                //OnPropertyChanged("SelectedItemClientMods");
                //OnPropertyChanged("ListOfMods");
            }
        }

        public ClientPlayer DataGridSelectedPlayerItem
        {
            get { return _dataGridSelectedPlayerItem; }
            set
            {
                _dataGridSelectedPlayerItem = value;

                switch (PageType)
                {
                    case "ArmaLauncher.Players":
                        if (Globals.Current.PagePlayers != null)
                        {
                            Globals.Current.PagePlayers.PlayersDataGrid.UpdateLayout();
                            //ViewSource.View.Refresh();
                        }
                        break;
                }
                //todo:remove this as it does not look like it is required
                //OnPropertyChanged("DataGridSelectedItem");
                //OnPropertyChanged("ServerList");
                //OnPropertyChanged("SelectedItemAutoMatch");
                //OnPropertyChanged("SelectedItemClientMods");
                //OnPropertyChanged("ListOfMods");
            }
        }

        public static event PropertyChangedEventHandler PropertyChanged;

        public ActionCommand SearchTextBoxTextChangedCommand
        {
            get
            {
                return new ActionCommand(() =>
                                         {
                                             var t = new TextBox();

                                             switch (PageType)
                                             {
                                                 case "ArmaLauncher.Servers":
                                                     if (Globals.Current.PageServers != null)
                                                     {
                                                         t = Globals.Current.PageServers.tbSearchFilter as TextBox;
                                                     }
                                                     break;
                                                 case "ArmaLauncher.Favorites":
                                                     if (Globals.Current.PageFavorites != null)
                                                     {
                                                         t = Globals.Current.PageFavorites.tbSearchFilter as TextBox;
                                                     }
                                                     break;
                                                 case "ArmaLauncher.Recent":
                                                     if (Globals.Current.PageRecent != null)
                                                     {
                                                         t = Globals.Current.PageRecent.tbSearchFilter as TextBox;
                                                     }
                                                     break;
                                                 case "ArmaLauncher.Notes":
                                                     if (Globals.Current.PageNotes != null)
                                                     {
                                                         t = Globals.Current.PageNotes.tbSearchFilter as TextBox;
                                                     }
                                                     break;
                                                 case "ArmaLauncher.Players":
                                                     //todo:aaron-change this when new page happens
                                                     if (Globals.Current.PagePlayers != null)
                                                     {
                                                         t = Globals.Current.PagePlayers.tbSearchFilter as TextBox;
                                                     }
                                                     break;
                                             }

                                             SearchText = t.Text;
                                             MyData = CollectionViewSource.GetDefaultView(ViewSource.View);
                                             MyData.Filter = FilterData; //todo:aaron - fix for players page search
                                             ViewSource.View.Refresh();
                                         });
            }
        }

        public bool FilterData(object item)
        {
            if (PageType != "ArmaLauncher.Players")
            {
                // Server model search
                var server = (Server) item;

                if (server == null || server.Name == null)
                    return false;

                //return value.Name.ToLower().Contains(SearchText.ToLower())
                //    || value.Host.ToLower().Contains(SearchText.ToLower())
                //    || value.Mode.ToLower().ToString().Contains(SearchText.ToLower())
                //    || value.Mod.ToLower().ToString().Contains(SearchText.ToLower().ToString())
                //    || value.Signatures.ToLower().ToString().Contains(SearchText.ToLower().ToString())
                //    || value.Gamename.ToLower().ToString().Contains(SearchText.ToLower().ToString())
                //    || value.Mission.ToLower().ToString().Contains(SearchText.ToLower().ToString());

                if (SearchText != null && SearchText.Length > 2)
                {
                    return server.Name.ToLower().Contains(SearchText.ToLower())
                           || server.Host.ToLower().Contains(SearchText.ToLower())
                           || server.Mod.ToLower().Contains(SearchText.ToLower());
                }

                return true;
            }

            // Player model search
            var clientPlayer = (ClientPlayer) item;

            if (clientPlayer == null || clientPlayer.Player.Name == null)
                return false;

            if (SearchText != null && SearchText.Length > 2)
            {
                return clientPlayer.Player.Name.ToLower().Contains(SearchText.ToLower());
            }

            return true;
        }

        public bool FilterPlayerData(object item)
        {
            var value = (ClientPlayer)item;

            if (value == null || value.Player.Name == null)
                return false;

            //return value.Name.ToLower().Contains(SearchText.ToLower())
            //    || value.Host.ToLower().Contains(SearchText.ToLower())
            //    || value.Mode.ToLower().ToString().Contains(SearchText.ToLower())
            //    || value.Mod.ToLower().ToString().Contains(SearchText.ToLower().ToString())
            //    || value.Signatures.ToLower().ToString().Contains(SearchText.ToLower().ToString())
            //    || value.Gamename.ToLower().ToString().Contains(SearchText.ToLower().ToString())
            //    || value.Mission.ToLower().ToString().Contains(SearchText.ToLower().ToString());

            if (SearchText != null && SearchText.Length > 2)
            {
                return value.Player.Name.ToLower().Contains(SearchText.ToLower());
            }

            return true;
        }

        private void GetArmaServerList()
        {
            SteamProviderManager.GetServerList();
        }

        private void GetSingleArmaServer(Server queryServer)
        {
            const SteamLib.GameType gtype = SteamLib.GameType.Arma;
            var resultServer = new GameServer(queryServer.Host, Convert.ToInt32(queryServer.QueryPort), gtype);
            resultServer.QueryServer();

            //try to avoid blank server names
            if (string.IsNullOrEmpty(resultServer.Name))
                resultServer.Name = queryServer.Name;

            if (resultServer != null)
            {
                var serverToUpdate = new Server();

                switch (PageType)
                {
                    case "ArmaLauncher.Servers":
                        serverToUpdate = Globals.Current.ArmaServers.FirstOrDefault(i => i.Id == queryServer.Id);
                        break;
                    case "ArmaLauncher.Favorites":
                        serverToUpdate = Globals.Current.ArmaFavoriteServers.FirstOrDefault(i => i.Id == queryServer.Id);
                        break;
                    case "ArmaLauncher.Recent":
                        serverToUpdate = Globals.Current.ArmaRecentServers.FirstOrDefault(i => i.Id == queryServer.Id);
                        break;
                    case "ArmaLauncher.Notes":
                        serverToUpdate = Globals.Current.ArmaNotesServers.FirstOrDefault(i => i.Id == queryServer.Id);
                        break;
                    case "ArmaLauncher.Players":
                        //iterate through all servers across all players
                        foreach (var clientPlayer in Globals.Current.ArmaPlayers)
                        {
                            // replace any previous servers
                            serverToUpdate = clientPlayer.PlayerServers.FirstOrDefault(i => i.Id == queryServer.Id);
                            if (serverToUpdate != null)
                            {
                                Globals.Current.ViewModel.MapNewServer(resultServer, serverToUpdate);

                                // ping
                                if (serverToUpdate.Host != null)
                                    PingServerForList(serverToUpdate, false, true);

                                //MainViewModel.SavePlayer(clientPlayer);
                            }
                        }
                        break;
                }

                if (PageType != "ArmaLauncher.Players")
                {
                    if (serverToUpdate != null)
                    {
                        Globals.Current.ViewModel.MapNewServer(resultServer, serverToUpdate);
                    }
                }
            }

            //if (!IsPinging)
            //    ViewSource.View.Refresh();
        }

        private Server PingServerForList(Server server, bool isGlobalPing, bool isSinglePing)
        {
            Action action = async delegate()
            {
                ShouldPing = isSinglePing;

                if (PingCurrentIndex == 0)
                    ShouldPing = isGlobalPing;

                var isPinging = isGlobalPing && (IsPinging = true) && ShouldPing;

                var pingReply = await Task.Run(() =>
                {
                                           if (!isSinglePing)
                                           {
                                               if (!ShouldPing)
                                               {
                                                   StopPinging();
                                                   return null;
                                               }
                                           }

                                           var pingSender = new Ping();
                                           var addresss = IPAddress.Parse(server.Host);
                                           var reply = isGlobalPing
                                               ? pingSender.Send(addresss, MaxPing)
                                               : pingSender.Send(addresss);
                                           return reply;
                });

                if (pingReply != null && pingReply.Status == IPStatus.Success)
                {
                    server.Metadata.Ping = pingReply.RoundtripTime;
                }
                else if (isGlobalPing && pingReply != null && pingReply.Status == IPStatus.TimeExceeded)
                {
                    server.Metadata.Ping = 88888;
                }
                else
                {
                    server.Metadata.Ping = 99999;
                }

                //if (!isPinging)
                //    ViewSource.View.Refresh();

                if (isPinging && ShouldPing)
                {
                    // initiate process
                    if (PingCurrentIndex == 0)
                    {
                        PingRefreshGridIndex = 0;
                        PingRefreshGridShouldUpdate = true;
                    }

                    // determine target to refresh the grid - increment refresh grid index
                    if (PingRefreshGridShouldUpdate)
                    {
                        PingRefreshGridIndex = PingCurrentIndex + 50 > Globals.Current.ArmaServers.Count() - 1
                            ? Globals.Current.ArmaServers.Count() - 1
                            : PingCurrentIndex + 50;
                        PingRefreshGridShouldUpdate = false;
                    }

                    // update grid
                    if (PingCurrentIndex == PingRefreshGridIndex)
                    {
                        //ViewSource.View.Refresh();
                        PingRefreshGridShouldUpdate = true;
                    }

                    // increment index
                    PingCurrentIndex++;

                    if (PingCurrentIndex == Globals.Current.ArmaServers.Count() - 1 ||
                        PingCurrentIndex == Globals.Current.ArmaServers.Count())
                    {
                        StopPinging();
                    }
                }
                else
                {
                    // final grid refresh & reset globals
                    StopPinging();
                    //ViewSource.View.Refresh();
                }
                if (!isSinglePing)
                {
                    if (!ShouldPing)
                    {
                        StopPinging();
                    }
                }
            };
            Application.Current.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, action);

            return server;
        }

        //todo: maybe use this to improve scrolling
        private bool IsUserVisible(FrameworkElement element, FrameworkElement container)
        {
            if (!element.IsVisible)
                return false;
            Rect bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            Rect rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight);
        }

        public ActionCommand AddFavoriteClickCommand
        {
            get
            {
                return new ActionCommand(() =>
                                         {
                                             var server = new Server();
                                             new EditServerPopup(server, ViewSource, true);
                                         });
            }
        }

        public ActionCommand AddPlayerClickCommand
        {
            get
            {
                return new ActionCommand(() =>
                                         {
                                             var player = new ClientPlayer()
                                             {
                                                 Player = new Player(),
                                                 PlayerServers = new ObservableCollection<Server>()
                                             };
                                             new EditPlayerPopup(player, ViewSource, true);
                                         });
            }
        }

        public ActionCommand DeleteServerClickCommand
        {
            get
            {
                return new ActionCommand(() =>
                                         {
                                             if (DataGridSelectedItem != null)
                                             {
                                                 DataGridSelectedItem.Metadata.ShouldDelete = true;
                                                 Globals.Current.SaveRecentServerToDisk(DataGridSelectedItem);

                                                 // seems to be required when removing something from the grid
                                                 ResetViewSourceAssociations();
                                             }
                                         });
            }
        }

        public ActionCommand PlayerIsFriendClickCommand
        {
            get
            {
                return new ActionCommand(() =>
                {
                    if (DataGridSelectedPlayerItem != null)
                    {
                        DataGridSelectedPlayerItem.IsFriend = !DataGridSelectedPlayerItem.IsFriend;

                        DataGridSelectedPlayerItem.ShouldUpdate = true;

                        Globals.Current.SavePlayerToDisk(DataGridSelectedPlayerItem);

                        //ResetViewSourceAssociations();
                    }
                });
            }
        }

        public ActionCommand PlayerIsEnemyClickCommand
        {
            get {return new ActionCommand(() =>
            {
                if (DataGridSelectedPlayerItem != null)
                {
                    DataGridSelectedPlayerItem.IsEnemy = !DataGridSelectedPlayerItem.IsEnemy;

                    DataGridSelectedPlayerItem.ShouldUpdate = true;

                    Globals.Current.SavePlayerToDisk(DataGridSelectedPlayerItem);

                    //ResetViewSourceAssociations();
                }
                
            });}
        }

        public ActionCommand PlayerNotesClickCommand
        {
            get {return new ActionCommand(() =>
            {
                new EditPlayerPopup(DataGridSelectedPlayerItem, ViewSource);
            });}
        }

        public ActionCommand PlayerDeleteClickCommand
        {
            get { return new ActionCommand(() =>
            {
                if (DataGridSelectedPlayerItem != null)
                {
                    DataGridSelectedPlayerItem.ShouldDelete = true;
                    DataGridSelectedPlayerItem.ShouldUpdate = true;

                    Globals.Current.SavePlayerToDisk(DataGridSelectedPlayerItem);
                    
                    ResetViewSourceAssociations();
                }
            });}
        }

        public ActionCommand GetArmaListClickCommand
        {
            get
            {
                return new ActionCommand(() =>
                {
                    BtnGetArmaList.IsEnabled = false;
                    Globals.Current.PageServers.StopSearchingButton.Visibility = Visibility.Visible;
                    Globals.Current.PageServers.StopSearchingButton.IsEnabled = true;

                    InitializeObjects();

                    // global objects - clear these and refetch, or grid will keep the previous list and append to the end of it...
                    Globals.Current.ArmaServers.Clear();
                    Globals.Current.ArmaFavoriteServers.Clear();
                    Globals.Current.ArmaRecentServers.Clear();
                    Globals.Current.ArmaNotesServers.Clear();
                    Globals.Current.ArmaPasswordServers.Clear();

                    //clear all player's servers lists
                    foreach (var player in Globals.Current.ArmaPlayers)
                    {
                        player.IsOnline = false;
                        player.PlayerServers.Clear();
                    }

                    //Globals.Current.PageServers.MainDataGrid.Items.Clear();
                    ViewSource.View.Refresh();

                    //WARNING - DO NOT REMOVE THIS CODE and ADD TO IT ON NEW TABS... This is required to populate these server types on load on new main server list
                    // if ArmaServers is null, get last list from disk
                    if (Globals.Current.ArmaServers == null)
                        Globals.Current.GetServers();

                    //get password servers
                    Globals.Current.GetPasswordServers();

                    //get favorites
                    Globals.Current.GetFavoriteServers();

                    //get notes
                    Globals.Current.GetNotesServers();

                    //get recents
                    Globals.Current.GetRecentServers();

                    ResetViewSourceAssociations();

                    //get players -- SHOULD NOT DO THIS
                    //Globals.Current.GetPlayers();

                    HasFirstSearchInitiated = true;
                    IsSearching = true;
                    IsPinging = true;
                    PingCurrentIndex = 0;
                    Globals.Current.DeletePreviousServersFile();

                    GetArmaServerList();
                });
            }
        }

        public ActionCommand TheGridMouseDoubleClickCommand
        {
            get
            {
                return new ActionCommand(() =>
                {
                    if (DataGridSelectedItem == null) return;

                    if (DataGridSelectedItem.Metadata.EnterPasswordOnConnect == false)
                    {
                        Globals.Current.ConnectToServer(DataGridSelectedItem);
                    }
                    else
                    {
                        new PasswordPopup(DataGridSelectedItem, Application.Current.MainWindow);
                    }

                    //ping 1st
                    SteamProviderManager.PingSingleServer(DataGridSelectedItem);

                    //save / update server
                    MainViewModel.SaveServer(DataGridSelectedItem);

                    ResetViewSourceAssociations();
                });
            }
        }

        public ActionCommand PingServerClickCommand
        {
            get {return new ActionCommand(() =>
                                          {

                                              if (DataGridSelectedItem == null) return;

                                              RefreshAllServerInfo(DataGridSelectedItem);

                                              //save / update server
                                              MainViewModel.SaveServer(DataGridSelectedItem);

                                              ViewSource.View.Refresh();
                                              //IsSearching = false;
                                          });}
        }

        public void RefreshAllServerInfo(Server server)
        {
            //HasFirstSearchInitiated = true;
            //IsSearching = true;

            if (!string.IsNullOrEmpty(server.Id))
            {
                GetSingleArmaServer(server);
            }

            // ping
            if (server.Host != null)
                PingServerForList(server, false, true);
            
            ResetViewSourceAssociations();
        }

        public ActionCommand EnterPasswordOnConnectClickCommand
        {
            get
            {
                return new ActionCommand(() =>
                {
                    if (DataGridSelectedItem != null)
                    {
                        if (DataGridSelectedItem.Metadata.EnterPasswordOnConnect == false)
                            DataGridSelectedItem.Metadata.EnterPasswordOnConnect = true;
                        else
                            DataGridSelectedItem.Metadata.EnterPasswordOnConnect = false;

                        Globals.Current.SavePasswordServerToDisk(DataGridSelectedItem);
                    }
                });
            }
        }

        public ActionCommand ClickFavoriteServerCommand
        {
            get
            {
                return new ActionCommand(() =>
                {
                    if (DataGridSelectedItem != null)
                    {
                        if (DataGridSelectedItem.Metadata.IsFavorite == false)
                            DataGridSelectedItem.Metadata.IsFavorite = true;
                        else
                            DataGridSelectedItem.Metadata.IsFavorite = false;

                        Globals.Current.SaveFavoriteServerToDisk(DataGridSelectedItem);

                        //required to refresh view
                        ResetViewSourceAssociations();
                    }
                });
            }
        }

        public ActionCommand ClickNotesServerCommand
        {
            get
            {
                return new ActionCommand(() =>
                {
                    new EditServerPopup(DataGridSelectedItem, ViewSource);
                });
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            var eventToFire = PropertyChanged;
            if (eventToFire == null)
                return;

            eventToFire(this, new PropertyChangedEventArgs(propertyName));
        }

        public ActionCommand StopSearchingClickCommand
        {
            get { return new ActionCommand(() => Application.Current.Dispatcher.Invoke(() =>
            {
                SteamProviderManager.CancelServerQuery();
                StopSearching();
            }));
            }

        }

        public void StopSearching()
        {
            if (IsSearching)
            {
                IsSearching = false;
            }
            if (IsPinging)
            {
                StopPinging();
            }

            if (Globals.Current.ArmaServers == null)
                ViewSource.Source = Globals.Current.ArmaServers;

            if (!SteamProviderManager.IsQueryCancelPending())
            {
                BtnGetArmaList.IsEnabled = true;
                StopSearchingButton.Visibility = Visibility.Collapsed;
                StopSearchingButton.IsEnabled = false;
            }
        }

        public ActionCommand StopPingingClickCommand
        {
            get { return new ActionCommand(StopPinging); }
        }

        private void StopPinging()
        {
            PingCurrentIndex = 0;
            ShouldPing = false;
            IsPinging = false;
            IsGlobalPing = false;
            ResetViewSourceAssociations();
        }

        public ActionCommand DdGameTypeSelectionChangedCommand
        {
            get
            {
                return new ActionCommand(() =>
                                         {
                                             if (Globals.Current.ViewModel != null)
                                                this.PageType = Globals.Current.ViewModel.PageType;

                                             ResetCoreObjects();
                                             ResetViewSourceAssociations();
                                             InitializeObjects();
                                         });
            }
        }

        public ActionCommand PingAllArmaListClickCommand
        {
            get
            {
                return new ActionCommand(() =>
                {
                    IsPinging = true;
                    PingCurrentIndex = 0;

                    var targetServers = new ObservableCollection<Server>();
                    switch (PageType)
                    {
                        case "ArmaLauncher.Servers":
                            targetServers = Globals.Current.ArmaServers;
                            break;
                        case "ArmaLauncher.Favorites":
                            targetServers = Globals.Current.ArmaFavoriteServers;
                            break;
                        case "ArmaLauncher.Recent":
                            targetServers = Globals.Current.ArmaRecentServers;
                            break;
                        case "ArmaLauncher.Notes":
                            targetServers = Globals.Current.ArmaNotesServers;
                            break;
                        case "ArmaLauncher.Players":
                            //todo:aaron-change this when new page happens
                            var targetPlayers = new ObservableCollection<ClientPlayer>();
                            targetPlayers = Globals.Current.ArmaPlayers;
                            break;
                    }

                    foreach (var armaServer in targetServers)
                    {
                        if (IsPinging == false)
                            break;

                        try
                        {
                            //PING
                            Action action = async delegate()
                            {
                                await Task.Run(() =>
                                {
                                    var pingededServer = new Server();
                                    pingededServer = SteamProviderManager.PingSingleServer(armaServer);
                                    if (!string.IsNullOrEmpty(pingededServer.Id))
                                    {
                                        GetSingleArmaServer(pingededServer);
                                    }

                                    Application.Current.Dispatcher.Invoke((Action)delegate
                                    {
                                        SaveServer(pingededServer);
                                    });
                                    return pingededServer;
                                });
                            };
                            Application.Current.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, action);
                        }
                        catch (Exception e)
                        {
                            Globals.Current.Logger.Error(e);
                        }
                        PingCurrentIndex++;
                        ViewSource.View.Refresh();
                    }
                    StopPinging();
                });
            }
        }

        public ActionCommand PingAllPlayersArmaListClickCommand
        {
            get
            {
                return new ActionCommand(() =>
                {
                    IsPinging = true;
                    PingCurrentIndex = 0;

                    var targetPlayers = new ObservableCollection<ClientPlayer>();
                    switch (PageType)
                    {
                        case "ArmaLauncher.Players":
                            //todo:aaron-change this when new page happens
                            targetPlayers = Globals.Current.ArmaPlayers;
                            break;
                    }

                    foreach (var targetPlayer in targetPlayers)
                    {
                        //get servers
                        var targetServers = targetPlayer.PlayerServers;

                        foreach (var armaServer in targetServers)
                        {
                            if (IsPinging == false)
                                break;

                            try
                            {
                                //PING
                                Action action = async delegate()
                                {
                                    await Task.Run(() =>
                                    {
                                        var pingededServer = new Server();
                                        pingededServer = SteamProviderManager.PingSingleServer(armaServer) ;
                                        if (!string.IsNullOrEmpty(pingededServer.Id))
                                        {
                                            GetSingleArmaServer(pingededServer);
                                        }

                                        // replace old server data with new data
                                        var server = targetPlayer.PlayerServers.FirstOrDefault(i => i.Id == pingededServer.Id);
                                        if (server != null)
                                        {
                                            pingededServer = server;
                                        }

                                        return pingededServer;
                                    });
                                };
                                Application.Current.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, action);
                            }
                            catch (Exception e)
                            {
                                Globals.Current.Logger.Error(e);
                            }
                        }


                        PingCurrentIndex++;
                    }

                    IsPinging = false;

                    //if (!IsPinging)
                    //{
                    //    Application.Current.Dispatcher.Invoke((Action) delegate
                    //    {
                    //        foreach (var player in targetPlayers)
                    //        {
                    //            //save player
                    //            SavePlayer(player);
                    //        }
                    //    });
                    //}
                    ViewSource.View.Refresh();
                });
            }
        }

        public ActionCommand ClickPlayersImageCommand
        {
            get
            {
                return new ActionCommand(() =>
                                         {
                                             if (
                                                 DataGridSelectedItem == null) return;

                                             //Globals.Current.LoadingWindow =
                                             //    new LoadingWindow(Application.Current.MainWindow);
                                             //Globals.Current.LoadingWindow.Show();

                                             Helpers.UiServices.SetBusyState();

                                             RefreshAllServerInfo(DataGridSelectedItem);

                                             new PlayersPopup(DataGridSelectedItem, Application.Current.MainWindow, this.PageType);
                                         });
            }
        }
    }
}