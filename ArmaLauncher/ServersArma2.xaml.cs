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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using ArmaLauncher.Controls;
using ArmaLauncher.Models;
using ArmaLauncher.Properties;
using SteamLib;
using SteamLib.MasterServer;
using GameType = ArmaLauncher.Models.GameType;

namespace ArmaLauncher
{
    /// <summary>
    ///     Interaction logic for Servers.xaml
    /// </summary>
    public partial class ServersArma2 : Page, INotifyPropertyChanged
    {
        private SourceMasterServer masterQuery;
        private ICollectionView MyData;
        private string SearchText = string.Empty;
        private Server _dataGridSelectedItem;
        private bool _hasFirstSearchInitiated;
        private bool _isGlobalPing;
        private bool _isPinging;
        private bool _isSearching;
        private int _pingCurrentIndex;
        private int _pingRefreshGridIndex;
        private bool _pingRefreshGridShouldUpdate;
        private bool _shouldPing;

        public ServersArma2()
        {
            InitializeObjects();
        }

        private void InitializeObjects()
        {
            InitializeComponent();

            //reset grid
            dgServers.AutoGenerateColumns = true;
            dgServers.AutoGenerateColumns = false;
            //var temp = dgServers.ItemsSource;
            //dgServers.ItemsSource = null;
            //dgServers.Items.Clear();
            //dgServers.ItemsSource = temp;

            // global objects
            Globals.Current.ArmaServers = new List<Server>();
            Globals.Current.ServerList = new ObservableCollection<Server>();
            Globals.Current.ArmaFavoriteServers = new List<Server>();
            Globals.Current.ArmaRecentServers = new List<Server>();
            Globals.Current.ArmaNotesServers = new List<Server>();

            // local objects
            DataContext = this;
            HasFirstSearchInitiated = false;
            IsSearching = false;
            _gridFirstTimeLoaded = true;

            masterQuery = new SourceMasterServer();
            //masterQuery.SynchronizingObject = this;
            masterQuery.QueryAsyncCompleted += SourceMasterServer_QueryAsyncCompleted;
        }

        public bool SelectedItemAutoMatch { get; set; }
        public List<string> SelectedItemClientMods { get; set; }

        public List<string> ListOfMods
        {
            get
            {
                if (DataGridSelectedItem == null)
                    return null;

                if (DataGridSelectedItem.Metadata.ArmaGameType == GameType.Arma2)
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
                int def = Convert.ToInt32(ConfigurationManager.AppSettings["DefaultMaxPing"]);
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

        public GameType ArmaGameType { get; set; }

        public Server DataGridSelectedItem
        {
            get { return _dataGridSelectedItem; }
            set
            {
                _dataGridSelectedItem = value;
                dgServers.UpdateLayout();
                OnPropertyChanged("DataGridSelectedItem");
                OnPropertyChanged("ServerList");
                OnPropertyChanged("SelectedItemAutoMatch");
                OnPropertyChanged("SelectedItemClientMods");
                OnPropertyChanged("ListOfMods");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var t = sender as TextBox;
            SearchText = t.Text;
            MyData = CollectionViewSource.GetDefaultView(Globals.Current.ArmaServers);
            MyData.Filter = FilterData;
            dgServers.Items.Refresh();
        }

        private bool FilterData(object item)
        {
            var value = (Server) item;

            if (value == null || value.Name == null)
                return false;

            //return value.Name.ToLower().Contains(SearchText.ToLower())
            //    || value.Host.ToLower().Contains(SearchText.ToLower())
            //    || value.Mode.ToLower().ToString().Contains(SearchText.ToLower())
            //    || value.Mod.ToLower().ToString().Contains(SearchText.ToLower().ToString())
            //    || value.Signatures.ToLower().ToString().Contains(SearchText.ToLower().ToString())
            //    || value.Gamename.ToLower().ToString().Contains(SearchText.ToLower().ToString())
            //    || value.Mission.ToLower().ToString().Contains(SearchText.ToLower().ToString());

            return value.Name.ToLower().Contains(SearchText.ToLower())
                   || value.Host.ToLower().Contains(SearchText.ToLower())
                   || value.Mod.ToLower().Contains(SearchText.ToLower());
        }

        //private void GetArma2ServerList()
        //{
        //    tbSearchFilter.Text = string.Empty;
        //    Globals.Current.ServerList = new ObservableCollection<queryServer>();
        //    var uri = (Convert.ToBoolean(ConfigurationManager.AppSettings["DebugTestLocal"])) ? @"http://localhost/arma2feed.xml" : "http://arma2.swec.se/queryServer/list.xml?country=&mquery=&nquery=&state_playing=1&state_waiting=1"; //&state_down=1
        //    var query = string.Empty;
        //    var request = new RestRequest {Resource = query};

        //    GetArmaServers(request, uri);
        //}

        private void GetArma2ServerList()
        {
            string textMap = string.Empty;
            string textMod = "arma2arrowpc";

            masterQuery.QueryAsync(SourceMasterServer.QueryGame.Arma,
                SourceMasterServer.QueryRegionCode.All,
                SourceMasterServer.QueryFilter.None,
                textMap.Trim(),
                textMod.Trim(),
                SourceMasterQuery_Callback);
        }
        private void GetArma3ServerList()
        {
            string textMap = string.Empty;
            string textMod = "arma3";

            masterQuery.QueryAsync(SourceMasterServer.QueryGame.Arma,
                SourceMasterServer.QueryRegionCode.All,
                SourceMasterServer.QueryFilter.None,
                textMap.Trim(),
                textMod.Trim(),
                SourceMasterQuery_Callback);
        }

        private void GetSingleArmaServer(Server queryServer)
        {
            const SteamLib.GameType gtype = SteamLib.GameType.Arma;
            var resultServer = new GameServer(queryServer.Host, Convert.ToInt32(queryServer.QueryPort), gtype);
            resultServer.QueryServer();

            if (resultServer != null)
            {
                Server serverToUpdate = Globals.Current.ArmaServers.FirstOrDefault(i => i.Id == queryServer.Id);
                if (serverToUpdate != null)
                {
                    //update other stuff
                    serverToUpdate.Name = resultServer.Name.Replace("\r", "").Replace("\n", "");
                    serverToUpdate.Mod = resultServer.Mod.Replace("\r", "").Replace("\n", "");
                    serverToUpdate.NumPlayers = resultServer.NumPlayers;
                    //serverToUpdate.state = queryServer.state;
                    serverToUpdate.Game.Players = resultServer.Players;
                }
            }
            dgServers.Items.Refresh();
        }

        //private void GetSingleArmaServer(string uri, int? serverId)
        //{
        //    var query = string.Empty;
        //    var request = new RestRequest { Resource = query };
        //    ExecuteGetSingleArmaServer(request, uri, serverId);
        //}

        //private async void ExecuteGetSingleArmaServer(RestRequest request, string uri, int? serverId)
        //{
        //    var update = await Task.Run(() =>
        //    {
        //        var result = ApiManager.Execute<server>(request, uri, null, null);
        //        return result;
        //    });

        //    if (update != null)
        //    {
        //        var serverToUpdate = Globals.Current.ServerList.FirstOrDefault(i => i.id == serverId);
        //        if (serverToUpdate != null)
        //        {
        //            serverToUpdate.name = serverToUpdate.name.Replace("\r", "").Replace("\n", "");
        //            serverToUpdate.mod = serverToUpdate.mod.Replace("\r", "").Replace("\n", "");
        //            serverToUpdate.numplayers = update.numplayers;
        //            serverToUpdate.state = update.state;
        //            serverToUpdate.game = update.game;
        //        }
        //    }
        //    this.dgServers.Items.Refresh();
        //}

        private async void PingServerForList(Server server, bool isGlobalPing, bool isSinglePing)
        {
            ShouldPing = isSinglePing;

            if (PingCurrentIndex == 0)
                ShouldPing = isGlobalPing;

            bool isPinging = isGlobalPing && (IsPinging = true) && ShouldPing;

            PingReply pingReply = await Task.Run(() =>
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
                IPAddress addresss = IPAddress.Parse(server.Host);
                PingReply reply = isGlobalPing ? pingSender.Send(addresss, MaxPing) : pingSender.Send(addresss);
                return reply;
            });

            if (!isSinglePing)
            {
                if (!ShouldPing)
                {
                    StopPinging();
                    return;
                }
            }

            if (pingReply != null && pingReply.Status == IPStatus.Success)
            {
                Server serverToUpdate = Globals.Current.ArmaServers.FirstOrDefault(i => i.Id == server.Id);
                if (serverToUpdate != null)
                {
                    serverToUpdate.Metadata.Ping = pingReply.RoundtripTime;
                    //serverToUpdate.Metadata.PingDisplay = pingReply.RoundtripTime.ToString(CultureInfo.InvariantCulture);
                }
            }
            else if (isGlobalPing && pingReply != null && pingReply.Status == IPStatus.TimeExceeded)
            {
                Server serverToUpdate = Globals.Current.ArmaServers.FirstOrDefault(i => i.Id == server.Id);
                if (serverToUpdate != null)
                {
                    serverToUpdate.Metadata.Ping = 88888;
                    //serverToUpdate.Metadata.PingDisplay = "max_ping";
                }
            }
            else
            {
                Server serverToUpdate = Globals.Current.ArmaServers.FirstOrDefault(i => i.Id == server.Id);
                if (serverToUpdate != null)
                {
                    serverToUpdate.Metadata.Ping = 99999;
                    //serverToUpdate.Metadata.PingDisplay = "timeout";
                }
            }

            if (!isPinging)
                dgServers.Items.Refresh();

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
                    dgServers.Items.Refresh();
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
                dgServers.Items.Refresh();
            }
        }

        private Server PingServerSingle(Server server)
        {
            try
            {
                Dispatcher.Invoke((Action)(async () =>
                {
                    PingReply pingReply = await Task.Run(() =>
                    {
                        var pingSender = new Ping();
                        IPAddress addresss = IPAddress.Parse(server.Host);
                        PingReply reply = pingSender.Send(addresss);
                        return reply;
                    });

                    if (pingReply != null && pingReply.Status == IPStatus.Success)
                    {
                        server.Metadata.Ping = pingReply.RoundtripTime;
                    }
                    else if (pingReply != null && pingReply.Status == IPStatus.TimeExceeded)
                    {
                        server.Metadata.Ping = 88888;
                    }
                    else
                    {
                        server.Metadata.Ping = 99999;
                    }
                }));
            }
            catch (Exception e)
            {
                
            }

            return server;
        }

        private void SourceMasterQuery_Callback(object sender, SourceMasterServer.QueryAsyncEventArgs e)
        {
            Action action = async delegate()
            {
                await ProcessServers(e);
            };

            Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, action);
        }

        private async Task ProcessServers(SourceMasterServer.QueryAsyncEventArgs e)
        {
            //todo:areed - fix slow async UI
            try
            {
                Dispatcher.Invoke(async () =>
                                    {
                                        var newServer = await Task.Run(() => GetServer(e));
                                        await GetExtendedServerDetail(newServer);
                                    });
            }
            catch (Exception exception)
            {
                //Debug.Write(exception);
            }
        }

        private static Server GetServer(SourceMasterServer.QueryAsyncEventArgs e)
        {
            var server = new Server();
            server.Id = string.Format("{0}{1}", e.GameServer.Host.Replace(".", ""), e.GameServer.QueryPort);
            server.Passworded = e.GameServer.Passworded;
            server.Mod = e.GameServer.Mod != null ? e.GameServer.Mod.Replace("\r", "").Replace("\n", "") : string.Empty;
            server.Name = e.GameServer.Name != null ? e.GameServer.Name.Replace("\r", "").Replace("\n", "") : string.Empty;
            server.Island = e.GameServer.Map;
            server.NumPlayers = e.GameServer.NumPlayers;
            server.MaxPlayers = e.GameServer.MaxPlayers;
            server.DisplayNumPlayers = string.Format("{0} of {1}", server.NumPlayers, server.MaxPlayers);
            server.Game.Players = e.GameServer.Players;
            server.Host = e.GameServer.Host;
            server.QueryPort = e.GameServer.QueryPort;
            server.Name = e.GameServer.Name;

            foreach (DictionaryEntry param in e.GameServer.Parameters)
            {
                switch (param.Key.ToString())
                {
                    case "serverport":
                        server.GamePort = Convert.ToInt32(param.Value);
                        break;
                    case "modhashes:1-2":
                        server.Modhashes12 = param.Value.ToString();
                        break;
                    case "signames:0-1":
                        server.Signames01 = param.Value.ToString().Replace(";", "; ");
                        break;
                    case "timeleft":
                        server.Timeleft = param.Value.ToString();
                        break;
                    case "secureserver":
                        server.Secureserver = param.Value.ToString();
                        break;
                    case "botcount":
                        server.Botcount = param.Value.ToString();
                        break;
                    case "appid":
                        server.Appid = param.Value.ToString();
                        break;
                        //case "hostname":
                    case "gameversion":
                        server.Gameversion = param.Value.ToString();
                        break;
                        //case "mod":
                    case "hash":
                        server.Hash = param.Value.ToString();
                        break;
                        //case "maxplayers":
                    case "param2":
                        server.Param2 = param.Value.ToString();
                        break;
                        //case "numplayers":
                    case "mapname":
                        server.Mapname = param.Value.ToString();
                        break;
                    case "modnames:0-1":
                        server.Modnames01 = param.Value.ToString().Replace(";", "; ");
                        break;
                    case "players":
                        server.Players = param.Value.ToString();
                        break;
                    case "modhashes:0-2":
                        server.Modhashes02 = param.Value.ToString();
                        break;
                    case "modname":
                        server.Modname = param.Value.ToString();
                        break;
                    case "protocolver":
                        server.Protocolver = param.Value.ToString();
                        break;
                        //case "passworded":
                    case "serveros":
                        server.Serveros = param.Value.ToString();
                        break;
                    case "servertype":
                        server.Param1 = param.Value.ToString();
                        break;
                    case "param1":
                        server.Servertype = param.Value.ToString();
                        break;
                }
            }

            return server;
        }

        private async Task<bool> GetExtendedServerDetail(Server newServer)
        {
            // if we did not get a GamePort, guess at GamePort based off of QueryPort
            if (newServer.GamePort == 0)
            {
                if (newServer.QueryPort == null && newServer.QueryPort == 0)
                    return true;

                newServer.GamePort = (int)(newServer.QueryPort - 1);
            }

            //PING
            Action action = async delegate()
            {
                await Task.Run(() =>
                {
                    var pingededServer = new Server();
                    pingededServer = PingServerSingle(newServer);
                    return pingededServer;
                });
            };
            Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, action);

            //newServer = await Task.Run(() =>
            //                           {
            //                               var pingededServer = new Server();
            //                               pingededServer = PingServerSingle(newServer);
            //                               return pingededServer;
            //                           });


            string armaPasswordServersIds = string.Empty;

            if (Globals.Current.ArmaPasswordServers == null)
                Globals.Current.ArmaPasswordServers = new List<Server>();

            // PERF: get IDs for better perf
            foreach (Server server in Globals.Current.ArmaPasswordServers)
            {
                if (server.Metadata.ArmaGameType == ArmaGameType)
                {
                    if (server == Globals.Current.ArmaPasswordServers.Last())
                    {
                        armaPasswordServersIds += server.Id;
                    }
                    else
                    {
                        armaPasswordServersIds += server.Id + ",";
                    }
                }
            }

            string armaFavoriteServersIds = string.Empty;

            if (Globals.Current.ArmaFavoriteServers == null)
                Globals.Current.ArmaFavoriteServers = new List<Server>();

            // PERF: get IDs for better perf
            foreach (Server server in Globals.Current.ArmaFavoriteServers)
            {
                if (server.Metadata.ArmaGameType == ArmaGameType)
                {
                    if (server == Globals.Current.ArmaFavoriteServers.Last())
                    {
                        armaFavoriteServersIds += server.Id;
                    }
                    else
                    {
                        armaFavoriteServersIds += server.Id + ",";
                    }
                }
            }

            string armaNoteServersIds = string.Empty;

            if (Globals.Current.ArmaNotesServers == null)
                Globals.Current.ArmaNotesServers = new List<Server>();

            // PERF: get IDs for better perf
            foreach (Server server in Globals.Current.ArmaNotesServers)
            {
                if (server.Metadata.ArmaGameType == ArmaGameType)
                {
                    if (server == Globals.Current.ArmaNotesServers.Last())
                    {
                        armaNoteServersIds += server.Id;
                    }
                    else
                    {
                        armaNoteServersIds += server.Id + ",";
                    }
                }
            }

            string armaRecentServersIds = string.Empty;

            if (Globals.Current.ArmaRecentServers == null)
                Globals.Current.ArmaRecentServers = new List<Server>();

            // PERF: get IDs for better perf
            foreach (Server server in Globals.Current.ArmaRecentServers)
            {
                if (server.Metadata.ArmaGameType == ArmaGameType)
                {
                    if (server == Globals.Current.ArmaRecentServers.Last())
                    {
                        armaRecentServersIds += server.Id;
                    }
                    else
                    {
                        armaRecentServersIds += server.Id + ",";
                    }
                }
            }

            // mark passworded servers from feed
            if (newServer.Passworded == true)
                newServer.Metadata.EnterPasswordOnConnect = true;

            // mark passworded servers from disk
            if (armaPasswordServersIds.Contains(newServer.Id))
            {
                newServer.Metadata.EnterPasswordOnConnect = true;
                Server server =
                    Globals.Current.ArmaPasswordServers.FirstOrDefault(i => i.Id == newServer.Id);
                if (server != null)
                {
                    newServer.Metadata.ServerPassword = server.Metadata.ServerPassword;
                }
            }

            // mark favorites
            if (armaFavoriteServersIds.Contains(newServer.Id))
                newServer.Metadata.IsFavorite = true;

            // mark notes
            if (armaNoteServersIds.Contains(newServer.Id))
            {
                newServer.Metadata.HasNotes = true;
                Server server =
                    Globals.Current.ArmaNotesServers.FirstOrDefault(i => i.Id == newServer.Id);
                if (server != null)
                {
                    newServer.Metadata.Notes = server.Metadata.Notes;
                }
            }

            // mark recent dates
            if (armaRecentServersIds.Contains(newServer.Id))
            {
                Server server =
                    Globals.Current.ArmaRecentServers.FirstOrDefault(i => i.Id == newServer.Id);
                if (server != null)
                {
                    newServer.Metadata.LastPlayedDate = server.Metadata.LastPlayedDate;
                }
            }

            // update Arma game type metadata
            newServer.Metadata.ArmaGameType = ArmaGameType;

            // FINALLY add server to global model 
            if (newServer != null)
                Globals.Current.ArmaServers.Add(newServer);

            // cache result
            if (Globals.Current.ArmaServers != null)
            {
                Globals.Current.SaveServersToDisk(Globals.Current.ArmaServers, ArmaGameType);
            }

            // if ArmaServers is null, get last list from disk
            if (Globals.Current.ArmaServers == null)
                Globals.Current.ArmaServers = Globals.Current.GetServers(ArmaGameType);

            //get password servers
            Globals.Current.GetPasswordServers();

            //get favorites
            Globals.Current.GetFavoriteServers();

            //get notes
            Globals.Current.GetNotesServers();

            //get recents
            Globals.Current.GetRecentServers();

            //refresh grid
            dgServers.Items.Refresh();

            return false;
        }

        private void btnGetArma2List_Click(object sender, RoutedEventArgs e)
        {
            InitializeObjects();

            HasFirstSearchInitiated = true;
            IsSearching = true;
            PingCurrentIndex = 0;
            ArmaGameType = GameType.Arma2;
            GetArma2ServerList();
        }

        //private void btnGetArma3List_Click(object sender, RoutedEventArgs e)
        //{
        //    InitializeObjects();

        //    HasFirstSearchInitiated = true;
        //    IsSearching = true;
        //    PingCurrentIndex = 0;
        //    ArmaGameType = GameType.Arma3;
        //    GetArma3ServerList();
        //}

        private void dgServers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataGridSelectedItem == null) return;

            if (DataGridSelectedItem.Metadata.EnterPasswordOnConnect == false)
            {
                Globals.Current.ConnectToServer(DataGridSelectedItem);
            }
            else
            {
                new PasswordUserControl(DataGridSelectedItem);
            }
        }

        private void PingServer_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridSelectedItem == null) return;

            //HasFirstSearchInitiated = true;
            //IsSearching = true;

            if (!string.IsNullOrEmpty(DataGridSelectedItem.Id))
            {
                if (DataGridSelectedItem.Metadata.ArmaGameType == GameType.Arma2)
                {
                    //GetSingleArmaServer("http://arma2.swec.se/queryServer/xml/" + DataGridSelectedItem.Id,DataGridSelectedItem.id);
                    GetSingleArmaServer(DataGridSelectedItem);
                }

                if (DataGridSelectedItem.Metadata.ArmaGameType == GameType.Arma3)
                {
                    //GetSingleArmaServer("http://arma3.swec.se/queryServer/xml/" + DataGridSelectedItem.Id,DataGridSelectedItem.id);
                    GetSingleArmaServer(DataGridSelectedItem);
                }
            }

            // ping
            if (DataGridSelectedItem.Host != null)
                PingServerForList(DataGridSelectedItem, false, true);

            dgServers.Items.Refresh();
            //IsSearching = false;
        }

        private void Click_EnterPasswordOnConnect(object sender, RoutedEventArgs e)
        {
            bool result = false;
            var control = sender as CheckBox;
            if (control != null)
            {
                result = control.IsChecked != null && control.IsChecked == true;
            }

            if (DataGridSelectedItem != null)
            {
                DataGridSelectedItem.Metadata.EnterPasswordOnConnect = result;

                Globals.Current.SavePasswordServersToDisk(DataGridSelectedItem);
            }
        }

        private void Clicked_FavoriteServer(object sender, RoutedEventArgs e)
        {
            bool result = false;
            var control = sender as CheckBox;
            if (control != null)
            {
                result = control.IsChecked != null && control.IsChecked == true;
            }

            if (DataGridSelectedItem != null)
            {
                DataGridSelectedItem.Metadata.IsFavorite = result;

                Globals.Current.SaveFavoriteServersToDisk(DataGridSelectedItem);
            }
        }

        private void Click_NotesServer(object sender, RoutedEventArgs e)
        {
            var control = sender as CheckBox;

            new NotesUserControl(control, DataGridSelectedItem);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler eventToFire = PropertyChanged;
            if (eventToFire == null)
                return;

            eventToFire(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            MaxPing = slider != null
                ? Convert.ToInt32(slider.Value)
                : Convert.ToInt32(ConfigurationManager.AppSettings["DefaultMaxPing"]);
        }

        private void SourceMasterServer_QueryAsyncCompleted(object sender, EventArgs e)
        {
            StopSearching();
        }

        private void Click_StopSearching(object sender, RoutedEventArgs e)
        {
            masterQuery.CancelAsyncQuery();
            StopSearching();
        }

        private void StopSearching()
        {
            if (IsSearching)
            {
                IsSearching = false;
            }
        }

        private void Click_StopPinging(object sender, RoutedEventArgs e)
        {
            StopPinging();
        }

        private void StopPinging()
        {
            if (IsGlobalPing && IsPinging)
            {
                ShouldPing = false;
                IsPinging = false;
                IsGlobalPing = false;
            }
        }
    }
}