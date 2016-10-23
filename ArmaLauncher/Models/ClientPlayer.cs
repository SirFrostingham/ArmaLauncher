using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ArmaLauncher.Models
{
    public class ClientPlayer
    {
        public ClientPlayer()
        {
            PlayerServers = new ObservableCollection<Server>();
        }

        public Player Player { get; set; }
        public bool IsOnline { get; set; }
        public bool IsFriend { get; set; }
        public bool IsEnemy { get; set; }
        public bool HasNotes { get; set; }
        public string Notes { get; set; }
        public bool ShouldUpdate { get; set; }
        public bool ShouldDelete { get; set; }
        public GameType ArmaGameType { get; set; }

        public ObservableCollection<Server> PlayerServers { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var eventToFire = PropertyChanged;
            if (eventToFire == null)
                return;

            eventToFire(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
