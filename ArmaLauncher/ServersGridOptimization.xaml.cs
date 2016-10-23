using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using ArmaLauncher.Controls;
using ArmaLauncher.Helpers;
using ArmaLauncher.Models;
using Microsoft.Practices.ServiceLocation;
using RestSharp;

namespace ArmaLauncher
{
    /// <summary>
    /// Interaction logic for Servers.xaml
    /// </summary>
    public partial class ServersGridOptimization : Page, INotifyPropertyChanged
    {
        private int pageSize;
        public int PageSize
        {
            get
            {
                return pageSize;
            }
            set
            {
                pageSize = value;
                OnPropertyChanged("PageSize");
            }
        }

        private int pageTimeout;
        public int PageTimeout
        {
            get
            {
                return pageTimeout;
            }
            set
            {
                pageTimeout = value;
                OnPropertyChanged("PageTimeout");
            }
        }

        private int numItems;
        public int NumItems
        {
            get
            {
                return numItems;
            }
            set
            {
                numItems = value;
                OnPropertyChanged("NumItems");
            }
        }

        private int fetchDelay;
        public int FetchDelay
        {
            get
            {
                return fetchDelay;
            }
            set
            {
                fetchDelay = value;
                OnPropertyChanged("FetchDelay");
            }
        }

        private bool _gridFirstTimeLoaded { get; set; }
        private bool _HasFirstSearchInitiated;
        public bool HasFirstSearchInitiated
        {
            get { return _HasFirstSearchInitiated; }
            set
            {
                _HasFirstSearchInitiated = value;
                OnPropertyChanged("HasFirstSearchInitiated");
                OnPropertyChanged("IsSearching");
            }
        }

        private bool _isSearching;
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

        public GameType ArmaGameType { get; set; }

        public string ErrorServiceGet { get; set; }

        private server _dataGridSelectedItem;

        public server DataGridSelectedItem
        {
            get
            {
                return _dataGridSelectedItem;
            }
            set
            {
                _dataGridSelectedItem = value;
                this.dgServers.UpdateLayout();
                OnPropertyChanged("DataGridSelectedItem");
                OnPropertyChanged("ServerList");
            }
        }

        private ICollectionView MyData;
        private string SearchText = string.Empty;

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox t = sender as TextBox;
            SearchText = t.Text.ToString();
            MyData = CollectionViewSource.GetDefaultView(Globals.Current.ServerList);
            MyData.Filter = FilterData;
            this.dgServers.Items.Refresh();
        }
        private bool FilterData(object item)
        {
            var value = (server)item;

            if (value == null || value.name == null)
                return false;

            return value.name.ToLower().Contains(SearchText.ToLower())
                || value.host.ToLower().Contains(SearchText.ToLower())
                || value.mode.ToLower().ToString().Contains(SearchText.ToLower())
                || value.mod.ToLower().ToString().Contains(SearchText.ToLower().ToString())
                || value.signatures.ToLower().ToString().Contains(SearchText.ToLower().ToString())
                || value.gamename.ToLower().ToString().Contains(SearchText.ToLower().ToString())
                || value.mission.ToLower().ToString().Contains(SearchText.ToLower().ToString());
        }

        public ServersGridOptimization()
        {
            InitializeComponent();
            this.DataContext = this;
            this.HasFirstSearchInitiated = false;
            this.IsSearching = false;
            _gridFirstTimeLoaded = true;
        }

        private void GetArma2ServerList()
        {
            tbSearchFilter.Text = string.Empty;
            Globals.Current.ServerList = new ObservableCollection<server>();
            var uri = (Convert.ToBoolean(ConfigurationManager.AppSettings["DebugTestLocal"])) ? @"http://localhost/arma2feed.xml" : "http://arma2.swec.se/server/list.xml?country=&mquery=&nquery=&state_playing=1&state_waiting=1"; //&state_down=1
            var query = string.Empty;
            var request = new RestRequest {Resource = query};

            GetArmaServers(request, uri);
        }

        private server GetSingleArmaServer(string uri)
        {
            var serverInfo = new server();
            var query = string.Empty;
            var request = new RestRequest {Resource = query};
            GetSingleArmaServer(request, uri);
            return serverInfo;
        }

        private async void GetSingleArmaServer(RestRequest request, string uri)
        {
            var update = await Task.Run(() =>
                                                {
                                                    var result = ApiManager.Execute<server>(request, uri, null, null);
                                                    return result;
                                                });

            if (update != null)
            {
                var serverToUpdate = Globals.Current.ServerList.FirstOrDefault(i => i.id == DataGridSelectedItem.id);
                if (serverToUpdate != null)
                {
                    serverToUpdate.name = serverToUpdate.name.TrimStart('\r', '\n').TrimEnd('\r', '\n');
                    serverToUpdate.players = update.players;
                    serverToUpdate.state = update.state;
                    serverToUpdate.game = update.game;
                }
            }
            this.dgServers.Items.Refresh();
        }

        private async void PingServer(server server)
        {
            var pingReply = await Task.Run(() =>
                                                {
                                                    var pingSender = new Ping();
                                                    var addresss = IPAddress.Parse(server.host);
                                                    var reply = pingSender.Send(addresss);
                                                    return reply;
                                                });

            if (pingReply != null && pingReply.Status == IPStatus.Success)
            {
                var serverToUpdate = Globals.Current.ServerList.FirstOrDefault(i => i.id == server.id);
                if (serverToUpdate != null)
                {
                    serverToUpdate.Metadata.Ping = pingReply.RoundtripTime;
                }
            }
            else
            {
                var serverToUpdate = Globals.Current.ServerList.FirstOrDefault(i => i.id == server.id);
                if (serverToUpdate != null)
                {
                    serverToUpdate.Metadata.Ping = 10000;
                }
            }
            this.dgServers.Items.Refresh();
        }

        private void GetArma3ServerList()
        {
            tbSearchFilter.Text = string.Empty;
            Globals.Current.ServerList = new ObservableCollection<server>();
            var uri = (Convert.ToBoolean(ConfigurationManager.AppSettings["DebugTestLocal"])) ? @"http://localhost/arma3feed.xml" : "http://arma3.swec.se/server/list.xml?country=&mquery=&nquery=&state_playing=1&state_waiting=1"; //&state_down=1
            var query = string.Empty;
            var request = new RestRequest {Resource = query};

            GetArmaServers(request, uri);
        }

        private async void GetArmaServers(RestRequest request, string uri)
        {
            Globals.Current.ArmaServers = await Task.Run(() =>
                                                        {
                                                            var result = ApiManager.Execute<List<server>>(request, uri, null, null);

                                                            // cache result
                                                            if(result != null)
                                                            {
                                                                Globals.Current.SaveServersToDisk(result, ArmaGameType);
                                                            }

                                                            return result;
                                                        });

            // if ArmaServers is null, get last list from disk
            if (Globals.Current.ArmaServers == null)
                Globals.Current.ArmaServers = Globals.Current.GetServers(ArmaGameType);

            Globals.Current.GetFavoriteServers();

            // add to client side model
            if (Globals.Current.ArmaServers != null)
            {
                foreach (var armaServer in Globals.Current.ArmaServers)
                {
                    server newArmaServerInfo = null;

                    // get server info
                    if (Globals.Current.RefreshExtended && armaServer.players != 0 &&
                        !string.IsNullOrEmpty(armaServer.id.ToString()))
                        newArmaServerInfo = GetSingleArmaServer(armaServer.id.ToString());


                    if (newArmaServerInfo == null)
                        newArmaServerInfo = armaServer;

                    newArmaServerInfo.name = newArmaServerInfo.name.Replace("\r", "").Replace("\n", "");

                    //ping
                    if (Globals.Current.RefreshPings)
                        PingServer(newArmaServerInfo);

                    //get favorites
                    Globals.Current.GetFavoriteServers();

                    //set up favorite info
                    if (Globals.Current.ArmaFavoriteServers != null)
                    {
                        foreach (var favoriteServer in Globals.Current.ArmaFavoriteServers)
                        {
                            if (newArmaServerInfo.id == favoriteServer.id)
                            {
                                newArmaServerInfo.Metadata.IsFavorite = true;
                                break;
                            }
                        }
                    }

                    //get notes
                    Globals.Current.GetNotesServers();

                    //set up notes
                    if (Globals.Current.ArmaNotesServers != null)
                    {
                        foreach (var notesServer in Globals.Current.ArmaNotesServers)
                        {
                            if (newArmaServerInfo.id == notesServer.id)
                            {
                                newArmaServerInfo.Metadata.Notes = notesServer.Metadata.Notes;
                                newArmaServerInfo.Metadata.HasNotes = true;
                                break;
                            }
                        }
                    }

                    newArmaServerInfo.Metadata.ArmaGameType = ArmaGameType;

                    Globals.Current.ServerList.Add(newArmaServerInfo);
                    this.dgServers.Items.Refresh();
                    IsSearching = false;
                }
            }

            GridSettings();
        }

        private void GridSettings()
        {
            this.PageSize = 100; // 100 items per page
            this.PageTimeout = 100000; // 100 seconds before it times out
            this.NumItems = Globals.Current.ServerList.Count; // loads 100,000 items
            this.FetchDelay = 1000; // 1 second delay per "database" query

            var serverProvider = new ArmaServerProvider(numItems, fetchDelay);
            dgServers.DataContext = new AsyncVirtualizingCollection<server>(serverProvider, pageSize, pageTimeout);
        }

        private void btnGetArma2List_Click(object sender, RoutedEventArgs e)
        {
            HasFirstSearchInitiated = true;
            IsSearching = true;
            ArmaGameType = GameType.Arma2;
            GetArma2ServerList();
        }

        private void btnGetArma3List_Click(object sender, RoutedEventArgs e)
        {
            HasFirstSearchInitiated = true;
            IsSearching = true;
            ArmaGameType = GameType.Arma3;
            GetArma3ServerList();
        }

        private void dgServers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataGridSelectedItem.passworded == false)
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
            if (DataGridSelectedItem == null || !string.IsNullOrEmpty(DataGridSelectedItem.id.ToString())) return;

            HasFirstSearchInitiated = true;
            IsSearching = true;

            if (DataGridSelectedItem.Metadata.ArmaGameType == GameType.Arma2)
                GetSingleArmaServer("http://arma2.swec.se/server/xml/" + DataGridSelectedItem.id.ToString());

            if (DataGridSelectedItem.Metadata.ArmaGameType == GameType.Arma3)
                GetSingleArmaServer("http://arma3.swec.se/server/xml/" + DataGridSelectedItem.id.ToString());

            // ping
            PingServer(DataGridSelectedItem);
            IsSearching = false;
        }

        private void Checked_FavoriteServer(object sender, RoutedEventArgs e)
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var eventToFire = PropertyChanged;
            if (eventToFire == null)
                return;

            eventToFire(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
