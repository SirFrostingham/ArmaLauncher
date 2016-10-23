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
using ArmaLauncher.Helpers;
using ArmaLauncher.Models;
using ArmaLauncher.ViewModel;
using NLog.Targets;

namespace ArmaLauncher.Controls
{
    /// <summary>
    /// Interaction logic for DialogWindow.xaml
    /// </summary>
    public partial class DialogWindow : Window, INotifyPropertyChanged
    {
        public enum DialogType
        {
            Info,
            ErrorInfo,
            ErrorYesNo,
            QuestionYesNo
        }

        private bool _result;

        public bool Result
        {
            get
            {
                return _result;
            }
            set
            {
                if (_result == value) return;
                _result = value;
                OnPropertyChanged("Result");
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

        public DialogWindow(DialogType dialogType, string title, string message, FrameworkElement parentFrameworkElement, bool result = false)
        {
            InitializeComponent();
            this.DataContext = this;

            ParentFrameworkElement = parentFrameworkElement;
            Result = result;

            //set up dialog
            switch (dialogType)
            {
                case DialogType.Info:
                    lblTitle.Content = title;
                    tbDialog.Text = message;
                    btnYes.Visibility = Visibility.Hidden;
                    btnNo.Visibility = Visibility.Hidden;
                    btnOk.Visibility = Visibility.Visible;
                    btnOk.Width = 0;
                    break;
                case DialogType.ErrorInfo:
                    lblTitle.Foreground = new SolidColorBrush(Colors.OrangeRed);
                    borderDialog.BorderBrush = new SolidColorBrush(Colors.Firebrick);
                    lblTitle.Content = title;
                    tbDialog.Text = message;
                    btnYes.Visibility = Visibility.Hidden;
                    btnYes.Width = 0;
                    btnNo.Visibility = Visibility.Hidden;
                    btnNo.Width = 0;
                    btnOk.Visibility = Visibility.Visible;
                    break;
                case DialogType.ErrorYesNo:
                    lblTitle.Foreground = new SolidColorBrush(Colors.OrangeRed);
                    borderDialog.BorderBrush = new SolidColorBrush(Colors.Firebrick);
                    lblTitle.Content = title;
                    tbDialog.Text = message;
                    btnYes.Visibility = Visibility.Visible;
                    btnNo.Visibility = Visibility.Visible;
                    btnOk.Visibility = Visibility.Hidden;
                    btnOk.Width = 0;
                    break;
                case DialogType.QuestionYesNo:
                    lblTitle.Foreground = new SolidColorBrush(Colors.Yellow);
                    borderDialog.BorderBrush = new SolidColorBrush(Colors.Goldenrod);
                    lblTitle.Content = title;
                    tbDialog.Text = message;
                    btnYes.Visibility = Visibility.Visible;
                    btnNo.Visibility = Visibility.Visible;
                    btnOk.Visibility = Visibility.Hidden;
                    btnOk.Width = 0;
                    break;
                default:
                    lblTitle.Content = title;
                    tbDialog.Text = message;
                    btnYes.Visibility = Visibility.Hidden;
                    btnYes.Width = 0;
                    btnNo.Visibility = Visibility.Hidden;
                    btnNo.Width = 0;
                    btnOk.Visibility = Visibility.Visible;
                    break;
            }
            if (dialogType == DialogType.Info)
            {
                lblTitle.Content = title;
                tbDialog.Text = message;
            }

            Keyboard.Focus(tbDialog);
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            dialogWindow.Close();
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            dialogWindow.Close();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            dialogWindow.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var eventToFire = PropertyChanged;
            if (eventToFire == null)
                return;

            eventToFire(this, new PropertyChangedEventArgs(propertyName));
        }

        private void DialogWindow_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void DialogWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        //private void DialogWindow_OnLoaded(object sender, RoutedEventArgs e)
        //{
        //    Application curApp = Application.Current;
        //    Window mainWindow = curApp.MainWindow;
        //    this.Left = mainWindow.Left + (mainWindow.Width - this.ActualWidth) / 2;
        //    this.Top = mainWindow.Top + (mainWindow.Height - this.ActualHeight) / 2;
        //}

        private void DialogWindow_OnLoaded(object sender, RoutedEventArgs e)
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
        }
    }
}
