using System;
using SteamLib.Protocols;

namespace SteamLib
{
    /// <summary>
    /// A class to query gaming servers
    /// </summary>
    public class GameServer
    {
        public string Host;
        public int QueryPort;
        private int _timeOut = 1500;
        public GameType Type;
        private Protocol _serverInfo;

        private bool _debugMode = false;

        /// <param name="host">The IP address or the hostname of the gameserver</param>
        /// <param name="queryPort">The QueryPort of the gameserver</param>
        /// <param name="type">The gameserver type</param>
        public GameServer(string host, int queryPort, GameType type)
        {
            Host = host;
            QueryPort = queryPort;
            Type = type;

            CheckServerType();
        }

        /// <param name="host">The IP address or the hostname of the gameserver</param>
        /// <param name="queryPort">The QueryPort of the gameserver</param>
        /// <param name="type">The gameserver type</param>
        /// <param name="timeout">The timeout for the query</param>
        public GameServer(string host, int queryPort, GameType type, int timeout)
        {
            Host = host;
            QueryPort = queryPort;
            Type = type;
            _timeOut = timeout;

            CheckServerType();
        }

        /// <summary>
        /// 1/3/15 created for client 'GameServer'
        /// </summary>
        public GameServer()
        {
        }

        private void CheckServerType()
        {
            switch ((GameProtocol)Type)
            {

                case GameProtocol.Arma:
                    _serverInfo = new Arma(Host, QueryPort);
                    break; // TODO: might not be correct. Was : Exit Select
                case GameProtocol.HalfLife:
                    _serverInfo = new HalfLife(Host, QueryPort);
                    break; // TODO: might not be correct. Was : Exit Select
                case GameProtocol.Source:
                    _serverInfo = new Source(Host, QueryPort);
                    break; // TODO: might not be correct. Was : Exit Select
                default:
                    // GameProtocol.None
                    _serverInfo = new Source(Host, QueryPort);
                    break; // TODO: might not be correct. Was : Exit Select
            }
            _serverInfo.DebugMode = _debugMode;
        }

        /// <summary>
        /// Querys the serverinfos
        /// </summary>
        public void QueryServer()
        {
            try
            {
                _serverInfo.GetServerInfo();
            }
            catch (Exception e)
            {
                GlobalsLib.Current.Logger.Error(e);
            }
        }

        /// <summary>
        /// Cleans the color codes from the player names
        /// </summary>
        /// <param name="name">Playername</param>
        /// <returns>Cleaned playername</returns>
        public static string CleanName(string name)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("(\\^\\d)|(\\$\\d)");
            return regex.Replace(name, "");
        }


        /// <summary>
        /// Gets or sets the connectiontimeout
        /// </summary>
        public int Timeout
        {
            get { return _serverInfo.Timeout; }
            set { _serverInfo.Timeout = value; }
        }

        /// <summary>
        /// Gets the parsed parameters
        /// </summary>
        public System.Collections.Specialized.StringDictionary Parameters
        {
            get { return _serverInfo.Parameters; }
        }

        /// <summary>
        /// Gets if the server is online
        /// </summary>
        public bool IsOnline
        {
            get { return _serverInfo.IsOnline; }
        }

        /// <summary>
        /// Gets the time the last scan
        /// </summary>
        public DateTime ScanTime
        {
            get { return _serverInfo.ScanTime; }
        }

        /// <summary>
        /// Gets the players on the server
        /// </summary>
        public PlayerCollection Players
        {
            get { return _serverInfo.Players; }
        }

        /// <summary>
        /// Get the teamnames if there are any
        /// </summary>
        public System.Collections.Specialized.StringCollection Teams
        {
            get { return _serverInfo.Teams; }
        }

        /// <summary>
        /// Gets the maximal player number
        /// </summary>
        public int MaxPlayers
        {
            get { return _serverInfo.MaxPlayers; }
        }

        /// <summary>
        /// Gets the number of players on the server
        /// </summary>
        public int NumPlayers
        {
            get { return _serverInfo.NumPlayers; }
        }

        /// <summary>
        /// Gets the servername
        /// </summary>
        private string _name;
        public string Name
        {
            get { return string.IsNullOrEmpty(_serverInfo.Name) ? _name : _serverInfo.Name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets the active modification
        /// </summary>
        public string Mod
        {
            get { return _serverInfo.Mod; }
        }

        /// <summary>
        /// Gets the mapname
        /// </summary>
        public string Map
        {
            get { return _serverInfo.Map; }
        }

        /// <summary>
        /// Gets if the server is password protected
        /// </summary>
        public bool Passworded
        {
            get { return _serverInfo.Passworded; }
        }

        /// <summary>
        /// Gets the server gametype
        /// </summary>
        public GameType GameType
        {
            get { return Type; }
        }

        /// <summary>
        /// Gets the used protocol
        /// </summary>
        /// <value></value>
        public GameProtocol Protocol
        {
            get { return (GameProtocol)Type; }
        }

        /// <summary>
        /// Enables the debugging mode
        /// </summary>
        public bool DebugMode
        {
            get { return _debugMode; }
            set
            {
                if (_serverInfo != null)
                {
                    _serverInfo.DebugMode = value;
                }
                _debugMode = value;
            }
        }

    }
}


