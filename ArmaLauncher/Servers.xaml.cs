using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ArmaLauncher.ViewModel;

namespace ArmaLauncher
{
    /// <summary>
    ///     Interaction logic for Servers.xaml
    /// </summary>
    public partial class Servers : Page, INotifyPropertyChanged
    {
        public MainViewModel ViewModel { get; set; }

        public Servers()
        {
            InitializeComponent();
            InitializeObjects();
            DataContext = this;
            var pageType = this.GetType().FullName;
            ViewModel = new MainViewModel(pageType);
            Globals.Current.ViewModel = ViewModel;
            Globals.Current.PageServers = this;
        }

        private void InitializeObjects()
        {
            //reset grid
            this.MainDataGrid.AutoGenerateColumns = true;
            this.MainDataGrid.AutoGenerateColumns = false;
            this.MainDataGrid.Items.Refresh();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var eventToFire = PropertyChanged;
            if (eventToFire == null)
                return;

            eventToFire(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Servers_OnLoaded(object sender, RoutedEventArgs e)
        {
            InitializeObjects();
        }
    }
}