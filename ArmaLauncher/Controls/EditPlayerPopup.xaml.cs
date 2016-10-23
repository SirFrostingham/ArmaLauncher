using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
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
using ArmaLauncher.Helpers;
using ArmaLauncher.Models;
using ArmaLauncher.ViewModel;

namespace ArmaLauncher.Controls
{
    /// <summary>
    /// Interaction logic for EditPlayerPopup.xaml
    /// </summary>
    public partial class EditPlayerPopup : Popup, INotifyPropertyChanged
    {
        public CollectionViewSource ViewSource { get; set; }

        private bool IsAddPlayer { get; set; }

        private ClientPlayer _playerItem;

        public ClientPlayer PlayerItem
        {
            get
            {
                return _playerItem;
            }
            set
            {
                if (_playerItem == value) return;
                _playerItem = value;
                OnPropertyChanged("PlayerItem");
            }
        }

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

        public EditPlayerPopup(ClientPlayer playerItem, CollectionViewSource viewSource, bool isAddPlayer = false)
        {
            InitializeComponent();
            this.DataContext = this;
            //todo:aaron- figure out another way instead of passing FE from parent object
            ParentFrameworkElement = Application.Current.MainWindow;
            ViewSource = viewSource;
            IsAddPlayer = isAddPlayer;

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
                popEditPlayer.HorizontalOffset += e.HorizontalChange;
                popEditPlayer.VerticalOffset += e.VerticalChange;
            };
            // Move window end

            if (playerItem != null)
            {
                if (!string.IsNullOrEmpty(playerItem.Player.Name))
                {
                    tbName.Text = playerItem.Player.Name;
                }

                if (playerItem.HasNotes)
                {
                    playerItem.HasNotes = true;
                    tbNotes.Text = playerItem.Notes;
                }
                else
                {
                    playerItem.HasNotes = false;
                }
            }

            if (IsAddPlayer)
            {
                lblTitleEditPlayer.Content = "Add Player";
                tbName.IsReadOnly = false;
            }

            PlayerItem = playerItem;

            //Must refesh view
            ViewSource.View.Refresh();

            popEditPlayer.IsOpen = true;

            Keyboard.Focus(IsAddPlayer ? tbName : tbNotes);

            EditPlayerPopup_OnLoaded();
        }

        private void btnEditPlayerSave_Click(object sender, RoutedEventArgs e)
        {
            if (PlayerItem != null)
            {
                PlayerItem.Notes = tbNotes.Text;

                //add
                if (IsAddPlayer)
                    PlayerItem.Player.Name = tbName.Text;

                //edit
                if (!string.IsNullOrEmpty(tbNotes.Text))
                {
                    PlayerItem.HasNotes = true;
                    PlayerItem.ShouldUpdate = true;
                }
                else
                {
                    PlayerItem.HasNotes = false;
                }

                if (string.IsNullOrEmpty(PlayerItem.ArmaGameType.GetDescription()))
                    PlayerItem.ArmaGameType = Globals.Current.ArmaGameType;

                //save / update player
                MainViewModel.SavePlayer(PlayerItem);
            }

            //refresh
            ViewSource.View.Refresh();

            popEditPlayer.IsOpen = false;
        }

        private void btnEditPlayerCancel_Click(object sender, RoutedEventArgs e)
        {
            //refresh
            ViewSource.View.Refresh();

            popEditPlayer.IsOpen = false;
        }

        private void Click_PlayerIsFriend(object sender, RoutedEventArgs e)
        {
            var result = false;
            var control = sender as CheckBox;
            if (control != null)
            {
                result = control.IsChecked != null && control.IsChecked == true;
            }

            if (PlayerItem != null)
            {
                PlayerItem.IsFriend = result;
                PlayerItem.ShouldUpdate = true;

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

            if (PlayerItem != null)
            {
                PlayerItem.IsEnemy = result;
                PlayerItem.ShouldUpdate = true;

                //Globals.Current.SavePlayerToDisk(DataGridSelectedItem);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var eventToFire = PropertyChanged;
            if (eventToFire == null)
                return;

            eventToFire(this, new PropertyChangedEventArgs(propertyName));
        }

        private void EditPlayerPopup_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
        
        private void EditPlayerPopup_OnLoaded()
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
