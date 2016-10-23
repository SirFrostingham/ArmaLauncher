using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using NLog;

namespace SteamLib
{
    internal abstract class Protocol
    {
        // This integer constant represents an integer with the top bit set. It is needed when
        // Checking of a Source server is using BZip2 compression

        private const int _INT_TOPBIT = (int) -2147483648L;
        private Socket _serverConnection;
        private IPEndPoint _remoteIpEndPoint;
        private byte[] _sendBuffer;
        private byte[] _readBuffer;
        private int _timeout = 5000;
        private int _offset;
        private DateTime _scanTime;

        private bool _debugMode;
        protected string _requestString = "";
        protected string _responseString = "";
        protected bool _isOnline = true;
        protected int _packages;
        protected GameProtocol _protocol;
        protected PlayerCollection _players;
        protected StringDictionary _params;

        protected StringCollection _teams;
        public Protocol()
        {
            _players = new PlayerCollection();
            _params = new StringDictionary();
            _teams = new StringCollection();
        }


        protected void Connect(string host, int port)
        {
            _serverConnection = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _serverConnection.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, _timeout);

            IPAddress ip = default(IPAddress);
            try
            {
                ip = IPAddress.Parse(host);
            }
            catch (System.FormatException generatedExceptionName)
            {
                GlobalsLib.Current.Logger.Error(generatedExceptionName);
                ip = Dns.GetHostEntry(host).AddressList[0];
            }
            _remoteIpEndPoint = new IPEndPoint(ip, port);
        }

        protected bool Query(string request)
        {
            _readBuffer = new byte[100 * 1024];
            // 100kb should be enough
            EndPoint _remoteEndPoint = (EndPoint)_remoteIpEndPoint;
            _packages = 0;
            int read = 0;
            int bufferOffset = 0;

            // Request
            _sendBuffer = System.Text.Encoding.Default.GetBytes(request);

            _serverConnection.SendTo(_sendBuffer, _remoteIpEndPoint);

            // Response
            do
            {
                read = 0;
                try
                {
                    // Multipackage check
                    if (_packages > 0)
                    {
                        switch (_protocol)
                        {
                            case GameProtocol.Arma:
                            case GameProtocol.Source:
                            case GameProtocol.HalfLife:
                                // This is a multi-package response only if the initial response includes a special header,
                                // starting with the integer -2.
                                if (BitConverter.ToInt32(_readBuffer, 0) == -2)
                                {
                                    // We need to add all packets to a sorted list, sorted by their index.
                                    // This is because we can never be certain what order the packets arrive in.
                                    SortedList<int, byte[]> packets = new SortedList<int, byte[]>();
                                    List<byte> packetData = new List<byte>();

                                    // There are some values in this special "split-packet" header that we'll need to hold onto before moving along.
                                    int requestID = BitConverter.ToInt32(_readBuffer, 4);
                                    bool usesBzip2 = (_INT_TOPBIT & requestID) == _INT_TOPBIT;
                                    // Determine if the top bit is set in the request ID, which denotes that BZip2 compression is used.
                                    byte numPackets = _readBuffer[8];
                                    int splitSize = 0;
                                    int crcChecksum = 0;
                                    int headerSize = 0;


                                    // The source protocol includes an extra byte here that'll always contain 0,
                                    // which we'd want to ignore, but only if the protocol in use is Source.
                                    if (_protocol == GameProtocol.Source)
                                    {
                                        headerSize = 18;
                                        splitSize = BitConverter.ToInt32(_readBuffer, 10);
                                        if (usesBzip2)
                                        {
                                            crcChecksum = BitConverter.ToInt32(_readBuffer, 14);
                                        }
                                    }
                                    else
                                    {
                                        headerSize = 9;
                                        if (usesBzip2)
                                        {
                                            crcChecksum = BitConverter.ToInt32(_readBuffer, 11);
                                            headerSize += 2;
                                        }
                                    }

                                    // Add the first packet (that was sent along with the special "split-packet" header) to the list of bytes.
                                    for (int j = headerSize; j <= bufferOffset - 1; j++)
                                    {
                                        packetData.Add(_readBuffer[j]);
                                    }

                                    // Now, add this to the sorted list, and pass the packet number as the key.
                                    // The packet index is placed differently on GoldSource servers than on
                                    // Source server, hence this IF-statement.
                                    if (_protocol == GameProtocol.Source)
                                    {
                                        packets.Add(_readBuffer[9], packetData.ToArray());
                                    }
                                    else
                                    {
                                        packets.Add(_readBuffer[8] >> 4, packetData.ToArray());
                                        // The upper 4 bits represent the packet index.
                                    }

                                    packetData.Clear();

                                    // Read the next packets.
                                    for (int i = 0; i <= numPackets - 2; i++)
                                    {
                                        read = _serverConnection.ReceiveFrom(_readBuffer, ref _remoteEndPoint);
                                        // Subtract headerSize by 4 here because the 4 bytes used for CRC is only included in the first header.
                                        for (int j = headerSize - 8; j <= read; j++)
                                        {
                                            packetData.Add(_readBuffer[j]);
                                        }
                                        if (_protocol == GameProtocol.Source)
                                        {
                                            packets.Add(_readBuffer[9], packetData.ToArray());
                                        }
                                        else
                                        {
                                            try
                                            {
                                                packets.Add(_readBuffer[8] >> 4, packetData.ToArray());
                                                // The upper 4 bits represent the packet index.
                                            }
                                            catch (ArgumentException ex)
                                            {
                                                GlobalsLib.Current.Logger.Error(ex);
                                                //retry key + 1
                                                packets.Add(packets.Count + 1, packetData.ToArray());
                                                // The upper 4 bits represent the packet index.
                                            }
                                        }
                                        packetData.Clear();
                                    }

                                    // Once we have gotten this far, the SortedList "packet", should
                                    // Contain all packets in sorted order, now we just need to append them in that order.
                                    for (int i = 0; i <= packets.Count - 1; i++)
                                    {
                                        packetData.AddRange(packets.Values[i]);
                                    }


                                    if (usesBzip2)
                                    {
                                        System.IO.MemoryStream inStream = new System.IO.MemoryStream(packetData.ToArray(), 0, packetData.Count);
                                        System.IO.MemoryStream outStream = new System.IO.MemoryStream();
                                        System.IO.MemoryStream crcStream = default(System.IO.MemoryStream);
                                        CRC32 crc = new CRC32();

                                        ICSharpCode.SharpZipLib.BZip2.BZip2.Decompress(inStream, outStream);

                                        bufferOffset = splitSize;
                                        crcStream = new System.IO.MemoryStream(outStream.GetBuffer(), 0, bufferOffset, true, true);
                                        //outStream.Capacity = bufferOffset
                                        //outStream.Position = 0

                                        _readBuffer = crcStream.GetBuffer();

                                        read = 0;
                                        if (crcChecksum != crc.GetCrc32(crcStream))
                                        {
                                            return false;
                                        }

                                        inStream.Close();
                                        outStream.Close();
                                    }
                                    else
                                    {
                                        _readBuffer = packetData.ToArray();
                                        bufferOffset = _readBuffer.Length;
                                        read = 0;
                                    }
                                }
                                break;
                        }
                    }
                    else
                    {
                        // first package
                        read = _serverConnection.ReceiveFrom(_readBuffer, ref _remoteEndPoint);
                    }
                    bufferOffset += read;
                    _packages += 1;
                }
                catch (System.Net.Sockets.SocketException generatedExceptionName)
                {
                    GlobalsLib.Current.Logger.Error(generatedExceptionName);
                    _isOnline = false;
                    return false;
                }
            } while (read > 0);

            _scanTime = DateTime.Now;

            if (bufferOffset > 0 && bufferOffset != _readBuffer.Length)
            {
                byte[] temp = new byte[bufferOffset];
                for (int i = 0; i <= temp.Length - 1; i++)
                {
                    temp[i] = _readBuffer[i];
                }
                _readBuffer = temp;
                temp = null;
            }
            _responseString = System.Text.Encoding.Default.GetString(_readBuffer);

            if (_debugMode)
            {
                System.IO.FileStream stream = System.IO.File.OpenWrite("LastQuery.dat");
                stream.Write(_readBuffer, 0, _readBuffer.Length);
                stream.Close();
            }
            return true;
        }

        protected void AddParams(string[] parts)
        {
            if (!IsOnline)
            {
                return;
            }
            string key = null;
            string val = null;
            for (int i = 0; i <= parts.Length - 1; i++)
            {
                if (parts[i].Length == 0)
                {
                    continue;
                }
                key = parts[System.Math.Max(System.Threading.Interlocked.Increment(ref i), i - 1)];
                val = parts[i];

                if (key == "final")
                {
                    break; // TODO: might not be correct. Was : Exit For
                }
                if (key == "querid")
                {
                    continue;
                }

                _params[key] = val;
            }
        }

        protected byte[] Response
        {
            get { return _readBuffer; }
        }

        protected string ResponseString
        {
            get { return _responseString; }
        }

        protected int Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        protected string ReadNextParam(int offset)
        {
            if (offset > _readBuffer.Length)
            {
                throw new IndexOutOfRangeException();
            }
            _offset = offset;
            return ReadNextParam();
        }

        protected string ReadNextParam()
        {
            string temp = "";
            while (_offset < _readBuffer.Length)
            {
                if (_readBuffer[_offset] == 0)
                {
                    _offset += 1;
                    break; // TODO: might not be correct. Was : Exit While
                }
                temp += Convert.ToChar(_readBuffer[_offset]);
                _offset += 1;
            }
            return temp;
        }



        /// <summary>
        /// Gets or sets the connection timeout
        /// </summary>
        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }

        /// <summary>
        /// Gets the parsed parameters
        /// </summary>
        public StringDictionary Parameters
        {
            get { return _params; }
        }

        /// <summary>
        /// Gets the team names, not always set
        /// </summary>
        public StringCollection Teams
        {
            get { return _teams; }
        }

        /// <summary>
        /// Gets the players on the server
        /// </summary>
        public PlayerCollection Players
        {
            get { return _players; }
        }

        /// <summary>
        /// Gets the number of players on the server
        /// </summary>
        public int NumPlayers
        {
            get { return _players.Count; }
        }

        /// <summary>
        /// Gets the time of the last scan
        /// </summary>
        public DateTime ScanTime
        {
            get { return _scanTime; }
        }

        /// <summary>
        /// Enables the debugging mode
        /// </summary>
        public bool DebugMode
        {
            get { return _debugMode; }
            set { _debugMode = value; }
        }



        /// <summary>
        /// Querys the server info
        /// </summary>
        public abstract void GetServerInfo();

        /// <summary>
        /// Gets the server name
        /// </summary>
        public virtual string Name
        {
            get
            {
                if (!_isOnline)
                {
                    return null;
                }
                return _params["hostname"];
            }
        }

        /// <summary>
        /// Determines the mod
        /// </summary>
        public virtual string Mod
        {
            get
            {
                if (!_isOnline)
                {
                    return null;
                }
                return _params["modname"];
            }
        }

        /// <summary>
        /// Determines the mapname
        /// </summary>
        public virtual string Map
        {
            get
            {
                if (!_isOnline)
                {
                    return null;
                }
                return _params["mapname"];
            }
        }

        /// <summary>
        /// Determines if the server is password protected
        /// </summary>
        public virtual bool Passworded
        {
            get
            {
                if (_params.ContainsKey("passworded") && (_params["passworded"] != "0"))
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Determines if the server is online
        /// </summary>
        public virtual bool IsOnline
        {
            get { return _isOnline; }
        }

        /// <summary>
        /// Gets the max players 
        /// TODO:areed Fix maxplayers
        /// </summary>
        public virtual int MaxPlayers
        {
            get { return _params["maxplayers"] != null ? Int16.Parse(_params["maxplayers"]) : 0; }
        }

    }
}


