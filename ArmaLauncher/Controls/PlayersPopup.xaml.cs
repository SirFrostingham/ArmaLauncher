using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
using ArmaLauncher.Models;
using ArmaLauncher.ViewModel;
using Newtonsoft.Json;

namespace ArmaLauncher.Controls
{
    /// <summary>
    /// Interaction logic for PasswordPopup.xaml
    /// </summary>
    public partial class PlayersPopup : Popup, INotifyPropertyChanged
    {

        private FrameworkElement _parentFrameworkElement;

        public FrameworkElement ParentFrameworkElement
        {
            get
            {
                return _parentFrameworkElement;
            }
            set
            {
                if (Equals(_parentFrameworkElement, value)) return;
                _parentFrameworkElement = value;
                OnPropertyChanged("ParentFrameworkElement");
            }
        }

        private string _pageType;

        public string PageType
        {
            get
            {
                return _pageType;
            }
            set
            {
                if (Equals(_pageType, value)) return;
                _pageType = value;
                OnPropertyChanged("PageType");
            }
        }

        private ClientPlayer _dataGridSelectedItem;
        public ClientPlayer DataGridSelectedItem
        {
            get { return _dataGridSelectedItem; }
            set
            {
                _dataGridSelectedItem = value;
                dgPlayers.UpdateLayout();
            }
        }

        private ObservableCollection<ClientPlayer> _players;
        public ObservableCollection<ClientPlayer> Players
        {
            get
            {
                return _players;
            }
            set
            {
                if (_players == value) return;
                _players = value;
                OnPropertyChanged("Players");
            }
        }

        private Server _server;
        public Server Server
        {
            get
            {
                return _server;
            }
            set
            {
                if (_server == value) return;
                _server = value;
                OnPropertyChanged("Server");
            }
        }

        private ObservableCollection<ClientPlayer> _playersWithoutChanges;
        public ObservableCollection<ClientPlayer> PlayersWithoutChanges
        {
            get
            {
                return _playersWithoutChanges;
            }
            set
            {
                if (_playersWithoutChanges == value) return;
                _playersWithoutChanges = value;
                OnPropertyChanged("PlayersWithoutChanges");
            }
        }

        public PlayersPopup(Server serverItem, FrameworkElement parentFrameworkElement, string pageType)
        {
            InitializeComponent();
            this.DataContext = this;
            PageType = pageType;
            ParentFrameworkElement = parentFrameworkElement;
            Server = serverItem;

            //reset grid
            dgPlayers.AutoGenerateColumns = true;
            dgPlayers.AutoGenerateColumns = false;
            dgPlayers.Items.Refresh();

            // Move window start
            var thumb = new Thumb
            {
                Width = 0,
                Height = 0,
            };
            ContentCanvas.Children.Add(thumb);

            MouseDown += (sender, e) =>
            {
                thumb.RaiseEvent(e);
            };

            thumb.DragDelta += (sender, e) =>
            {
                popupPlayers.HorizontalOffset += e.HorizontalChange;
                popupPlayers.VerticalOffset += e.VerticalChange;
            };
            // Move window end

            var errorWhileLoadingPlyers = false;

            // convert server player list to client player list
            var players = new ObservableCollection<ClientPlayer>();
            foreach (var player in serverItem.Game.Players)
            {
                try
                {
                    var type = player.GetType();
                    if (type.Name == "JObject")
                    {
                        // jSON:  deserialize if this object was cached
                        var serverPlayer = new Player();
                        string json = player.ToString();
                        serverPlayer = JsonConvert.DeserializeObject<Player>(json);
                        var clientPlayer = new ClientPlayer();
                        clientPlayer.Player = serverPlayer;

                        //check player list for player 1st
                        var gPlayer =
                            Globals.Current.ArmaPlayers.FirstOrDefault(i => i.Player.Name == clientPlayer.Player.Name);
                        if (gPlayer != null)
                        {
                            clientPlayer = gPlayer;
                        }

                        players.Add(clientPlayer);
                    }
                    else
                    {
                        // serialize to follow the same process as above
                        string json = JsonConvert.SerializeObject(player);

                        // jSON:  deserialize if this object was cached
                        var serverPlayer = new Player();
                        serverPlayer = JsonConvert.DeserializeObject<Player>(json);
                        var clientPlayer = new ClientPlayer();
                        clientPlayer.Player = serverPlayer;

                        //check player list for player 1st
                        var gPlayer =
                            Globals.Current.ArmaPlayers.FirstOrDefault(i => i.Player.Name == clientPlayer.Player.Name);
                        if (gPlayer != null)
                        {
                            clientPlayer = gPlayer;
                        }

                        players.Add(clientPlayer);
                    }
                }
                catch (Exception)
                {
                    //do not add weird player name to grid...
                    errorWhileLoadingPlyers = true;
                }
            }

            if (errorWhileLoadingPlyers)
            {
                var dialog = new DialogWindow(
                    DialogWindow.DialogType.ErrorYesNo,
                    "DOH!",
                    "Click 'Yes' to try to load player list,\r\n or click 'No' to return to the main grid \r\nand click the green refresh button first...",
                    this);
                dialog.ShowDialog();

                if (!dialog.Result)
                {
                    popupPlayers.IsOpen = false;
                }
            }
            else
            {
                Players = players;
                PlayersWithoutChanges = players;

                popupPlayers.IsOpen = true;
                Keyboard.Focus(dgPlayers);

                dgPlayers.Items.Refresh();
            }

            //Globals.Current.LoadingWindow.Close();

            PlayersPopup_OnLoaded();
        }

        private void btnPlayersSave_Click(object sender, RoutedEventArgs e)
        {
            if (Players != null)
            {
                foreach (var clientPlayer in Players)
                {
                    //save / update player
                    if (clientPlayer.ShouldUpdate)
                    {
                        // add current server with the player
                        if (Server != null)
                        {

                            if (clientPlayer.PlayerServers == null)
                                clientPlayer.PlayerServers = new ObservableCollection<Server>();

                            // dupe check just in case
                            var server = clientPlayer.PlayerServers.FirstOrDefault(i => i.Id == Server.Id);
                            if (server == null)
                            {
                                clientPlayer.IsOnline = true;
                                clientPlayer.PlayerServers.Add(Server);
                            }
                        }

                        MainViewModel.SavePlayer(clientPlayer);
                        MainViewModel.ResetViewSourceAssociations();
                    }
                }
            }

            popupPlayers.IsOpen = false;
        }

        private void btnPlayersCancel_Click(object sender, RoutedEventArgs e)
        {
            var unsavedChangedDetected = false;

            foreach (var clientPlayer in Players)
            {
                if (clientPlayer.ShouldUpdate)
                {
                    unsavedChangedDetected = true;
                    break;
                }
            }

            if (unsavedChangedDetected)
            {
                var dialog = new DialogWindow(
                    DialogWindow.DialogType.QuestionYesNo, 
                    "Unsaved Changes!", 
                    "Continue without saving changes? \r\n\r\n - Click 'Yes' to cancel changes... \r\n - Click 'No' to go back and save changes...",
                    this);
                dialog.ShowDialog();

                if (dialog.Result)
                {
                    // reset Players for next load
                    Players = PlayersWithoutChanges;
                }
                else
                {
                    popupPlayers.IsOpen = true;
                    return;
                }
            }

            popupPlayers.IsOpen = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var eventToFire = PropertyChanged;
            if (eventToFire == null)
                return;

            eventToFire(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Click_PlayerIsFriend(object sender, RoutedEventArgs e)
        {
            var result = false;
            var control = sender as CheckBox;
            if (control != null)
            {
                result = control.IsChecked != null && control.IsChecked == true;
            }

            if (DataGridSelectedItem != null)
            {
                DataGridSelectedItem.IsFriend = result;
                DataGridSelectedItem.ShouldUpdate = true;

                //Globals.Current.SavePlayerToDisk(DataGridSelectedItem);
            }
        }

        private void Click_PlayerIsEnemy(object sender, RoutedEventArgs e)
        {
            var result = false;
            var control = sender as CheckBox;
            if (control != null)
            {
                result = control.IsChecked != null && control.IsChecked == true;
            }

            if (DataGridSelectedItem != null)
            {
                DataGridSelectedItem.IsEnemy = result;
                DataGridSelectedItem.ShouldUpdate = true;

                //Globals.Current.SavePlayerToDisk(DataGridSelectedItem);
            }
        }

        private void Click_PlayerNotes(object sender, RoutedEventArgs e)
        {
            var viewSource = new CollectionViewSource();

            switch (PageType)
            {
                case "ArmaLauncher.Servers":
                    viewSource = Globals.Current.PageServers.ViewModel.ViewSource;
                    break;
                case "ArmaLauncher.Favorites":
                    viewSource = Globals.Current.PageFavorites.ViewModel.ViewSource;
                    break;
                case "ArmaLauncher.Recent":
                    viewSource = Globals.Current.PageRecent.ViewModel.ViewSource;
                    break;
                case "ArmaLauncher.Notes":
                    viewSource = Globals.Current.PageNotes.ViewModel.ViewSource;
                    break;
                case "ArmaLauncher.Players":
                    viewSource = Globals.Current.PagePlayers.ViewModel.ViewSource;
                    break;
            }

            new EditPlayerPopup(DataGridSelectedItem, viewSource);
        }

        private void Clicked_DeletePlayer(object sender, RoutedEventArgs e)
        {
            var result = false;
            var control = sender as CheckBox;
            if (control != null)
            {
                result = control.IsChecked != null && control.IsChecked == true;
            }

            if (DataGridSelectedItem != null)
            {
                DataGridSelectedItem.ShouldDelete = result;
                DataGridSelectedItem.ShouldUpdate = true;

                //Globals.Current.SavePlayerToDisk(DataGridSelectedItem);
            }
        }

        private void PlayersPopup_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void PlayersPopup_OnLoaded()
        {
            var left = 0.0;
            var top = 0.0;
            var width = 0.0;
            var height = 0.0;

            var typeString = ParentFrameworkElement.GetType().AssemblyQualifiedName;

            if (!String.IsNullOrEmpty(typeString) && typeString.Contains("Popup"))
            {
                var targetToLoadOver = ParentFrameworkElement as Popup;
                if (targetToLoadOver != null)
                {
                    left = targetToLoadOver.HorizontalOffset;
                    top = targetToLoadOver.VerticalOffset;
                    width = targetToLoadOver.Width;
                    height = targetToLoadOver.Height;
                }

                this.HorizontalOffset = left + (width / 6);
                this.VerticalOffset = top - (height / 1.5);
            }
            else
            {
                var targetToLoadOver = ParentFrameworkElement as Window;
                if (targetToLoadOver != null)
                {
                    left = targetToLoadOver.Left;
                    top = targetToLoadOver.Top;
                    width = targetToLoadOver.Width;
                    height = targetToLoadOver.Height;
                }

                this.HorizontalOffset = left + (width / 6);
                this.VerticalOffset = top + (height / 1.1);
            }
        }
    }
}
