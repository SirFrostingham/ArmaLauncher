using System;
using Microsoft.VisualBasic;

namespace SteamLib.Protocols
{
    internal class Source : Protocol
    {
        // FF FF FF FF 57

        private const string _QUERY_GETCHALLANGE = "ÿÿÿÿW";
        // FF FF FF FF 54 53 6F 75 72 63 65 20 45 6E 67 69
        // 6E 65 20 51 75 65 72 79 00       
        // Changed this from just ÿÿÿÿT.
        private string _QUERY_DETAILS = "ÿÿÿÿTSource Engine Query" + Strings.Chr(0);

        // FF FF FF FF 56

        private const string _QUERY_RULES = "ÿÿÿÿV";
        // FF FF FF FF 55

        private const string _QUERY_PLAYERS = "ÿÿÿÿU";
        // This value is used to determine if a reply is an "S2C_CHALLENGE", ie if it contains
        // a new challenge number for us to use.

        private const byte _S2C_CHALLENGE_NUMBER = 65;
        // This value is used to determine if a reply is a reply to a A2S_RULES query.

        private const byte _A2S_RULES_NUMBER = 69;
        // This value is used to determine if a reply is a reply to a A2S_PLAYER query.

        private const byte _A2S_PLAYER_NUMBER = 68;

        private string _CHALLANGE;
        /// Serverhost address
        /// Serverport
        public Source(string host, int port)
        {
            base._protocol = GameProtocol.Source;
            Connect(host, port);
        }

        /// 
        /// Querys the serverinfos
        /// 

        public override void GetServerInfo()
        {
                if (Query(_QUERY_DETAILS))
                {
                    ParseDetails();
                }
                else
                {
                    return;
                }

                if (Query(_QUERY_PLAYERS + Strings.Chr(0) + Strings.Chr(0) + Strings.Chr(0) + Strings.Chr(0)))
                {
                    if (Response.Length > 4)
                    {
                        // As long as the response is a challenge-number response,
                        // We need to re-send the query with the new challenge string.
                        while (Response[4] == _S2C_CHALLENGE_NUMBER)
                        {
                            GetChallenge();
                            Query(_QUERY_PLAYERS + _CHALLANGE);
                        }
                        ParsePlayers();
                    }
                }

            if (Query(_QUERY_RULES + Strings.Chr(0) + Strings.Chr(0) + Strings.Chr(0) + Strings.Chr(0)))
            {

                if (Response.Length > 4)
                {
                    // As long as the response is a challenge-number response,
                    // We need to re-send the query with the new challenge string.
                    while (Response[4] == _S2C_CHALLENGE_NUMBER)
                    {
                        GetChallenge();
                        Query(_QUERY_RULES + _CHALLANGE);
                    }
                    ParseRules();
                }
            }
        }

        private void GetChallenge()
        {
            // Calling ToString on a byte array will only return
            // System.Byte[], which is of no use for us.
            // _CHALLANGE = Response.ToString()
            //
            // Instead, we should just keep the challenge number,
            // which is a 4 byte integer.
            _CHALLANGE = System.Text.Encoding.Default.GetString(Response, 5, 4);
        }

        private void ParseDetails()
        {
            _params["protocolver"] = Response[5].ToString();
            if (Response[5] == 0)
            {
                return;
            }
            _params["hostname"] = ReadNextParam(6);
            _params["mapname"] = ReadNextParam();
            _params["mod"] = ReadNextParam();
            _params["modname"] = ReadNextParam();

            // The field that denotes the number of players on the server is not necessarily always on this index (Response.Length - 7), therefor the variable i is not accurate.
            // The next field in the response now is the AppID field (2 byte long), which is a unique ID for all Steam applications.
            // I'm not sure you want to include it or not, but for now I will.
            //Dim i As Integer = Response.Length - 7
            int location = base.Offset;
            _params["appid"] = (Response[base.Offset] | (Convert.ToInt32(Response[System.Threading.Interlocked.Increment(ref location)] << 8))).ToString();
            // Perform binary OR to get the short value from the two bytes.
            int offset = base.Offset;
            _params["players"] = Response[System.Threading.Interlocked.Increment(ref offset)].ToString();
            int i = base.Offset;
            _params["maxplayers"] = Response[System.Threading.Interlocked.Increment(ref i)].ToString();
            int location1 = base.Offset;
            _params["botcount"] = Response[System.Threading.Interlocked.Increment(ref location1)].ToString();
            // Protocol version 15 seems to end here. So lets not go any further if thats the protocol of this reply.
            if (Response[5] != 15)
            {
                int offset1 = base.Offset;
                _params["servertype"] = Convert.ToChar(Response[System.Threading.Interlocked.Increment(ref offset1)]).ToString();
                int i1 = base.Offset;
                _params["serveros"] = Convert.ToChar(Response[System.Threading.Interlocked.Increment(ref i1)]).ToString();
                int location2 = base.Offset;
                _params["passworded"] = Response[System.Threading.Interlocked.Increment(ref location2)].ToString();
                int offset2 = base.Offset;
                _params["secured"] = Response[System.Threading.Interlocked.Increment(ref offset2)].ToString();

                base.Offset += 1;
                //Increment the offset to take the last read byte into account.

                _params["gameversion"] = ReadNextParam();
                // In most cases, the reply will end here. But some servers include an Extra Data Flag along
                // with some extra data, so if we'll read that too, if available.

                if (base.Offset < Response.Length)
                {
                    byte flag = Response[base.Offset];
                    base.Offset += 1;
                    //  	The server's game port # is included
                    if ((flag & 0x80) > 0)
                    {
                        _params["serverport"] = Convert.ToString(BitConverter.ToInt16(Response, base.Offset));
                        base.Offset += 2;
                    }


                    // The spectator port # and then the spectator server name are included
                    if ((flag & 0x40) > 0)
                    {
                        _params["spectatorport"] = Convert.ToString(BitConverter.ToInt16(Response, base.Offset));
                        base.Offset += 2;
                        _params["spectatorname"] = ReadNextParam();
                    }

                    // The game tag data string for the server is included [future use]
                    if ((flag & 0x20) > 0)
                    {
                        _params["gametagdata"] = ReadNextParam();
                    }
                }

            }
        }

        private void ParseRules()
        {
            string key = null;
            string val = null;
            int ruleCount = BitConverter.ToInt16(Response, 5);
            base.Offset = 7;

            for (int i = 0; i <= ruleCount - 1; i++)
            {
                key = ReadNextParam();
                val = ReadNextParam();
                if (key.Length == 0)
                {
                    continue;
                }
                _params[key] = val;
            }
        }

        private void ParsePlayers()
        {
            if (Response.Length > 4)
            {
                // 68 = 'D'
                if (Response[4] != 68)
                {
                    return;
                }
                byte numPlayers = Response[5];
                _params["numplayers"] = numPlayers.ToString();
                base.Offset = 6;

                int pNr = 0;
                // The number of players reported as playing on the server (given by the variable numPlayers)
                // apparently isnt necessarily the same as the number of players reported in the response.
                // Thats why a For-loop wont work here, instead we'll have to use a While-loop.
                while (pNr < numPlayers - 2 && base.Offset < Response.Length)
                {
                    pNr = _players.Add(new Player());
                    _players[pNr].Parameters.Add("playernr", Response[base.Offset].ToString());
                    // Removed the Math.Max here. It shouldnt be needed.
                    base.Offset += 1;
                    //Increment the offset AFTER getting the playernr, not before.
                    _players[pNr].Name = ReadNextParam();
                    _players[pNr].Score = BitConverter.ToInt32(Response, Offset);
                    base.Offset += 4;
                    _players[pNr].Time = new TimeSpan(0, 0, Convert.ToInt32(BitConverter.ToSingle(Response, Offset)));
                    base.Offset += 4;
                }
            }
        }
    }
}


