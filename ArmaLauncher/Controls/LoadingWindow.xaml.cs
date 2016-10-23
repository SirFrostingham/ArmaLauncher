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
using System.Windows.Shapes;

namespace ArmaLauncher.Controls
{
    /// <summary>
    /// Interaction logic for LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window
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
        public LoadingWindow(FrameworkElement parentFrameworkElement)
        {
            InitializeComponent();
            this.DataContext = this;

            ParentFrameworkElement = parentFrameworkElement;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var eventToFire = PropertyChanged;
            if (eventToFire == null)
                return;

            eventToFire(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadingWindow_OnLoaded(object sender, RoutedEventArgs e)
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

                this.Left = left + (width / 3);
                this.Top = top - (height / 1.5);
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

                this.Left = left + (width - this.ActualWidth) / 2;
                this.Top = top + (height - this.ActualHeight) / 2;
            }

            loadingWindow.Height = 100;
            loadingWindow.Width = 100;
            loadingWindow.Visibility = Visibility.Visible;
            loadingAnimation.Visibility = Visibility.Visible;
            Opacity = 1;
        }
    }
}
