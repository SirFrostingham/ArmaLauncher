using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
using GalaSoft.MvvmLight.Messaging;
using FirstFloor.ModernUI.Windows.Controls;

namespace ArmaLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow, INotifyPropertyChanged
    {
        public MainWindow()
        {
            Helpers.UiServices.SetBusyState();
            InitializeComponent();
            DataContext = this;
            Application.Current.MainWindow = this;
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            this.DataContext = this;
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.WindowState = Globals.Current.AppStateMaximized ? WindowState.Maximized : WindowState.Normal;

            CommandBindings.Add(new CommandBinding(AppStateOpen));
            CommandBindings.Add(new CommandBinding(AppStateMaximize));
            CommandBindings.Add(new CommandBinding(AppStateMinimize));

            Application.Current.Exit += new ExitEventHandler(Current_Exit);

            // navigate to 1st page
            this.ContentSource = new Uri("/Servers.xaml", UriKind.Relative);
        }

        private void Current_Exit(object sender, ExitEventArgs e)
        {
            Globals.Logout();
        }   


        public ICommand AppStateOpen
        {
            get { return new DelegateCommand(DoubleClickCommandExecuted); }
        }

        public ICommand AppStateMaximize
        {
            get { return new DelegateCommand(AppStateMaximizedCommandExecuted); }
        }

        public ICommand AppResetWindowPosition
        {
            get { return new DelegateCommand(AppResetWindowPositonCommandExecuted); }
        }

        private void AppStateMaximizedCommandExecuted(object parameter)
        {
            this.WindowState = WindowState.Maximized;
        }

        public ICommand AppStateMinimize
        {
            get { return new DelegateCommand(AppStateMinimizedCommandExecuted); }
        }

        private void AppStateMinimizedCommandExecuted(object parameter)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void AppResetWindowPositonCommandExecuted(object parameter)
        {
            Globals.Current.AppWidth = Convert.ToDouble(ConfigurationManager.AppSettings["AppWidth"]);
            Globals.Current.AppHeight = Convert.ToDouble(ConfigurationManager.AppSettings["AppHeight"]);
            Globals.Current.AppTop = Convert.ToDouble(ConfigurationManager.AppSettings["AppTop"]);
            Globals.Current.AppLeft = Convert.ToDouble(ConfigurationManager.AppSettings["AppLeft"]);
            Globals.Current.AppStateMaximized = Convert.ToBoolean(ConfigurationManager.AppSettings["AppStateMaximized"]);
        }

        public ICommand AppStateClose
        {
            get { return new DelegateCommand(AppStateCloseCommandExecuted); }
        }

        private void AppStateCloseCommandExecuted(object parameter)
        {
            Application.Current.MainWindow.Close();
        }

        public ICommand DoubleClickCommand
        {
            get { return new DelegateCommand(DoubleClickCommandExecuted); }
        }

        private void DoubleClickCommandExecuted(object parameter)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        // THIS METHOD MINIMIZES TO SYSTEM TRAY, NOT TASKBAR
        //protected override void OnStateChanged(EventArgs e)
        //{
        //    if (WindowState == WindowState.Minimized)
        //    {
        //        this.Hide();
        //    }

        //    base.OnStateChanged(e);
        //}

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ModernWindow_Closing(object sender, CancelEventArgs e)
        {
            //save  everything on exit
            Globals.Current.SaveArmaServersToDisk();
            Globals.Current.SaveArmaFavoriteServersToDisk();
            Globals.Current.SaveArmaRecentServersToDisk();
            Globals.Current.SaveArmaNotesServersToDisk();
            Globals.Current.SaveArmaPasswordServersToDisk();
            Globals.Current.SaveArmaPlayersToDisk();

            if(this.WindowState == WindowState.Maximized)
            {
                Globals.Current.AppWidth = this.Width;
                Globals.Current.AppHeight = this.Height;
                Globals.Current.AppTop = this.Top;
                Globals.Current.AppLeft = this.Left;
                Globals.Current.AppStateMaximized = true;
            }
            else
            {
                Globals.Current.AppWidth = this.Width;
                Globals.Current.AppHeight = this.Height;
                Globals.Current.AppTop = this.Top;
                Globals.Current.AppLeft = this.Left;
                Globals.Current.AppStateMaximized = false;
            }
        }
    }
}
