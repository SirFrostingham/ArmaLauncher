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
    /// Interaction logic for EditServerPopup.xaml
    /// </summary>
    public partial class EditServerPopup : Popup, INotifyPropertyChanged
    {
        public CollectionViewSource ViewSource { get; set; }

        private bool IsAddServer { get; set; }

        private Server _serverItem;

        public Server ServerItem
        {
            get
            {
                return _serverItem;
            }
            set
            {
                if (_serverItem == value) return;
                _serverItem = value;
                OnPropertyChanged("ServerItem");
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

        public EditServerPopup(Server serverItem, CollectionViewSource viewSource, bool isAddServer = false)
        {
            InitializeComponent();
            this.DataContext = this;
            //todo:aaron- figure out another way instead of passing FE from parent object
            ParentFrameworkElement = Application.Current.MainWindow;
            ViewSource = viewSource;
            IsAddServer = isAddServer;

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
                popEditServer.HorizontalOffset += e.HorizontalChange;
                popEditServer.VerticalOffset += e.VerticalChange;
            };
            // Move window end

            //edit
            if (serverItem != null)
            {

                if (!string.IsNullOrEmpty(serverItem.Host))
                {
                    tbIp.Text = serverItem.Host;
                }

                if (!string.IsNullOrEmpty(serverItem.GamePort.ToString(CultureInfo.InvariantCulture)))
                {
                    tbPort.Text = serverItem.GamePort.ToString(CultureInfo.InvariantCulture);
                }

                if (!string.IsNullOrEmpty(serverItem.QueryPort.ToString()))
                {
                    tbSteamQueryPort.Text = serverItem.QueryPort.ToString();
                }

                if (!string.IsNullOrEmpty(serverItem.Name))
                {
                    tbName.Text = serverItem.Name;
                }

                if (serverItem.Metadata.HasNotes)
                {
                    serverItem.Metadata.HasNotes = true;
                    tbNotes.Text = serverItem.Metadata.Notes;
                }
                else
                {
                    serverItem.Metadata.HasNotes = false;
                }
            }

            if (IsAddServer)
            {
                lblTitleEditServer.Content = "Add Server";
            }

            ServerItem = serverItem;

            //Must refesh view
            ViewSource.View.Refresh();

            popEditServer.IsOpen = true;
            Keyboard.Focus(tbNotes);

            EditServerPopup_OnLoaded();
        }

        private void btnEditServerSave_Click(object sender, RoutedEventArgs e)
        {
            if (ServerItem != null)
            {
                ServerItem.Host = tbIp.Text;
                ServerItem.GamePort = Convert.ToInt32(tbPort.Text);
                ServerItem.QueryPort = String.IsNullOrEmpty(tbSteamQueryPort.Text) ? Convert.ToInt32(tbPort.Text) + 1 : Convert.ToInt32(tbSteamQueryPort.Text);
                ServerItem.Name = tbName.Text;
                ServerItem.Metadata.Notes = tbNotes.Text;
                ServerItem.Id = string.Format("{0}{1}", ServerItem.Host.Replace(".", ""), ServerItem.QueryPort);

                //edit
                if (!string.IsNullOrEmpty(tbNotes.Text))
                {
                    ServerItem.Metadata.HasNotes = true;
                }
                else
                {
                    ServerItem.Metadata.HasNotes = false;
                }

                // for Add / not Edit
                if (IsAddServer)
                {
                    if (!ServerItem.Metadata.IsFavorite)
                        ServerItem.Metadata.IsFavorite = true;
                }

                if (string.IsNullOrEmpty(ServerItem.Metadata.ArmaGameType.GetDescription()))
                    ServerItem.Metadata.ArmaGameType = Globals.Current.ArmaGameType;

                //save / update server
                MainViewModel.SaveServer(ServerItem);
            }

            //refresh
            ViewSource.View.Refresh();

            popEditServer.IsOpen = false;
        }

        private void btnEditServerCancel_Click(object sender, RoutedEventArgs e)
        {
            //refresh
            ViewSource.View.Refresh();

            popEditServer.IsOpen = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var eventToFire = PropertyChanged;
            if (eventToFire == null)
                return;

            eventToFire(this, new PropertyChangedEventArgs(propertyName));
        }

        private void EditServerPopup_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void EditServerPopup_OnLoaded()
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

                this.HorizontalOffset = left + (width / 3);
                this.VerticalOffset = top - (height / 3);
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

                this.HorizontalOffset = left + (width / 3);
                this.VerticalOffset = top + (height / 3);
            }
        }
    }
}
