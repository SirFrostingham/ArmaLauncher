using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamLib;

namespace ArmaLauncher.Models
{
    public class Server : INotifyPropertyChanged
    {
        public Server()
        {
            Metadata = new ServerMetadata();
            Game = new Game();
            ModsList = new List<Param>();
            ModNamesList = new List<Param>();
            ModHashesList = new List<Param>();
            ParamsList = new List<Param>();
            SigNamesList = new List<Param>();
        }

        // server XML feed
        public bool? Battleye { get; set; }
        public string Country { get; set; }
        public DateTime? Createdat { get; set; }
        public bool? Dedicated { get; set; }
        public string Gamename { get; set; }
        public string Host { get; set; }
        public string Id { get; set; }
        public string Island { get; set; }
        public int? Language { get; set; }
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
        public string Mission { get; set; }
        public string Mod { get; set; }
        public string Mode { get; set; }
        public string Modhash { get; set; }
        public string Name { get; set; }
        public bool? Passworded { get; set; }
        public string Platform { get; set; }
        public int? NumPlayers { get; set; }
        public int? MaxPlayers { get; set; }
        public string DisplayNumPlayers { get; set; }
        public int GamePort { get; set; }
        public int? QueryPort { get; set; }
        public string Requiredversion { get; set; }
        public string Signatures { get; set; }
        public string State { get; set; }
        public DateTime? Updatedate { get; set; }
        public bool? Verifysignatures { get; set; }
        public string Version { get; set; }

        //new parameters
        public string SigNamesDisplay { get; set; }
        public List<Param> SigNamesList { get; set; }
        public string ParamsDisplay { get; set; }
        public List<Param> ParamsList { get; set; }
        public string ModsDisplay { get; set; }
        public List<Param> ModsList { get; set; }
        public string ModNamesDisplay { get; set; }
        public List<Param> ModNamesList { get; set; }
        public string ModHashesDisplay { get; set; }
        public List<Param> ModHashesList { get; set; }
        public string Timeleft {get; set;}
        public string Secureserver {get; set;}
        public string Botcount {get; set;}
        public string Appid {get; set;}
        public string Gameversion {get; set;}
        public string Hash {get; set;}
        public string Mapname {get; set;}
        public string Players {get; set;}
        public string Modname {get; set;}
        public string Protocolver {get; set;}
        public string Serveros {get; set;}
        public string Servertype {get; set;}

        // single server call
        public Game Game { get; set; }

        // extended
        public ServerMetadata Metadata { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var eventToFire = PropertyChanged;
            if (eventToFire == null)
                return;

            eventToFire(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Param
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class Game
    {
        public Game()
        {
            Players = new PlayerCollection();
        }

        public int? Id { get; set; }
        public string Mission { get; set; }
        public string Island { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? EndTime { get; set; }
        public int? PlayerTime { get; set; }
        public DateTime? Updated { get; set; }
        public int? MaxPlayers { get; set; }
        public PlayerCollection Players { get; set; }
    }

    public class Player
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string Team { get; set; }
        public DateTime? ConnectTime { get; set; }
        public DateTime? DisconnectTime { get; set; }
        public int? Kills { get; set; }
        public int? Deaths { get; set; }
        public int? Score { get; set; }
    }

    public class ServerMetadata
    {
        public ServerMetadata()
        {
            Ping = null;
            LastPlayedDate = "never";
        }

        public bool EnterPasswordOnConnect { get; set; }
        public string ServerPassword { get; set; }
        public bool IsFavorite { get; set; }
        public bool ShouldDelete { get; set; }
        public GameType ArmaGameType { get; set; }
        public bool HasNotes { get; set; }
        public string Notes { get; set; }
        public long? Ping { get; set; }
        public string PingDisplay { get; set; }
        public string LastPlayedDate { get; set; }
        public bool AutoMatchClientMod { get; set; }
        public List<string> ClientMods { get; set; }
    }

    //public class ArmaServerModels : ObservableCollection<Server> { }
}
