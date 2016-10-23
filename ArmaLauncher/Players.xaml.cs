using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ArmaLauncher.Models;
using ArmaLauncher.ViewModel;

namespace ArmaLauncher
{
    /// <summary>
    ///     Interaction logic for Players.xaml
    /// </summary>
    public partial class Players : Page, INotifyPropertyChanged
    {

        private ClientPlayer _dataGridSelectedItem;
        public ClientPlayer DataGridSelectedItem
        {
            get { return _dataGridSelectedItem; }
            set
            {
                _dataGridSelectedItem = value;
                PlayersDataGrid.UpdateLayout();
            }
        }
        public MainViewModel ViewModel { get; set; }

        public Players()
        {
            InitializeComponent();
            InitializeObjects();
            DataContext = this;
            var pageType = this.GetType().FullName;
            ViewModel = new MainViewModel(pageType);
            Globals.Current.ViewModel = ViewModel;
            Globals.Current.PagePlayers = this;
        }

        private void InitializeObjects()
        {
            //reset grid
            this.PlayersDataGrid.AutoGenerateColumns = true;
            this.PlayersDataGrid.AutoGenerateColumns = false;
            this.PlayersDataGrid.Items.Refresh();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var eventToFire = PropertyChanged;
            if (eventToFire == null)
                return;

            eventToFire(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Players_OnLoaded(object sender, RoutedEventArgs e)
        {
            InitializeObjects();
        }
    }
}