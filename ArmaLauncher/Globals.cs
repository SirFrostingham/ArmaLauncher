using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using ArmaLauncher.Controls;
using ArmaLauncher.Helpers;
using ArmaLauncher.Models;
using ArmaLauncher.Properties;
using ArmaLauncher.ViewModel;
using FirstFloor.ModernUI.Presentation;
using FirstFloor.ModernUI.Windows.Navigation;
using Newtonsoft.Json;
using NLog;
using Formatting = System.Xml.Formatting;
using JsonSerializer = RestSharp.Serializers.JsonSerializer;

namespace ArmaLauncher
{
    public class Globals : INotifyPropertyChanged
    {
        private static Globals _globals;

        public static Globals Current
        {
            get
            {
                if (_globals == null)
                    _globals = new Globals();

                return _globals;
            }
        }

        public SteamProviderType SteamProviderType { get; set; }
        public Servers PageServers { get; set; }
        public Favorites PageFavorites { get; set; }
        public Recent PageRecent { get; set; }
        public Notes PageNotes { get; set; }
        public Players PagePlayers { get; set; }

        /// <summary>
        /// USAGE: pass string parameter of page.
        /// EXAMPLE: "/AppSettings.xaml"
        /// </summary>
        /// <param name="page"></param>
        public void NavigateToPage(string page)
        {
            IInputElement target = NavigationHelper.FindFrame("_top", Application.Current.MainWindow);
            NavigationCommands.GoToPage.Execute(page, target);
        }

        public MainViewModel ViewModel { get; set; }

        public Logger Logger = LogManager.GetCurrentClassLogger();

        private LoadingWindow _loadingWindow;
        public LoadingWindow LoadingWindow
        {
            get { return _loadingWindow; }
            set { _loadingWindow = value; }
        }

        public string SelectedGameType
        {
            get
            {
                var def = ConfigurationManager.AppSettings["SelectedGameType"];
                var returnVal = string.IsNullOrEmpty(Settings.Default.SelectedGameType)
                    ? def
                    : Settings.Default.SelectedGameType;
                return returnVal;
            }
            set
            {
                Settings.Default.SelectedGameType = value;
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("SelectedGameType"));
                this.OnPropertyChanged(new PropertyChangedEventArgs("ArmaGameType"));
            }
        }

        private GameType _armaGameType { get; set; }

        public GameType ArmaGameType
        {            
            get
            {
                var def = ConfigurationManager.AppSettings["SelectedGameType"];
                GameType defaultGameType;
                switch (def)
                {
                    case "Arma3":
                        defaultGameType = GameType.Arma3;
                        break;
                    case "Arma2":
                        defaultGameType = GameType.Arma2;
                        break;
                    case "DayZ":
                        defaultGameType = GameType.DayZ;
                        break;
                    case "Empyrion":
                        defaultGameType = GameType.Empyrion;
                        break;
                    case "Ark":
                        defaultGameType = GameType.Ark;
                        break;
                    default:
                        defaultGameType = GameType.Arma3;
                        break;
                }

                var selectedGameType = _armaGameType;
                if (!string.IsNullOrEmpty(Settings.Default.SelectedGameType))
                {
                    switch (Settings.Default.SelectedGameType)
                    {
                        case "Arma3":
                            selectedGameType = GameType.Arma3;
                            break;
                        case "Arma2":
                            selectedGameType = GameType.Arma2;
                            break;
                        case "DayZ":
                            selectedGameType = GameType.DayZ;
                            break;
                        case "Empyrion":
                            defaultGameType = GameType.Empyrion;
                            break;
                        case "Ark":
                            defaultGameType = GameType.Ark;
                            break;
                        default:
                            selectedGameType = GameType.Arma3;
                            break;
                    }
                }

                return (selectedGameType == GameType.None)
                    ? defaultGameType
                    : selectedGameType;
            }
            set
            {
                _armaGameType = value;

                switch (_armaGameType)
                {
                    case GameType.Arma3:
                        SelectedGameType = "Arma3";
                        break;
                    case GameType.Arma2:
                        SelectedGameType = "Arma2";
                        break;
                    case GameType.DayZ:
                        SelectedGameType = "DayZ";
                        break;
                    case GameType.Empyrion:
                        SelectedGameType = "Empyrion";
                        break;
                    case GameType.Ark:
                        SelectedGameType = "Ark";
                        break;
                    case GameType.None:
                        SelectedGameType = "None";
                        break;
                    default:
                        SelectedGameType = "Arma3";
                        break;  
                }
            }
        }

        public IEnumerable<GameType> GameTypeValues
        {
            get
            {
                return Enum.GetValues(typeof(GameType))
                    .Cast<GameType>();
            }
        }

        public const string ServersFilename = @"_{0}_servers.json";
        public const string ServersRecentFilename = @"_{0}_servers_recent.json";
        public const string ServersPasswordFilename = @"_{0}_servers_password.json";
        public const string ServersFavoriteFilename = @"_{0}_servers_favorite.json";
        public const string ServersNotesFilename = @"_{0}_servers_notes.json";
        public const string PlayersFilename = @"_{0}_players.json";

        //public const string Arma3ServersFilename = @"_Arma3_servers.json";
        //public const string Arma3ServersRecentFilename = @"_Arma3_servers_recent.json";
        //public const string Arma3ServersPasswordFilename = @"_Arma3_servers_password.json";
        //public const string Arma3ServersFavoriteFilename = @"_Arma3_servers_favorite.json";
        //public const string Arma3ServersNotesFilename = @"_Arma3_servers_notes.json";
        //public const string Arma3PlayersFilename = @"_Arma3_players.json";

        public void ConnectToServer(Server selectedserver)
        {
            if (selectedserver == null) return;

            var armaExe = string.Empty;
            var armaPath = string.Empty;
            var exeParams = string.Empty;
            var extraParams = string.Empty;
            var connectionParams = string.Empty;
            var modParms = string.Empty;

            if (selectedserver.Metadata.ArmaGameType == GameType.Arma2)
            {
                armaExe = this.Arma2OAExe;
            }

            if (selectedserver.Metadata.ArmaGameType == GameType.Arma3)
            {
                armaExe = this.Arma3Exe;
            }

            if (string.IsNullOrEmpty(armaExe))
            {
                var dialog = new DialogWindow(
                    DialogWindow.DialogType.ErrorYesNo,
                    "DOH!",
                    "The game executable is empty.  Go to Settings to set it?",
                    Application.Current.MainWindow);
                dialog.ShowDialog();

                if (dialog.Result)
                {
                    NavigateToPage("/AppSettings.xaml");
                }

                return;
            }

            var pathIsSet = true;

            switch (selectedserver.Metadata.ArmaGameType)
            {
                case GameType.Arma2:
                        if (string.IsNullOrEmpty(this.Arma2Path))
                            pathIsSet = false;
                    break;
                case GameType.Arma3:
                        if (string.IsNullOrEmpty(this.Arma3Path))
                            pathIsSet = false;
                    break;
            }

            if (!pathIsSet)
            {
                var dialog = new DialogWindow(
                    DialogWindow.DialogType.ErrorYesNo,
                    "DOH!",
                    "The game executable is empty.  Go to Settings to set it?",
                    Application.Current.MainWindow);
                dialog.ShowDialog();

                if (dialog.Result)
                {
                    NavigateToPage("/AppSettings.xaml");
                }
                
                return;
            }

            if (selectedserver.Metadata.ArmaGameType == GameType.Arma2)
                armaPath = Path.GetFullPath(this.Arma2OAPath.Trim());

            if (selectedserver.Metadata.ArmaGameType == GameType.Arma3)
                armaPath = Path.GetFullPath(this.Arma3Path.Trim());

            connectionParams = string.Format("-connect={0} -port={1}", selectedserver.Host, selectedserver.GamePort);

            if (selectedserver.Passworded == true && !string.IsNullOrEmpty(selectedserver.Metadata.ServerPassword))
                connectionParams += " -password=" + selectedserver.Metadata.ServerPassword;

            //build ExeParams and Extra params
            if (selectedserver.Metadata.ArmaGameType == GameType.Arma2)
            {
                exeParams = Arma2OAExeParams;
                extraParams = Arma2ExtraParams;
            }

            if (selectedserver.Metadata.ArmaGameType == GameType.Arma3)
            {
                exeParams = Arma3ExeParams;
                extraParams = Arma3ExtraParams;
            }

            //build launch params
            var modList = new List<string>();

            if (selectedserver.Metadata.ArmaGameType == GameType.Arma2)
                modList = this.Arma2ModParams.Split(new char[] { ';' }).ToList();

            if (selectedserver.Metadata.ArmaGameType == GameType.Arma3)
                modList = this.Arma3ModParams.Split(new char[] { ';' }).ToList();

            modParms = modList.Aggregate(modParms, (current, mod) => current + (mod + ";"));

            //TODO:areed - do I want to do this?
            //if (!string.IsNullOrEmpty(selectedserver.Mod))
            //{
            //    var supportedModList = new List<string>();

            //    if (selectedserver.Metadata.ArmaGameType == GameType.Arma2)
            //        supportedModList = this.Arma2SupportedModList.Split(new char[] { ';' }).ToList();

            //    if (selectedserver.Metadata.ArmaGameType == GameType.Arma3)
            //        supportedModList = this.AppNotes.Split(new char[] { ';' }).ToList();

            //    var targetList = selectedserver.Mod.Split(new char[] { ';' }).ToList();

            //    foreach (var target in targetList)
            //    {
            //        foreach (var supported in supportedModList)
            //        {
            //            if (target == supported)
            //                modParms += target + ";";
            //        }
            //    }
            //}

            try
            {
                var connectToServer = new Process();
                connectToServer.StartInfo.UseShellExecute = false;
                connectToServer.StartInfo.FileName = string.Format("{0}\\{1}", armaPath, armaExe);
                connectToServer.StartInfo.Arguments = string.Format(" {0} {1} {2} {3}",exeParams , modParms, connectionParams, extraParams);
                connectToServer.StartInfo.CreateNoWindow = true;

                if (!Current.DebugModeDoNotLaunchGame)
                    connectToServer.Start();

                selectedserver.Metadata.LastPlayedDate = DateTime.Now.ToShortDateString();
            }
            catch (Win32Exception e)
            {
                var dialog = new DialogWindow(
                    DialogWindow.DialogType.ErrorYesNo,
                    "DOH!",
                    "The game executable is empty.  Go to Settings to set it?",
                    Application.Current.MainWindow);
                dialog.ShowDialog();

                if (dialog.Result)
                {
                    NavigateToPage("/AppSettings.xaml");
                }
            }
        }

        public void DeletePreviousServersFile()
        {
            var file = string.Format(ServersFilename, ArmaGameType);

            //delete previous file
            File.Delete(file);
        }

        public void GetServersProcessSaveServersToDisk(ObservableCollection<Server> servers)
        {
            var file = string.Format(ServersFilename, ArmaGameType);

            var fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

            using (fs)
            {
                WriteServersStreamJson(servers, fs);
            }
        }

        public void SaveSingleServerToDisk(Server server)
        {
            if (ArmaServers == null)
                ArmaServers = new ObservableCollection<Server>();

            if (ArmaServers.Count == 0)
                GetServers();

            var dupeFound = false;

            foreach (var singleServer in ArmaServers)
            {
                if (server.Id == singleServer.Id)
                {
                    dupeFound = true;
                    break;
                }
            }

            if (!dupeFound)
            {
                //Add
                ArmaServers.Add(server);
            }
            else
            {
                //Edit
                var newList = new ObservableCollection<Server>();
                foreach (var armaServer in ArmaServers)
                {
                    newList.Add(armaServer.Id == server.Id ? server : armaServer);
                }

                ArmaServers = newList;
            }

            SaveArmaServersToDisk();
        }

        public void SaveArmaServersToDisk()
        {
            if (ArmaServers == null) return;

            FileStream fs;

            var file = string.Format(ServersFilename, ArmaGameType);

            //delete previous file
            File.Delete(file);

            using (fs = File.Open(file, FileMode.OpenOrCreate))
            {
                WriteServersStreamJson(ArmaServers, fs);
            }
        }

        public void SavePlayerToDisk(ClientPlayer player)
        {
            if (ArmaPlayers == null)
                ArmaPlayers = new ObservableCollection<ClientPlayer>();

            if (ArmaPlayers.Count == 0)
                GetPlayers();

            var dupeFound = false;

            foreach (var aPlayer in ArmaPlayers)
            {
                if (player.Player.Name == aPlayer.Player.Name)
                {
                    dupeFound = true;
                    break;
                }
            }

            //reset ShouldUpdate property - aka never save with ShouldUpdate=="true"
            player.ShouldUpdate = false;

            if (!dupeFound && !player.ShouldDelete)
            {
                //Add
                ArmaPlayers.Add(player);
            }
            else
            {
                //Edit
                var newList = new ObservableCollection<ClientPlayer>();
                foreach (var armaPlayer in ArmaPlayers)
                {
                    var targetPlayerUpdate = armaPlayer.Player.Name == player.Player.Name ? player : armaPlayer;

                    if(!targetPlayerUpdate.ShouldDelete)
                        newList.Add(targetPlayerUpdate);
                }

                ArmaPlayers = newList;
            }

            SaveArmaPlayersToDisk();
        }

        public void SaveArmaPlayersToDisk()
        {
            if (ArmaPlayers == null) return;

            FileStream fs;

            var file = string.Format(PlayersFilename, ArmaGameType);

            //delete previous file
            File.Delete(file);

            using (fs = File.Open(file, FileMode.OpenOrCreate))
            {
                WritePlayersStreamJson(ArmaPlayers, fs);
            }
        }

        public void SaveRecentServerToDisk(Server server)
        {
            if(ArmaRecentServers == null)
                ArmaRecentServers = new ObservableCollection<Server>();

            var newServerListToSave = new ObservableCollection<Server>();

            if (ArmaRecentServers.Count == 0)
                GetRecentServers();

            var dupeFound = false;

            foreach (var RecentServer in ArmaRecentServers)
            {
                if (server.Id == RecentServer.Id)
                {
                    dupeFound = true;
                    break;
                }
            }

            if (ArmaRecentServers != null)
            {
                if (dupeFound && ArmaRecentServers.Count > 0)
                {
                    foreach (var recentServer in ArmaRecentServers)
                    {
                        if (server.Id == recentServer.Id && server.Metadata.LastPlayedDate != null && !server.Metadata.ShouldDelete)
                        {
                            newServerListToSave.Add(server);
                        }
                        else if (server.Id == recentServer.Id && server.Metadata.LastPlayedDate == null)
                        {
                            // do nothing / do not add
                        }
                        else
                        {
                            // add current servers back on list to save
                            if (!recentServer.Metadata.ShouldDelete)
                                newServerListToSave.Add(recentServer);
                        }
                    }
                }
                else
                {
                    //add previous Recents back on
                    newServerListToSave = ArmaRecentServers;

                    if ((server.Metadata.LastPlayedDate != null && server.Metadata.LastPlayedDate != "never") && !server.Metadata.ShouldDelete)
                        newServerListToSave.Add(server);
                }
            }

            ArmaRecentServers = newServerListToSave;

            SaveArmaRecentServersToDisk();
        }

        public void SaveArmaRecentServersToDisk()
        {
            if (ArmaRecentServers == null) return;

            FileStream fs;

            var file = string.Format(ServersRecentFilename, ArmaGameType);

            //delete previous file
            File.Delete(file);

            using (fs = File.Open(file, FileMode.OpenOrCreate))
            {
                WriteServersStreamJson(ArmaRecentServers, fs);
            }
        }

        public void SavePasswordServerToDisk(Server server)
        {
            if (ArmaPasswordServers == null)
                ArmaPasswordServers = new ObservableCollection<Server>();

            var newServerListToSave = new ObservableCollection<Server>();

            if (ArmaPasswordServers.Count == 0)
                GetPasswordServers();

            var dupeFound = false;

            foreach (var passwordServer in ArmaPasswordServers)
            {
                if (server.Id == passwordServer.Id)
                {
                    dupeFound = true;
                    break;
                }
            }

            if (ArmaPasswordServers != null)
            {
                if (dupeFound && ArmaPasswordServers.Count > 0)
                {
                    foreach (var passwordServer in ArmaPasswordServers)
                    {
                        if (server.Id == passwordServer.Id && server.Metadata.EnterPasswordOnConnect)
                        {
                            newServerListToSave.Add(server);
                        }
                        else if (server.Id == passwordServer.Id && server.Metadata.EnterPasswordOnConnect == false)
                        {
                            // do nothing / do not add
                        }
                        else
                        {
                            // add current servers back on list to save
                            newServerListToSave.Add(passwordServer);
                        }
                    }
                }
                else
                {
                    //add previous passwords back on
                    newServerListToSave = ArmaPasswordServers;

                    if (server.Metadata.EnterPasswordOnConnect)
                        newServerListToSave.Add(server);
                }
            }

            ArmaPasswordServers = newServerListToSave;

            SaveArmaPasswordServersToDisk();
        }

        public void SaveArmaPasswordServersToDisk()
        {
            if (ArmaPasswordServers == null) return;

            FileStream fs;

            var file = string.Format(ServersPasswordFilename, ArmaGameType);

            //delete previous file
            File.Delete(file);

            using (fs = File.Open(file, FileMode.OpenOrCreate))
            {
                WriteServersStreamJson(ArmaPasswordServers, fs);
            }
        }

        public void SaveFavoriteServerToDisk(Server server)
        {
            if(ArmaFavoriteServers == null)
                ArmaFavoriteServers = new ObservableCollection<Server>();

            var newServerListToSave = new ObservableCollection<Server>();

            if (ArmaFavoriteServers.Count == 0)
                GetFavoriteServers();

            var dupeFound = false;

            foreach (var favoriteServer in ArmaFavoriteServers)
            {
                if (server.Id == favoriteServer.Id)
                {
                    dupeFound = true;
                    break;
                }
            }

            if (ArmaFavoriteServers != null)
            {
                if (dupeFound && ArmaFavoriteServers.Count > 0)
                {
                    foreach (var favoriteServer in ArmaFavoriteServers)
                    {
                        if (server.Id == favoriteServer.Id && server.Metadata.IsFavorite)
                        {
                            newServerListToSave.Add(server);
                        }
                        else if (server.Id == favoriteServer.Id && server.Metadata.IsFavorite == false)
                        {
                            // do nothing / do not add
                        }
                        else
                        {
                            // add current servers back on list to save
                            newServerListToSave.Add(favoriteServer);
                        }
                    }
                }
                else
                {
                    //add previous favorites back on
                    newServerListToSave = ArmaFavoriteServers;

                    if (server.Metadata.IsFavorite)
                        newServerListToSave.Add(server);
                }
            }

            ArmaFavoriteServers = newServerListToSave;

            SaveArmaFavoriteServersToDisk();
        }

        public void SaveArmaFavoriteServersToDisk()
        {
            if (ArmaFavoriteServers == null) return;

            FileStream fs;

            var file = string.Format(ServersFavoriteFilename, ArmaGameType);

            //delete previous file
            File.Delete(file);

            using (fs = File.Open(file, FileMode.OpenOrCreate))
            {
                WriteServersStreamJson(ArmaFavoriteServers, fs);
            }
        }

        public void SaveNotesServerToDisk(Server server)
        {
            if (ArmaNotesServers == null)
                ArmaNotesServers = new ObservableCollection<Server>();

            var newServerListToSave = new ObservableCollection<Server>();

            if (ArmaNotesServers.Count == 0)
                GetNotesServers();

            var dupeFound = false;

            foreach (var notesServer in ArmaNotesServers)
            {
                if (server.Id == notesServer.Id)
                {
                    dupeFound = true;
                    break;
                }
            }

            if (ArmaNotesServers != null)
            {
                if (dupeFound && ArmaNotesServers.Count > 0)
                {
                    foreach (var notesServer in ArmaNotesServers)
                    {
                        if (server.Id == notesServer.Id && server.Metadata.HasNotes)
                        {
                            newServerListToSave.Add(server);
                        }
                        else if (server.Id == notesServer.Id && server.Metadata.HasNotes == false)
                        {
                            // do nothing / do not add
                        }
                        else
                        {
                            // add current servers back on list to save
                            newServerListToSave.Add(notesServer);
                        }
                    }
                }
                else
                {
                    //add previous notes servers back on
                    newServerListToSave = ArmaNotesServers;

                    if (server.Metadata.HasNotes)
                        newServerListToSave.Add(server);
                }
            }

            ArmaNotesServers = newServerListToSave;

            SaveArmaNotesServersToDisk();
        }

        public void SaveArmaNotesServersToDisk()
        {
            if (ArmaNotesServers == null) return;

            FileStream fs;

            var file = string.Format(ServersNotesFilename, ArmaGameType);

            //delete previous file
            File.Delete(file);

            using (fs = File.Open(file, FileMode.OpenOrCreate))
            {
                WriteServersStreamJson(ArmaNotesServers, fs);
            }
        }

        public List<string> GetArma2ModList()
        {
            var allDirectories = Directory.GetDirectories(string.Format("{0}", Arma2OAPath));

            return allDirectories.Where(directory => directory.Contains("@")).ToList();
        }
        public List<string> GetArma3ModList()
        {
            var allDirectories = Directory.GetDirectories(string.Format("{0}", Arma3Path));

            return allDirectories.Where(directory => directory.Contains("@")).ToList();
        }

        public void GetServers()
        {
            var file = string.Format(ServersFilename, ArmaGameType);

            if (File.Exists(file))
            {
                ArmaServers = ReadServersStreamJson(file);
            }

            if (ArmaServers == null)
                ArmaServers = new ObservableCollection<Server>();
        }

        public void GetRecentServers()
        {
            var file = string.Format(ServersRecentFilename, ArmaGameType);

            if (File.Exists(file))
            {
                ArmaRecentServers = ReadServersStreamJson(file);
            }

            if (ArmaRecentServers == null)
                ArmaRecentServers = new ObservableCollection<Server>();

            // backfill ArmaServers
            foreach (var server in ArmaRecentServers)
            {
                if (server != null)
                {
                    var serverToUpdate = Globals.Current.ArmaServers.FirstOrDefault(i => i.Id == server.Id);
                    if (serverToUpdate != null)
                    {
                        serverToUpdate.Metadata.LastPlayedDate = server.Metadata.LastPlayedDate;
                    }
                }
            }
        }

        public void GetPasswordServers()
        {
            var file = string.Format(ServersPasswordFilename, ArmaGameType);

            if (File.Exists(file))
            {
                ArmaPasswordServers = ReadServersStreamJson(file);
            }

            if (ArmaPasswordServers == null)
                ArmaPasswordServers = new ObservableCollection<Server>();

            // backfill ArmaServers
            foreach (var server in ArmaPasswordServers)
            {
                if (server != null)
                {
                    var serverToUpdate = Globals.Current.ArmaServers.FirstOrDefault(i => i.Id == server.Id);
                    if (serverToUpdate != null)
                    {
                        serverToUpdate.Metadata.ServerPassword = server.Metadata.ServerPassword;
                    }
                }
            }
        }

        public void GetFavoriteServers()
        {
            var file = string.Format(ServersFavoriteFilename, ArmaGameType);

            if (File.Exists(file))
            {
                ArmaFavoriteServers = ReadServersStreamJson(file);
            }

            if (ArmaFavoriteServers == null)
                ArmaFavoriteServers = new ObservableCollection<Server>();

            // backfill ArmaServers
            foreach (var server in ArmaFavoriteServers)
            {
                if (server != null)
                {
                    var serverToUpdate = Globals.Current.ArmaServers.FirstOrDefault(i => i.Id == server.Id);
                    if (serverToUpdate != null)
                    {
                        serverToUpdate.Metadata.IsFavorite = server.Metadata.IsFavorite;
                    }
                }
            }
        }

        public void GetNotesServers()
        {
            var file = string.Format(ServersNotesFilename, ArmaGameType);

            if (File.Exists(file))
            {
                ArmaNotesServers = ReadServersStreamJson(file);
            }

            if (ArmaNotesServers == null)
                ArmaNotesServers = new ObservableCollection<Server>();

            // backfill ArmaServers
            foreach (var server in ArmaNotesServers)
            {
                if (server != null)
                {
                    var serverToUpdate = Globals.Current.ArmaServers.FirstOrDefault(i => i.Id == server.Id);
                    if (serverToUpdate != null)
                    {
                        serverToUpdate.Metadata.HasNotes = server.Metadata.HasNotes;
                        serverToUpdate.Metadata.Notes = server.Metadata.Notes;
                    }
                }
            }
        }

        public void GetPlayers()
        {
            var file = string.Format(PlayersFilename, ArmaGameType);

            if (File.Exists(file))
            {
                ArmaPlayers = ReadPlayersStreamJson(file);
            }

            if (ArmaPlayers == null)
                ArmaPlayers = new ObservableCollection<ClientPlayer>();
        }

        private static ObservableCollection<Server> ReadServersStreamJson(string fs)
        {
            var servers = new ObservableCollection<Server>();
            using (var r = new StreamReader(fs))
            {
                string json = r.ReadToEnd();
                servers = JsonConvert.DeserializeObject<ObservableCollection<Server>>(json);
            }
            return servers;
        }

        private static ObservableCollection<ClientPlayer> ReadPlayersStreamJson(string fs)
        {
            var servers = new ObservableCollection<ClientPlayer>();
            using (var r = new StreamReader(fs))
            {
                string json = r.ReadToEnd();
                servers = JsonConvert.DeserializeObject<ObservableCollection<ClientPlayer>>(json);
            }
            return servers;
        }

        private static void WriteServersStreamJson(ObservableCollection<Server> servers, FileStream fs)
        {

            App.Current.Dispatcher.Invoke((Action) delegate // <--- HERE
            {
                using (var sw = new StreamWriter(fs))
                using (JsonWriter jw = new JsonTextWriter(sw))
                {
                    jw.Formatting = (Newtonsoft.Json.Formatting) Formatting.Indented;
                    var serializer = new JsonSerializer();
                    var result = serializer.Serialize(servers.ToList());
                    jw.WriteRaw(result);
                }
            });
        }

        private static void WritePlayersStreamJson(ObservableCollection<ClientPlayer> players, FileStream fs)
        {

            App.Current.Dispatcher.Invoke((Action) delegate // <--- HERE
            {
                using (var sw = new StreamWriter(fs))
                using (JsonWriter jw = new JsonTextWriter(sw))
                {
                    jw.Formatting = (Newtonsoft.Json.Formatting) Formatting.Indented;
                    var serializer = new JsonSerializer();
                    var result = serializer.Serialize(players.ToList());
                    jw.WriteRaw(result);
                }
            });
        }

        private ObservableCollection<Server> _armaServers;
        public ObservableCollection<Server> ArmaServers
        {
            get { return _armaServers; }
            set
            {
                _armaServers = value;
            }
        }

        private ObservableCollection<Server> _armaRecentServers;
        public ObservableCollection<Server> ArmaRecentServers
        {
            get { return _armaRecentServers; }
            set
            {
                _armaRecentServers = value;
            }
        }

        private ObservableCollection<Server> _armaPasswordServers;
        public ObservableCollection<Server> ArmaPasswordServers
        {
            get { return _armaPasswordServers; }
            set
            {
                _armaPasswordServers = value;
            }
        }

        private ObservableCollection<Server> _armaFavoriteServers;
        public ObservableCollection<Server> ArmaFavoriteServers
        {
            get { return _armaFavoriteServers; }
            set
            {
                _armaFavoriteServers = value;
            }
        }

        private ObservableCollection<Server> _armaNotesServers;
        public ObservableCollection<Server> ArmaNotesServers
        {
            get { return _armaNotesServers; }
            set
            {
                _armaNotesServers = value;
                OnPropertyChanged("ArmaNotesServers");
                OnPropertyChanged("ServerNotesList");
            }
        }

        private ObservableCollection<ClientPlayer> _playersList;
        public ObservableCollection<ClientPlayer> PlayersList
        {
            get { return _playersList; }
            set
            {
                _playersList = value;
                OnPropertyChanged("PlayersList");
            }
        }

        private ObservableCollection<ClientPlayer> _armaPlayers;
        public ObservableCollection<ClientPlayer> ArmaPlayers
        {
            get { return _armaPlayers; }
            set
            {
                _armaPlayers = value;
                OnPropertyChanged("ArmaPlayers");
                OnPropertyChanged("PlayersList");
            }
        }

        public double AppWidth
        {
            get
            {
                var def = Convert.ToDouble(ConfigurationManager.AppSettings["AppWidth"]);
                return string.IsNullOrEmpty(Settings.Default.AppWidth) ? def : Convert.ToDouble(Settings.Default.AppWidth);
            }
            set
            {
                Settings.Default.AppWidth = Convert.ToString(value);
                Settings.Default.Save();
                OnPropertyChanged("AppWidth");
            }
        }

        public double AppHeight
        {
            get
            {
                var def = Convert.ToDouble(ConfigurationManager.AppSettings["AppHeight"]);
                return string.IsNullOrEmpty(Settings.Default.AppHeight) ? def : Convert.ToDouble(Settings.Default.AppHeight);
            }
            set
            {
                Settings.Default.AppHeight = Convert.ToString(value);
                Settings.Default.Save();
                OnPropertyChanged("AppHeight");
            }
        }

        public double AppTop
        {
            get
            {
                var def = Convert.ToDouble(ConfigurationManager.AppSettings["AppTop"]);
                return string.IsNullOrEmpty(Settings.Default.AppTop) ? def : Convert.ToDouble(Settings.Default.AppTop);
            }
            set
            {
                Settings.Default.AppTop = Convert.ToString(value);
                Settings.Default.Save();
                OnPropertyChanged("AppTop");
            }
        }

        public double AppLeft
        {
            get
            {
                var def = Convert.ToDouble(ConfigurationManager.AppSettings["AppLeft"]);
                return string.IsNullOrEmpty(Settings.Default.AppLeft) ? def : Convert.ToDouble(Settings.Default.AppLeft);
            }
            set
            {
                Settings.Default.AppLeft = Convert.ToString(value);
                Settings.Default.Save();
                OnPropertyChanged("AppLeft");
            }
        }

        public bool AppStateMaximized
        {
            get
            {
                var def = Convert.ToBoolean(ConfigurationManager.AppSettings["AppStateMaximized"]);
                return string.IsNullOrEmpty(Settings.Default.AppStateMaximized) ? def : Convert.ToBoolean(Settings.Default.AppStateMaximized);
            }
            set
            {
                Settings.Default.AppStateMaximized = Convert.ToString(value);
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("AppStateMaximized"));
            }
        }

        public static void Logout()
        {
            //TODO dispose objects
        }

        public List<AppSettings> AppSettingsWindow
        {
            get { return _addAppWindow; }
        }
        private readonly List<AppSettings> _addAppWindow = new List<AppSettings>();

        public bool CloseLauncherApp
        {
            get
            {
                var def = Convert.ToBoolean(ConfigurationManager.AppSettings["CloseLauncherApp"]);
                return string.IsNullOrEmpty(Settings.Default.CloseLauncherApp) ? def : Convert.ToBoolean(Settings.Default.CloseLauncherApp);
            }
            set
            {
                Settings.Default.CloseLauncherApp = Convert.ToString(value);
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("CloseLauncherApp"));
            }
        }

        public string Arma2LaunchParams
        {
            get
            {
                var def = ConfigurationManager.AppSettings["Arma2LaunchParams"];
                return string.IsNullOrEmpty(Settings.Default.Arma2LaunchParams) ? def : Settings.Default.Arma2LaunchParams;
            }
            set
            {
                Settings.Default.Arma2LaunchParams = value;
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("Arma2LaunchParams"));
            }
        }

        public string Arma2Path
        {
            get
            {
                var def = ConfigurationManager.AppSettings["Arma2Path"];
                return string.IsNullOrEmpty(Settings.Default.Arma2Path) ? def : Settings.Default.Arma2Path;
            }
            set
            {
                Settings.Default.Arma2Path = value;
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("Arma2Path"));
            }
        }

        public bool WindowedMode
        {
            get
            {
                var def = Convert.ToBoolean(ConfigurationManager.AppSettings["WindowedMode"]);
                return string.IsNullOrEmpty(Settings.Default.WindowedMode) ? def : Convert.ToBoolean(Settings.Default.WindowedMode);
            }
            set
            {
                Settings.Default.WindowedMode = Convert.ToString(value);
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("WindowedMode"));
            }
        }

        public bool GlobalAutoMatchClientMod
        {
            get
            {
                var def = Convert.ToBoolean(ConfigurationManager.AppSettings["GlobalAutoMatchClientMod"]);
                return string.IsNullOrEmpty(Settings.Default.GlobalAutoMatchClientMod) ? def : Convert.ToBoolean(Settings.Default.GlobalAutoMatchClientMod);
            }
            set
            {
                Settings.Default.WindowedMode = Convert.ToString(value);
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("GlobalAutoMatchClientMod"));
            }
        }

        public string Arma2OAPath
        {
            get
            {
                var def = ConfigurationManager.AppSettings["Arma2OAPath"];
                return string.IsNullOrEmpty(Settings.Default.Arma2OAPath) ? def : Settings.Default.Arma2OAPath;
            }
            set
            {
                Settings.Default.Arma2OAPath = value;
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("Arma2OAPath"));
            }
        }

        public string Arma3Path
        {
            get
            {
                var def = ConfigurationManager.AppSettings["Arma3Path"];
                return string.IsNullOrEmpty(Settings.Default.Arma3Path) ? def : Settings.Default.Arma3Path;
            }
            set
            {
                Settings.Default.Arma3Path = value;
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("Arma3Path"));
            }
        }

        public bool DebugModeDoNotLaunchGame
        {
            get
            {
                var def = Convert.ToBoolean(ConfigurationManager.AppSettings["DebugModeDoNotLaunchGame"]);
                return string.IsNullOrEmpty(Settings.Default.DebugModeDoNotLaunchGame) ? def : Convert.ToBoolean(Settings.Default.DebugModeDoNotLaunchGame);
            }
            set
            {
                Settings.Default.DebugModeDoNotLaunchGame = Convert.ToString(value);
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("DebugModeDoNotLaunchGame"));
            }
        }

        public bool DebugModeTestLocalOnly
        {
            get
            {
                var def = Convert.ToBoolean(ConfigurationManager.AppSettings["DebugModeTestLocalOnly"]);
                return string.IsNullOrEmpty(Settings.Default.DebugModeTestLocalOnly) ? def : Convert.ToBoolean(Settings.Default.DebugModeTestLocalOnly);
            }
            set
            {
                Settings.Default.DebugModeTestLocalOnly = Convert.ToString(value);
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("DebugModeTestLocalOnly"));
            }
        }

        public bool RefreshExtended
        {
            get
            {
                var def = Convert.ToBoolean(ConfigurationManager.AppSettings["RefreshExtended"]);
                return string.IsNullOrEmpty(Settings.Default.RefreshExtended) ? def : Convert.ToBoolean(Settings.Default.RefreshExtended);
            }
            set
            {
                Settings.Default.RefreshExtended = Convert.ToString(value);
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("RefreshExtended"));
            }
        }

        public string Arma3LaunchParams
        {
            get
            {
                var def = ConfigurationManager.AppSettings["Arma3LaunchParams"];
                return string.IsNullOrEmpty(Settings.Default.Arma3LaunchParams) ? def : Settings.Default.Arma3LaunchParams;
            }
            set
            {
                Settings.Default.Arma3LaunchParams = value;
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("Arma3LaunchParams"));
            }
        }

        public bool IncludeArma2PathInArma3LaunchParams
        {
            get
            {
                var def = Convert.ToBoolean(ConfigurationManager.AppSettings["IncludeArma2PathInArma3LaunchParams"]);
                return string.IsNullOrEmpty(Settings.Default.IncludeArma2PathInArma3LaunchParams) ? def : Convert.ToBoolean(Settings.Default.IncludeArma2PathInArma3LaunchParams);
            }
            set
            {
                Settings.Default.IncludeArma2PathInArma3LaunchParams = Convert.ToString(value);
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("IncludeArma2PathInArma3LaunchParams"));
            }
        }

        public bool IncludeArma2OAPathInArma3LaunchParams
        {
            get
            {
                var def = Convert.ToBoolean(ConfigurationManager.AppSettings["IncludeArma2OAPathInArma3LaunchParams"]);
                return string.IsNullOrEmpty(Settings.Default.IncludeArma2OAPathInArma3LaunchParams) ? def : Convert.ToBoolean(Settings.Default.IncludeArma2OAPathInArma3LaunchParams);
            }
            set
            {
                Settings.Default.IncludeArma2OAPathInArma3LaunchParams = Convert.ToString(value);
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("IncludeArma2OAPathInArma3LaunchParams"));
            }
        }

        public bool IncludeAdditionalArmaPaths1InArma3LaunchParams
        {
            get
            {
                var def = Convert.ToBoolean(ConfigurationManager.AppSettings["IncludeAdditionalArmaPaths1InArma3LaunchParams"]);
                return string.IsNullOrEmpty(Settings.Default.IncludeAdditionalArmaPaths1InArma3LaunchParams) ? def : Convert.ToBoolean(Settings.Default.IncludeAdditionalArmaPaths1InArma3LaunchParams); 
            }
            set
            {
                Settings.Default.IncludeAdditionalArmaPaths1InArma3LaunchParams = Convert.ToString(value);
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("IncludeAdditionalArmaPaths1InArma3LaunchParams"));
            }
        }

        public string AdditionalArmaPaths1
        {
            get
            {
                var def = ConfigurationManager.AppSettings["AdditionalArmaPaths1"];
                return string.IsNullOrEmpty(Settings.Default.AdditionalArmaPaths1) ? def : Settings.Default.AdditionalArmaPaths1;
            }
            set
            {
                Settings.Default.AdditionalArmaPaths1 = value;
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("AdditionalArmaPaths1"));
            }
        }

        public bool IncludeAddtionalArmaPaths1InArma2LaunchParams
        {
            get
            {
                var def = Convert.ToBoolean(ConfigurationManager.AppSettings["IncludeAddtionalArmaPaths1InArma2LaunchParams"]);
                return string.IsNullOrEmpty(Settings.Default.IncludeAddtionalArmaPaths1InArma2LaunchParams) ? def : Convert.ToBoolean(Settings.Default.IncludeAddtionalArmaPaths1InArma2LaunchParams);
            }
            set
            {
                Settings.Default.IncludeAddtionalArmaPaths1InArma2LaunchParams = Convert.ToString(value);
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("IncludeAddtionalArmaPaths1InArma2LaunchParams"));
            }
        }

        public string AdditionalArmaPaths2
        {
            get
            {
                var def = ConfigurationManager.AppSettings["AdditionalArmaPaths2"];
                return string.IsNullOrEmpty(Settings.Default.AdditionalArmaPaths2) ? def : Settings.Default.AdditionalArmaPaths2;
            }
            set
            {
                Settings.Default.AdditionalArmaPaths2 = value;
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("AdditionalArmaPaths2"));
            }
        }

        public bool IncludeAdditionalArmaPaths2InArma2LaunchParams
        {
            get
            {
                var def = Convert.ToBoolean(ConfigurationManager.AppSettings["IncludeAdditionalArmaPaths2InArma2LaunchParams"]);
                return string.IsNullOrEmpty(Settings.Default.IncludeAdditionalArmaPaths2InArma2LaunchParams) ? def : Convert.ToBoolean(Settings.Default.IncludeAdditionalArmaPaths2InArma2LaunchParams);
            }
            set
            {
                Settings.Default.IncludeAdditionalArmaPaths2InArma2LaunchParams = Convert.ToString(value);
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("IncludeAdditionalArmaPaths2InArma2LaunchParams"));
            }
        }

        public bool IncludeAdditionalArmaPaths2InArma3LaunchParams
        {
            get
            {
                var def = Convert.ToBoolean(ConfigurationManager.AppSettings["IncludeAdditionalArmaPaths2InArma3LaunchParams"]);
                return string.IsNullOrEmpty(Settings.Default.IncludeAdditionalArmaPaths2InArma3LaunchParams) ? def : Convert.ToBoolean(Settings.Default.IncludeAdditionalArmaPaths2InArma3LaunchParams);
            }
            set
            {
                Settings.Default.IncludeAdditionalArmaPaths2InArma3LaunchParams = Convert.ToString(value);
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("IncludeAdditionalArmaPaths2InArma3LaunchParams"));
            }
        }

        public string Arma2OAExe
        {
            get
            {
                var def = ConfigurationManager.AppSettings["Arma2OAExe"];
                return string.IsNullOrEmpty(Settings.Default.Arma2OAExe) ? def : Settings.Default.Arma2OAExe;
            }
            set
            {
                Settings.Default.Arma2OAExe = value;
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("Arma2OAExe"));
            }
        }

        public string Arma2OAExeParams
        {
            get
            {
                var def = ConfigurationManager.AppSettings["Arma2OAExeParams"];
                return string.IsNullOrEmpty(Settings.Default.Arma2OAExeParams) ? def : Settings.Default.Arma2OAExeParams;
            }
            set
            {
                Settings.Default.Arma2OAExeParams = value;
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("Arma2OAExeParams"));
            }
        }

        public string Arma2ModParams
        {
            get
            {
                var def = ConfigurationManager.AppSettings["Arma2ModParams"];
                return string.IsNullOrEmpty(Settings.Default.Arma2ModParams) ? def : Settings.Default.Arma2ModParams;
            }
            set
            {
                Settings.Default.Arma2ModParams = value;
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("Arma2ModParams"));
            }
        }

        public string Arma2ExtraParams
        {
            get
            {
                var def = ConfigurationManager.AppSettings["Arma2ExtraParams"];
                return string.IsNullOrEmpty(Settings.Default.Arma2ExtraParams) ? def : Settings.Default.Arma2ExtraParams;
            }
            set
            {
                Settings.Default.Arma2ExtraParams = value;
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("Arma2ExtraParams"));
            }
        }

        public string Arma3Exe
        {
            get
            {
                var def = ConfigurationManager.AppSettings["Arma3Exe"];
                return string.IsNullOrEmpty(Settings.Default.Arma3Exe) ? def : Settings.Default.Arma3Exe;
            }
            set
            {
                Settings.Default.Arma3Exe = value;
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("Arma3Exe"));
            }
        }

        public string Arma3ExeParams
        {
            get
            {
                var def = ConfigurationManager.AppSettings["Arma3ExeParams"];
                return string.IsNullOrEmpty(Settings.Default.Arma3ExeParams) ? def : Settings.Default.Arma3ExeParams;
            }
            set
            {
                Settings.Default.Arma3ExeParams = value;
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("Arma3ExeParams"));
            }
        }

        public string Arma3ModParams
        {
            get
            {
                var def = ConfigurationManager.AppSettings["Arma3ModParams"];
                return string.IsNullOrEmpty(Settings.Default.Arma3ModParams) ? def : Settings.Default.Arma3ModParams;
            }
            set
            {
                Settings.Default.Arma3ModParams = value;
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("Arma3ModParams"));
            }
        }

        public string Arma3ExtraParams
        {
            get
            {
                var def = ConfigurationManager.AppSettings["Arma3ExtraParams"];
                return string.IsNullOrEmpty(Settings.Default.Arma3ExtraParams) ? def : Settings.Default.Arma3ExtraParams;
            }
            set
            {
                Settings.Default.Arma3ExtraParams = value;
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("Arma3ExtraParams"));
            }
        }

        public BitmapImage ServerRefreshPingImage
        {
            get
            {
                var imageSource = new BitmapImage();
                imageSource.BeginInit();
                imageSource.UriSource = new Uri(@"Images/refresh_64.png", UriKind.RelativeOrAbsolute);
                imageSource.CacheOption = BitmapCacheOption.OnLoad;
                imageSource.EndInit();
                return imageSource;
            }
        }

        public string Arma2SupportedModList
        {
            get
            {
                var def = ConfigurationManager.AppSettings["Arma2SupportedModList"];
                return string.IsNullOrEmpty(Settings.Default.Arma2SupportedModList) ? def : Settings.Default.Arma2SupportedModList;
            }
            set
            {
                Settings.Default.Arma2SupportedModList = value;
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("Arma2SupportedModList"));
            }
        }

        public string AppNotes
        {
            get
            {
                var def = ConfigurationManager.AppSettings["AppNotes"];
                return string.IsNullOrEmpty(Settings.Default.AppNotes) ? def : Settings.Default.AppNotes;
            }
            set
            {
                Settings.Default.AppNotes = value;
                Settings.Default.Save();
                this.OnPropertyChanged(new PropertyChangedEventArgs("AppNotes"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, e);
            }
        }
        protected void OnPropertyChanged(string propertyName)
        {
            var eventToFire = PropertyChanged;
            if (eventToFire == null)
                return;

            eventToFire(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
