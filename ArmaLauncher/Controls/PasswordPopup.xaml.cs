using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using ArmaLauncher.Models;
using ArmaLauncher.ViewModel;

namespace ArmaLauncher.Controls
{
    /// <summary>
    /// Interaction logic for PasswordPopup.xaml
    /// </summary>
    public partial class PasswordPopup : Popup, INotifyPropertyChanged
    {
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

        public PasswordPopup(Server serverItem, FrameworkElement parentFrameworkElement)
        {
            InitializeComponent();
            this.DataContext = this;
            ParentFrameworkElement = parentFrameworkElement;

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
                popupPassword.HorizontalOffset += e.HorizontalChange;
                popupPassword.VerticalOffset += e.VerticalChange;
            };
            // Move window end

            ServerItem = serverItem;

            if (serverItem != null && !string.IsNullOrEmpty(serverItem.Metadata.ServerPassword))
            {
                tbPassword.Text = serverItem.Metadata.ServerPassword;
            }

            popupPassword.IsOpen = true;
            Keyboard.Focus(tbPassword);
            PasswordPopup_OnLoaded();
        }

        private void btnPasswordConnect_Click(object sender, RoutedEventArgs e)
        {
            if (ServerItem != null)
            {
                if (!string.IsNullOrEmpty(tbPassword.Text))
                {
                    ServerItem.Metadata.ServerPassword = tbPassword.Text;

                    //save / update server
                    MainViewModel.SaveServer(ServerItem);

                    Globals.Current.ConnectToServer(ServerItem);
                }
            }

            popupPassword.IsOpen = false;
        }

        private void btnPasswordCancel_Click(object sender, RoutedEventArgs e)
        {
            popupPassword.IsOpen = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var eventToFire = PropertyChanged;
            if (eventToFire == null)
                return;

            eventToFire(this, new PropertyChangedEventArgs(propertyName));
        }

        private void PasswordPopup_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void PasswordPopup_OnLoaded()
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

                this.HorizontalOffset = left + (width / 2.5);
                this.VerticalOffset = top + (height / 1.5);
            }
        }
    }
}
