using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using SteamLib.Helpers;

namespace SteamLib.MasterServer
{
    public class SourceMasterServer
    {
        // When binding the query socket to a port, 51500 is the lowest port number
        // we'll use.
        public delegate void QueryAsyncCompletedEventHandler(object sender, EventArgs e);

        public delegate void QueryCallBack(object sender, QueryAsyncEventArgs e);

        public delegate void QueryRawCallBack(object sender, QueryRawAsyncEventArgs e);

        private string MasterSeedIp { get; set; }
        private int MasterServerRetries { get; set; }
        private string LastQueryGameMod { get; set; }
        private bool MasterServerQueryFailed { get; set; }
        private List<string> MasterServersAttemptList { get; set; }
        private List<int> MasterPortList { get; set; }
        private bool IsFirstSearch { get; set; }
        private bool MasterServerFound { get; set; }
        private string MasterServer { get; set; }
        private int MasterServerPort { get; set; }
        private EndPoint MasterEndPoint { get; set; }

        public enum QueryFilter
        {
            None = 1,
            Dedicated = 2,
            AntiCheat = 4,
            Linux = 8,
            NotEmpty = 16,
            NotFull = 32,
            SpectatorProxies = 64,
            Empty = 128,
            WhiteListed = 256
        }

        public enum QueryGame
        {
            Arma,
            HalfLife,
            Source
        }

        /// <summary>
        /// Cannot change the order of these regions
        /// </summary>
        public enum QueryRegionCode
        {
            USWestcoast = 1,
            SouthAmerica,
            Europe,
            Asia,
            Australia,
            MiddleEast,
            Africa,
            All = 255
        }

        private const int PORT_LOWER_BOUND = 51500;
        // A datagram can not be larger than 1400 bytes.
        private const int MAX_DATAGRAM_SIZE = 1400;
        // The timeout that we will use in our Receive calls.

        private const int TIMEOUT = 1600;
        private readonly QueryThreadPool queryPool;
        // This callback is used whenever we perform an async master server query
        // with subsequent queries to each game server.

        private QueryCallBack AsyncCallBack;
        // The main thread used for querying the master server.

        // A boolean flag used to denote that cancelation is pending.

        public bool QueryCancellationPending;
        private Thread queryThread;
        // This class is used to enqueue the queries.

        // This ISynchronizeInvoke object will let us marshal callback calls to the
        // Proper thread.

        private ISynchronizeInvoke syncObject;

        public SourceMasterServer()
        {
            queryPool = new QueryThreadPool(Convert.ToInt32(ConfigurationManager.AppSettings["AppMaxThreads"]));
            queryPool.AllQueriesProcessed += QueryPool_AllQueriesProcessed;
            MasterServersAttemptList = new List<string>();
            MasterPortList = new List<int>();
            MasterServerQueryFailed = false;
        }

        public SourceMasterServer(object parameters, string seedIp, QueryCallBack asyncCallBack)
        {
            queryPool = new QueryThreadPool(Convert.ToInt32(ConfigurationManager.AppSettings["AppMaxThreads"]));
            queryPool.AllQueriesProcessed += QueryPool_AllQueriesProcessed;
            MasterSeedIp = seedIp;
            MasterServersAttemptList = new List<string>();
            MasterPortList = new List<int>();
            MasterServerQueryFailed = false;
            //GetGameServerList(parameters);
            
            // The parameters parameter will hold an Object array containing all the 
            // data needed to perform a query.
            var arguments = (object[]) parameters;
            var game = (QueryGame) arguments[0];
            var region = (QueryRegionCode) arguments[1];
            var filter = (QueryFilter) arguments[2];
            var map = (string) arguments[3];
            var gameMod = (string) arguments[4];
            var callBack = (QueryRawCallBack) arguments[5];
            var returnRaw = (bool) arguments[6];

            QueryAsync(game, region, filter, map, gameMod, asyncCallBack);
        }

        public ISynchronizeInvoke SynchronizingObject
        {
            get { return syncObject; }
            set { syncObject = value; }
        }

        public event QueryAsyncCompletedEventHandler QueryAsyncCompleted;

        private void QueryPool_AllQueriesProcessed(object sender, EventArgs e)
        {
            OnQueryAsyncCompleted();
        }

        /// <summary>
        ///     Queries the master server and returns the raw data as an array of IPEndPoint.
        /// </summary>
        /// <param name="game">What type of servers to query for.</param>
        /// <param name="region">What region to filter on.</param>
        /// <param name="filter">Specifies a filter for the query. Several filters can be set using bitwise OR.</param>
        /// <param name="map">A map to filter on. Pass String.Empty for all maps.</param>
        /// <param name="gameMod">A mod to filter on. Pass String.Empty for all mods.</param>
        /// <returns>Returns an array of IPEndPoint</returns>
        /// <remarks></remarks>
        public IPEndPoint[] QueryRaw(QueryGame game, QueryRegionCode region, QueryFilter filter, string map,
            string gameMod)
        {
            int received = 0;
            int retries = 0;
            int maxRetries = 0;
            bool endOfQuery = false;
            bool timeoutOccured = false;
            EndPoint masterEndPoint = MasterServerFound ? MasterEndPoint : GetGameServerList(game);
            var gameServers = new List<IPEndPoint>();
            var udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            byte[] query = BuildQuery(region, filter, map, gameMod, "0.0.0.0:0");

            var buffer = new byte[MAX_DATAGRAM_SIZE];

            // Initially, we will try to re-send the query 10 times
            // if we arent getting any reply.
            maxRetries = 10;

            udpSocket.Bind(new IPEndPoint(IPAddress.Any, 51500));
            udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, TIMEOUT);
            // Send the query
            // Keep querying until the end of the IP list.
            do
            {
                retries = 0;
                // Keep sending the query if there is no reply.
                do
                {
                    retries += 1;
                    udpSocket.SendTo(query, masterEndPoint);
                    try
                    {
                        received = udpSocket.ReceiveFrom(buffer, ref masterEndPoint);
                        timeoutOccured = false;
                    }
                    catch (Exception ex)
                    {
                        GlobalsLib.Current.Logger.Error(ex);
                        // Timeout occured
                        timeoutOccured = true;
                    }
                } while (timeoutOccured & retries < maxRetries);

                // Once we have gotten this far, we KNOW that the server is online,
                // so lets increate the maxRetries.
                maxRetries = 20;
                // If timeoutOccured is True at this point, we know that we where unable
                // to get a reply and had to give up.
                if (timeoutOccured)
                {
                    throw new NoReplyException("No response from the master server.");
                }

                // Add the newly received IPs to the list.
                gameServers.AddRange(ParseReplyBuffer(buffer, received));

                if (IsNullAddress(gameServers[gameServers.Count - 1]))
                {
                    // We have reached the end of the IP list.
                    endOfQuery = true;
                }
                else
                {
                    // We have not yet reached the end of the IP list.
                    // We need to re-query the server to receive more of the list.
                    query = BuildQuery(region, filter, map, gameMod, gameServers[gameServers.Count - 1].ToString());
                }
            } while (!endOfQuery);

            return gameServers.ToArray();
        }

        /// <summary>
        ///     Queries the master server asyncronously and invokes a callback
        ///     each time a portion of the IP list has been received. Returns the servers in raw format,
        ///     that is, as an array of IPEndPoint.
        /// </summary>
        /// <param name="game">What type of servers to query for.</param>
        /// <param name="region">What region to filter on</param>
        /// <param name="filter">Specifies a filter for the query. Several filters can be set using bitwise OR.</param>
        /// <param name="map">A map to filter on. Pass String.Empty for all maps.</param>
        /// <param name="gameMod">A mod to filter on. Pass String.Empty for all mods.</param>
        /// <param name="callback">The callback to be invoked each time a portion of the IP list has been received.</param>
        /// <remarks></remarks>
        public void QueryRawAsync(QueryGame game, QueryRegionCode region, QueryFilter filter, string map, string gameMod,
            QueryRawCallBack callback)
        {
            if ((queryThread != null) && queryThread.IsAlive)
            {
                throw new InvalidOperationException(
                    "Query is already running. You can either cancel the query by calling CancelAsyncQuery, or wait for the query to finish");
            }
            QueryCancellationPending = false;
            queryThread = new Thread(RunAsyncQuery);
            queryThread.IsBackground = true;
            queryThread.Start(new object[]
            {
                game,
                region,
                filter,
                map,
                gameMod,
                callback,
                true
            });
        }

        /// <summary>
        ///     Queries the master server asyncronously and subsequently
        ///     queries the returned game server.
        /// </summary>
        /// <param name="game">What type of servers to query for.</param>
        /// <param name="region">What region to filter on</param>
        /// <param name="filter">Specifies a filter for the query. Several filters can be set using bitwise OR.</param>
        /// <param name="map">A map to filter on. Pass String.Empty for all maps.</param>
        /// <param name="gameMod">A mod to filter on. Pass String.Empty for all mods.</param>
        /// <param name="callback">The callback to be invoked each time a gameserver has been queried.</param>
        /// <remarks></remarks>
        public void QueryAsync(QueryGame game, QueryRegionCode region, QueryFilter filter, string map, string gameMod,
            QueryCallBack callback)
        {
            if ((queryThread != null) && queryThread.IsAlive)
            {
                //throw new InvalidOperationException("Query is already running. You can either cancel the query by calling CancelAsyncQuery, or wait for the query to finish");
                CancelAsyncQuery();
            }
            QueryCancellationPending = false;
            AsyncCallBack = callback;
            queryThread = new Thread(RunAsyncQuery);
            queryThread.IsBackground = true;
            queryThread.Start(new object[]
            {
                game,
                region,
                filter,
                map,
                gameMod,
                new QueryRawCallBack(QueryAsync_CallBack),
                false
            });
        }

        private void RunAsyncQuery(object parameters)
        {
            // The parameters parameter will hold an Object array containing all the 
            // data needed to perform a query.
            var arguments = (object[]) parameters;
            var game = (QueryGame) arguments[0];
            var region = (QueryRegionCode) arguments[1];
            var filter = (QueryFilter) arguments[2];
            var map = (string) arguments[3];
            var gameMod = (string) arguments[4];
            var callBack = (QueryRawCallBack) arguments[5];
            var returnRaw = (bool) arguments[6];

            if (LastQueryGameMod == gameMod)
            {
                //reset server query system
                MasterServerFound = false;
                MasterServer = null;
                MasterServerPort = 0;
                MasterEndPoint = null;
            }

            GetGameServerList(parameters);
        }

        // This method will bind the socket to a portnumber.
        // From investigation I have noticed that the master server wont reply
        // if you use too low source port numbers. This is why we can not
        // let the system assign a random port number for us.
        private void BindSocket(Socket udpSocket)
        {
            var retryAttempts = 5;
            int port = PORT_LOWER_BOUND;
            bool successfullyBound = false;
            do
            {
                if (successfullyBound)
                    break;

                try
                {
                    udpSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                    successfullyBound = true;
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    {
                        successfullyBound = false;
                    }
                    else
                    {
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                retryAttempts += retryAttempts;
            } while (!successfullyBound || retryAttempts != 5);
        }

        // This method is used when QueryAsync has been called. It acts as a callback for the
        // RunAsyncQuery method. It will subsequently query each game server that it receives.
        private void QueryAsync_CallBack(object sender, QueryRawAsyncEventArgs e)
        {
            for (int i = 0; i <= e.GameServers.Length - 1; i++)
            {
                //Dim b As Boolean = System.Threading.ThreadPool.QueueUserWorkItem(AddressOf QueryGameServer, New Object() {e.GameServers(i), e.Type})
                queryPool.AddQuery(QueryGameServer, new object[]
                {
                    e.GameServers[i],
                    e.Type
                });
            }
        }

        // This method acts as a callback to the QueueUserWorkItem call in the QueryAsync_CallBack method.
        private void QueryGameServer(object parameters)
        {
            if (!QueryCancellationPending)
            {
                var arguments = (object[]) parameters;

                try
                {
                    var serverEndPoint = (IPEndPoint)arguments[0];
                    var server = new GameServer(serverEndPoint.Address.ToString(), serverEndPoint.Port,
                        (GameType)arguments[1]);
                    server.QueryServer();
                    if (server.IsOnline && !QueryCancellationPending)
                    {
                        OnInvokeQueryCallBack(AsyncCallBack, new QueryAsyncEventArgs(server));
                    }
                }
                catch (Exception e)
                {
                    GlobalsLib.Current.Logger.Error(e);
                }
            }
        }

        // This method will cancel any current query.
        public void CancelAsyncQuery()
        {
            queryPool.CancelAll();
            QueryCancellationPending = true;

            if (MasterServerQueryFailed)
            {
                //reset server query system
                MasterServerFound = false;
                MasterServer = null;
                MasterServerPort = 0;
                MasterEndPoint = null;
                MasterServersAttemptList = new List<string>();
            }
        }

        // This method takes the reply from the master server, given as a byte array, and
        // returns the IPEndPoints built from it.
        private List<IPEndPoint> ParseReplyBuffer(byte[] buffer, int dataLength)
        {
            var addresses = new List<IPEndPoint>();
            int bufferIndex = 6;
            do
            {
                var ipep = new IPEndPoint(new IPAddress(new[]
                {
                    buffer[bufferIndex],
                    buffer[bufferIndex + 1],
                    buffer[bufferIndex + 2],
                    buffer[bufferIndex + 3]
                }), GetUInt16NetworkOrder(buffer[bufferIndex + 4], buffer[bufferIndex + 5]));

                bufferIndex += 6;

                addresses.Add(ipep);
            } while (!(bufferIndex >= dataLength));

            return addresses;
        }

        /// <summary>
        /// Refactored to TEST the connection...
        /// This method randomly selects one of the IPs that the master servers hostname has been resolved too.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="parameters"></param>
        /// <param name="port"></param>
        /// <param name="masterServer"></param>
        /// <returns></returns>
        private void GetGameServerList(object parameters, string masterServer = null, int? port = null, string passedInSeedIp = null)
        {
            // The parameters parameter will hold an Object array containing all the 
            // data needed to perform a query.
            var arguments = (object[])parameters;
            var game = (QueryGame)arguments[0];
            var region = (QueryRegionCode)arguments[1];
            var filter = (QueryFilter)arguments[2];
            var map = (string)arguments[3];
            var gameMod = (string)arguments[4];
            var callBack = (QueryRawCallBack)arguments[5];
            var listOfMasterServers = new List<string>();
            //if (MasterEndPoint != null && gameMod == LastQueryGameMod) return (IPEndPoint)MasterEndPoint;

            if (masterServer == null)
            {
                listOfMasterServers = game == QueryGame.Arma ? new List<string> { "hl2master.steampowered.com" } 
                    : new List<string> { "hl1master.steampowered.com", "hl2master.steampowered.com" };

                masterServer = listOfMasterServers.PickRandom();
            }

            // test server settings before real get game server list
            var masterPortListDefinition = game == QueryGame.Arma ? new List<int> { 27011 } 
                    : new List<int> { 27010, 27011, 27012, 27013 };
            var done = false;
            while (!done && !MasterServerFound)
            {
                var tempPort = masterPortListDefinition.PickRandom();

                //reset port
                var shouldResetPorts = masterPortListDefinition.Intersect(MasterPortList).Count() == masterPortListDefinition.Count();

                if(shouldResetPorts)
                    MasterPortList = new List<int>();

                var isPortInList = MasterPortList.Any(a => a.Equals(tempPort));
                if (isPortInList) continue;

                int receivedTest = 0;
                var timeoutOccuredTest = false;
                var masterSeedIp = "0.0.0.0:0";
                var bufferTest = new byte[MAX_DATAGRAM_SIZE];
                EndPoint masterEndPointTest = GetGameServerList(masterServer, tempPort);
                byte[] queryTest = BuildQuery(region, filter, map, gameMod, masterSeedIp);
                //TODO:test server:port
                var udpSocketTest = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                udpSocketTest.SendTo(queryTest, masterEndPointTest);
                try
                {
                    receivedTest = udpSocketTest.ReceiveFrom(bufferTest, ref masterEndPointTest);
                    timeoutOccuredTest = false;
                }
                catch (Exception ex)
                {
                    GlobalsLib.Current.Logger.Error(ex);
                    // Timeout occured
                    timeoutOccuredTest = true;
                }

                if (timeoutOccuredTest)
                    continue;

                done = true;
                MasterServer = masterServer;
                MasterServerPort = tempPort;
                MasterServerFound = true;
                MasterEndPoint = masterEndPointTest;
                MasterPortList.Add(tempPort);
            }

            // go for it, and get list

            if (MasterSeedIp == null)
                MasterSeedIp = "0.0.0.0:0";
            else if (passedInSeedIp != null)
                MasterSeedIp = passedInSeedIp;

            int totalServers = 0;
            List<IPEndPoint> gameServers = default(List<IPEndPoint>);
            int received = 0;
            int retries = 0;
            int maxRetries = 0;
            bool endOfQuery = false;
            bool timeoutOccured = false;
            EndPoint masterEndPoint = GetGameServerList(MasterServer, MasterServerPort);
            var udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            byte[] query = BuildQuery(region, filter, map, gameMod, MasterSeedIp);

            var buffer = new byte[MAX_DATAGRAM_SIZE];

            // Initially, we will try to re-send the query 10 times
            // if we arent getting any reply.
            maxRetries = 10;

            //BindSocket(udpSocket);
            //udpSocket.Bind(New System.Net.IPEndPoint(System.Net.IPAddress.Any, 51500))
            udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, TIMEOUT);

            // Send the query
            // Keep querying until the end of the IP list.
            do
            {
                if (QueryCancellationPending)
                    break;

                retries = MasterServerRetries;
                // Keep sending the query if there is no reply.
                do
                {
                    if (QueryCancellationPending)
                        break;

                    MasterServerRetries += 1;
                    udpSocket.SendTo(query, masterEndPoint);
                    try
                    {
                        //if (MasterServerFound)
                        //    break;

                        received = udpSocket.ReceiveFrom(buffer, ref masterEndPoint);
                        timeoutOccured = false;
                        MasterEndPoint = masterEndPoint;
                        break;
                    }
                    catch (Exception ex)
                    {
                        GlobalsLib.Current.Logger.Error(ex);
                        // Timeout occured
                        timeoutOccured = true;

                        //if (!MasterServersAttemptList.Contains(string.Format("{0}:{1}", masterServer, port)))
                        //    MasterServersAttemptList.Add(string.Format("{0}:{1}", masterServer, port));

                        ////retry
                        ////GetGameServerList(parameters);
                        //GetGameServerList(parameters, MasterServer, MasterServerPort, MasterSeedIp);
                    }

                } while ((!QueryCancellationPending && MasterServerRetries < maxRetries));

                if (!QueryCancellationPending)
                {
                    // Once we have gotten this far, we KNOW that the server is online,
                    // so lets increate the maxRetries.
                    maxRetries = 20;
                    // If timeoutOccured is True at this point, we know that we where unable
                    // to get a reply and had to give up.

                    if (!timeoutOccured)
                    {
                        // Parse the reply.
                        gameServers = ParseReplyBuffer(buffer, received);

                        MasterSeedIp = string.Format("{0}:{1}", gameServers.Last().Address, gameServers.Last().Port);

                        if (IsNullAddress(gameServers[gameServers.Count - 1]))
                        {
                            // We have reached the end of the IP list.

                            // Remove the dummy address that denotes the end of the list.
                            gameServers.RemoveAt(gameServers.Count - 1);
                            endOfQuery = true;
                        }
                        else
                        {
                            // We have not yet reached the end of the IP list.
                            // We need to re-query the server to receive more of the list.
                            query = BuildQuery(region, filter, map, gameMod,
                                gameServers[gameServers.Count - 1].ToString());
                        }

                        // Keep track of how many servers we've enqueued for querying.
                        totalServers += gameServers.Count;

                        // Invoke the callback to notify the caller of new IPs.
                        if (game == QueryGame.HalfLife)
                        {
                            OnInvokeQueryRawCallBack(callBack,
                                new QueryRawAsyncEventArgs(gameServers.ToArray(), GameType.HalfLife));
                        }
                        else if (game == QueryGame.Arma)
                        {
                            OnInvokeQueryRawCallBack(callBack,
                                new QueryRawAsyncEventArgs(gameServers.ToArray(), GameType.Arma));
                        }
                        else
                        {
                            OnInvokeQueryRawCallBack(callBack,
                                new QueryRawAsyncEventArgs(gameServers.ToArray(), GameType.CounterStrikeSource));
                        }
                    }
                }

            } while ((!endOfQuery && !QueryCancellationPending));

            //return (IPEndPoint) masterEndPoint;
            
            udpSocket.Close();

            if (totalServers == 0)
            {
                OnQueryAsyncCompleted();
            }
        }

        /// <summary>
        /// Refactored to work with testing master server
        /// This method randomly selects one of the IPs that the master servers hostname has been resolved too.
        /// </summary>
        /// <param name="masterServer"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        private IPEndPoint GetGameServerList(string masterServer, int port)
        {
            IPAddress[] masterIPs = null;
            IPEndPoint masterEndPoint = null;
            var rand = new Random();

            masterIPs = ResolveHostname(masterServer);
            masterEndPoint = new IPEndPoint(masterIPs[rand.Next(0, masterIPs.Length)], port);

            return masterEndPoint;
        }
        /// <summary>
        /// OLD METHOD - does NOT test master server and port...
        /// This method randomly selects one of the IPs that the master servers hostname has been resolved too.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        private IPEndPoint GetGameServerList(QueryGame game)
        {
            IPAddress[] masterIPs = null;
            IPEndPoint masterEndPoint = null;
            var rand = new Random();

            switch (game)
            {
                case QueryGame.Arma:
                    masterIPs = ResolveHostname("hl2master.steampowered.com");

                    masterEndPoint = new IPEndPoint(masterIPs[rand.Next(0, masterIPs.Length)], 27011);
                    break;
                case QueryGame.HalfLife:
                    masterIPs = ResolveHostname("hl1master.steampowered.com");

                    masterEndPoint = new IPEndPoint(masterIPs[rand.Next(0, masterIPs.Length)], 27010);
                    break;
                case QueryGame.Source:
                    masterIPs = ResolveHostname("hl2master.steampowered.com");

                    masterEndPoint = new IPEndPoint(masterIPs[rand.Next(0, masterIPs.Length)], 27011);
                    break;
            }

            return masterEndPoint;
        }

        // This method resolves the given hostname.
        private IPAddress[] ResolveHostname(string host)
        {
            IPHostEntry entry = Dns.GetHostEntry(host);
            return entry.AddressList;
        }

        private void OnInvokeQueryRawCallBack(QueryRawCallBack callback, QueryRawAsyncEventArgs e)
        {
            if (syncObject == null)
            {
                callback(this, e);
            }
            else
            {
                syncObject.Invoke(callback, new object[]
                {
                    this,
                    e
                });
            }
        }

        private void OnInvokeQueryCallBack(QueryCallBack callback, QueryAsyncEventArgs e)
        {
            if (syncObject == null)
            {
                callback(this, e);
            }
            else
            {
                syncObject.Invoke(callback, new object[]
                {
                    this,
                    e
                });
            }
        }

        private void OnQueryAsyncCompleted()
        {
            if (syncObject == null || !syncObject.InvokeRequired)
            {
                if (QueryAsyncCompleted != null)
                {
                    QueryAsyncCompleted(this, EventArgs.Empty);
                }
            }
            else
            {
                syncObject.Invoke(new OnQueryAsyncCompletedDelegate(OnQueryAsyncCompleted), null);
            }
        }

        // This method determines if an address consists of only 0's.
        // Such an address denotes the end of the IP list obtained by the master server.
        private bool IsNullAddress(IPEndPoint endpoint)
        {
            byte[] addressBytes = endpoint.Address.GetAddressBytes();
            return addressBytes[0] == 0 && addressBytes[1] == 0 && addressBytes[2] == 0 && addressBytes[3] == 0 &&
                   endpoint.Port == 0;
        }

        // This method takes two bytes and returns them as an uint16.
        private UInt16 GetUInt16NetworkOrder(byte byte1, byte byte2)
        {
            // Convert the first byte to a uint16.
            UInt16 value = Convert.ToUInt16(byte1);

            // Shift it 8 bits to the left.
            value <<= 8;

            // Use bitwise OR to "place" the value of byte2 on the lower 8 bits of 'value'.
            value = (ushort) (value | byte2);

            return value;
        }

        // This method builds a query using the given parameters.
        private byte[] BuildQuery(QueryRegionCode region, QueryFilter filter, string map, string gameMod, string ipSeed)
        {
            var debugQuery = new StringBuilder();
            var queryBuilder = new List<byte>();

            // Master server queries always begin with 49 (the character 1).
            queryBuilder.Add(49);
            debugQuery.Append("1");

            // Then follows the region code
            queryBuilder.Add(Convert.ToByte(region));
            debugQuery.Append(region);

            // Then follows the IP and port seed, which initially will be 0.0.0.0:0
            queryBuilder.AddRange(Encoding.UTF8.GetBytes(ipSeed));
            debugQuery.Append(ipSeed);

            // A null-byte to terminate the previous string.
            queryBuilder.Add(0);
            debugQuery.Append("\\0");

            // And then follows the filter string:
            queryBuilder.AddRange(BuildFilter(filter));
            debugQuery.Append(filter);

            // Add the map and/or gamemod filters, if any.
            if (map != string.Empty)
            {
                queryBuilder.AddRange(Encoding.UTF8.GetBytes(string.Format("\\map\\{0}", map)));
                debugQuery.AppendFormat("\\map\\{0}", map);
            }
            if (gameMod != string.Empty)
            {
                queryBuilder.AddRange(Encoding.UTF8.GetBytes(string.Format("\\gamedir\\{0}", gameMod)));
                debugQuery.AppendFormat("\\gamedir\\{0}", gameMod);
            }

            // This line will make sure we wont get any Left4Dead servers in our reply.
            // If Left4Dead servers should be returned from the query, just remove this line.
            queryBuilder.AddRange(Encoding.UTF8.GetBytes("\\napp\\500"));
            debugQuery.Append("\\napp\\500");

            // And finally, terminate the previous string with a null-byte.
            queryBuilder.Add(0);
            debugQuery.Append("\\0");

            return queryBuilder.ToArray();
        }

        // This method builds the filter to be used for the query, based on the given QueryFilter argument.
        private byte[] BuildFilter(QueryFilter filter)
        {
            var filterBuilder = new List<byte>();

            if ((filter & QueryFilter.Dedicated) > 0)
            {
                filterBuilder.AddRange(Encoding.UTF8.GetBytes("\\type\\d"));
            }
            if ((filter & QueryFilter.AntiCheat) > 0)
            {
                filterBuilder.AddRange(Encoding.UTF8.GetBytes("\\secure\\1"));
            }
            if ((filter & QueryFilter.Linux) > 0)
            {
                filterBuilder.AddRange(Encoding.UTF8.GetBytes("\\linux\\1"));
            }
            if ((filter & QueryFilter.NotEmpty) > 0)
            {
                filterBuilder.AddRange(Encoding.UTF8.GetBytes("\\empty\\1"));
            }
            if ((filter & QueryFilter.NotFull) > 0)
            {
                filterBuilder.AddRange(Encoding.UTF8.GetBytes("\\full\\1"));
            }
            if ((filter & QueryFilter.SpectatorProxies) > 0)
            {
                filterBuilder.AddRange(Encoding.UTF8.GetBytes("\\proxy\\1"));
            }
            if ((filter & QueryFilter.Empty) > 0)
            {
                filterBuilder.AddRange(Encoding.UTF8.GetBytes("\\noplayers\\1"));
            }
            if ((filter & QueryFilter.WhiteListed) > 0)
            {
                filterBuilder.AddRange(Encoding.UTF8.GetBytes("\\white\\1"));
            }

            return filterBuilder.ToArray();
        }

        public class NoReplyException : Exception
        {
            public NoReplyException(string message)
                : base(message)
            {
            }
        }

        private delegate void OnQueryAsyncCompletedDelegate();

        // The SynchronizingObject is used when a callback is called.
        // It invokes the callback on the thread that created the SynchronizingObject.
        // The most common use is just to set this to the Form that will handle the callbacks.

        public class QueryAsyncEventArgs : EventArgs
        {
            private readonly GameServer server;

            private bool endOfReply;

            public QueryAsyncEventArgs(GameServer server)
            {
                this.server = server;
            }

            public GameServer GameServer
            {
                get { return server; }
            }
        }

        public class QueryRawAsyncEventArgs : EventArgs
        {
            private readonly IPEndPoint[] servers;

            private readonly GameType typeOfGame;

            public QueryRawAsyncEventArgs(IPEndPoint[] servers, GameType typeOfGame)
            {
                this.servers = servers;
                this.typeOfGame = typeOfGame;
            }

            public IPEndPoint[] GameServers
            {
                get { return servers; }
            }

            public GameType Type
            {
                get { return typeOfGame; }
            }
        }
    }
}

