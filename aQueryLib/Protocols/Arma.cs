using System;
using Microsoft.VisualBasic;

namespace SteamLib.Protocols
{
    internal class Arma : Protocol
    {

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

        /// <param name="host">Serverhost address</param>
        /// <param name="port">Serverport</param>
        public Arma(string host, int port)
        {
            base._protocol = GameProtocol.Arma;
            Connect(host, port);
        }

        /// <summary>
        /// Querys the serverinfos
        /// </summary>
        public override void GetServerInfo()
        {
                //If Not IsOnline Then
                //    Exit Sub
                //End If

                if (Query(_QUERY_DETAILS))
                {
                    ParseDetails();
                }
                else
                {
                    return;
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

                if (Query(_QUERY_PLAYERS + Strings.Chr(0) + Strings.Chr(0) + Strings.Chr(0) + Strings.Chr(0)))
                {

                    if (Response.Length > 4)
                    {
                        while (Response[4] == _S2C_CHALLENGE_NUMBER)
                        {
                            GetChallenge();
                            Query(_QUERY_PLAYERS + _CHALLANGE);
                        }
                        ParsePlayers();
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

        /// <summary>
        /// Gets the active modification
        /// </summary>
        public override string Mod
        {
            get { return _params["mod"]; }
        }


        private void ParseDetails()
        {
            if (Response.Length > 4)
            {
                Offset = 6;
                // Goldsource servers can respond either in the "new" way (that is, in the same manner as Source servers),
                // or in the "old" way. Before going any furter we need to find out
                // what type of response we have got.
                // If the byte on index 5 is equal to 'I' (0x49), the reply follows the new protocol standard,
                // but if the byte equals 'm' (0x6D), it does not.
                if (Response[4] == 73)
                {
                    // Reply follows new protocol standard.
                    this.ParseDetailsSource();
                }
                else
                {
                    // Reply follows old protocol standard.

                    string nextParam = ReadNextParam();
                    base.Offset = 6;
                    int indexColon = nextParam.IndexOf(':');

                    if (Response[5] == 0)
                    {
                        // Undocumented protocol version.
                        // Unsupported for now.
                        _isOnline = false;
                        return;
                    }
                    else
                    {
                        if (indexColon == -1)
                        {
                            // String contained no colon, so it cant have been
                            // An ip address with port number.
                            this.ParseDetailsSource();
                        }
                        else
                        {
                            this.ParseDetailsGoldSource();
                        }
                    }
                }
            }
        }

        private void ParseDetailsSource()
        {
            /* EXAMPLE RESPONSE:
             ÿÿÿÿI!Wasteland v0.9g Full saving! |moosemilker.com\0\0Arma3\0Arma 3\0\0\0\0(\0dw\0\01.28.126958\0±þ\b\bhNuû@bt,r128,n0,s1,i0,mf,lf,vt,dt,g65545,c4194303-4194303,pw,\0’£\0\0\0\0\0
             */

            /* PARAMETERS DEFINITION:
             https://community.bistudio.com/wiki/STEAMWORKSquery
             */

            _params["protocolver"] = Response[5].ToString();
            _params["hostname"] = ReadNextParam(6);
            _params["mapname"] = ReadNextParam();
            _params["mod"] = ReadNextParam();
            _params["modname"] = ReadNextParam();

            //todo: parse right stuff for different game types
            if (Mod == "arma2arrowpc")
            {
                // The field that denotes the number of players on the server is not necessarily always on this index (Response.Length - 7), therefor the variable i is not accurate.
                // The next field in the response now is the AppID field (2 byte long), which is a unique ID for all Steam applications.
                // I'm not sure you want to include it or not, but for now I will.
                //Dim i As Integer = Response.Length - 7
                int location = base.Offset;
                _params["appid"] = (Response[base.Offset] | (Convert.ToInt32(Response[System.Threading.Interlocked.Increment(ref location)] << 8))).ToString();
                // Perform binary OR to get the short value from the two bytes.
                int offset = base.Offset;
                _params["players"] = Response[System.Threading.Interlocked.Increment(ref offset)].ToString();
                int i = base.Offset+2;
                _params["maxplayers"] = Response[System.Threading.Interlocked.Increment(ref i)].ToString(); //TODO:areed Fix maxplayers
                int location1 = base.Offset;
                _params["botcount"] = Response[System.Threading.Interlocked.Increment(ref location1)].ToString();
                int offset1 = base.Offset;
                _params["servertype"] = Strings.ChrW(Response[System.Threading.Interlocked.Increment(ref offset1)]).ToString();
                int i1 = base.Offset;
                _params["serveros"] = Strings.ChrW(Response[System.Threading.Interlocked.Increment(ref i1)]).ToString();
                int location2 = base.Offset + 6;
                _params["passworded"] = Response[System.Threading.Interlocked.Increment(ref location2)].ToString();
                int offset2 = base.Offset;
                _params["secureserver"] = Response[System.Threading.Interlocked.Increment(ref offset2)].ToString();

                base.Offset += 1;
                //Increment the offset to take the last read byte into account.

                _params["gameversion"] = ReadNextParam();
                // In most cases, the reply will end here. But some servers include an Extra Data Flag along
                // with some extra data, so if we'll read that too, if available.

                if (base.Offset < Response.Length)
                {
                    byte flag = Response[base.Offset + 20];
                    //base.Offset += 1;
                    base.Offset += 20;
                    //  	The server's game port # is included
                    //if ((flag & 0x80) > 0)
                    //{
                        _params["serverport"] = Convert.ToString(BitConverter.ToInt16(Response, base.Offset-1));
                        base.Offset += 2;
                    //}


                    // The spectator port # and then the spectator server name are included
                    //if ((flag & 0x40) > 0)
                    //{
                        _params["spectatorport"] = Convert.ToString(BitConverter.ToInt16(Response, base.Offset));
                        base.Offset += 2;
                        _params["spectatorname"] = ReadNextParam();
                    //}

                    // The game tag data string for the server is included [future use]
                    //if ((flag & 0x20) > 0)
                    //{
                        _params["gametagdata"] = ReadNextParam();
                    //}
                }
            }
            else if (Mod == "Arma3")
            {
                // The field that denotes the number of players on the server is not necessarily always on this index (Response.Length - 7), therefor the variable i is not accurate.
                // The next field in the response now is the AppID field (2 byte long), which is a unique ID for all Steam applications.
                // I'm not sure you want to include it or not, but for now I will.
                //Dim i As Integer = Response.Length - 7
                int location = base.Offset;
                _params["appid"] = (Response[base.Offset] | (Convert.ToInt32(Response[System.Threading.Interlocked.Increment(ref location)] << 8))).ToString();
                // Perform binary OR to get the short value from the two bytes.
                int offset = base.Offset;
                _params["players"] = Response[System.Threading.Interlocked.Increment(ref offset)].ToString();
                int i = base.Offset+2;
                _params["maxplayers"] = Response[System.Threading.Interlocked.Increment(ref i)].ToString(); //TODO:areed Fix maxplayers
                int location1 = base.Offset;
                _params["botcount"] = Response[System.Threading.Interlocked.Increment(ref location1)].ToString();
                int offset1 = base.Offset;
                _params["servertype"] = Strings.ChrW(Response[System.Threading.Interlocked.Increment(ref offset1)]).ToString();
                int i1 = base.Offset;
                _params["serveros"] = Strings.ChrW(Response[System.Threading.Interlocked.Increment(ref i1)]).ToString();
                int location2 = base.Offset + 6;
                _params["passworded"] = Response[System.Threading.Interlocked.Increment(ref location2)].ToString();
                int offset2 = base.Offset;
                _params["secureserver"] = Response[System.Threading.Interlocked.Increment(ref offset2)].ToString();

                base.Offset += 1;
                //Increment the offset to take the last read byte into account.

                _params["gameversion"] = ReadNextParam();
                // In most cases, the reply will end here. But some servers include an Extra Data Flag along
                // with some extra data, so if we'll read that too, if available.

                if (base.Offset < Response.Length)
                {
                    byte flag = Response[base.Offset+20];
                    //base.Offset += 1;
                    base.Offset += 20;
                    //  	The server's game port # is included
                    //if ((flag & 0x80) > 0)
                    //{
                        _params["serverport"] = Convert.ToString(BitConverter.ToInt16(Response, base.Offset));
                        base.Offset += 2;
                    //}


                    // The spectator port # and then the spectator server name are included
                    //if ((flag & 0x40) > 0)
                    //{
                        _params["spectatorport"] = Convert.ToString(BitConverter.ToInt16(Response, base.Offset));
                        base.Offset += 2;
                        _params["spectatorname"] = ReadNextParam();
                    //}

                    // The game tag data string for the server is included [future use]
                    //if ((flag & 0x20) > 0)
                    //{
                        _params["gametagdata"] = ReadNextParam();
                    //}
                }
            }
            else
            {
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
                _params["maxplayers"] = Response[System.Threading.Interlocked.Increment(ref i)].ToString(); //TODO:areed Fix maxplayers
                int location1 = base.Offset;
                _params["botcount"] = Response[System.Threading.Interlocked.Increment(ref location1)].ToString();
                int offset1 = base.Offset;
                _params["servertype"] = Strings.ChrW(Response[System.Threading.Interlocked.Increment(ref offset1)]).ToString();
                int i1 = base.Offset;
                _params["serveros"] = Strings.ChrW(Response[System.Threading.Interlocked.Increment(ref i1)]).ToString();
                int location2 = base.Offset + 6;
                _params["passworded"] = Response[System.Threading.Interlocked.Increment(ref location2)].ToString();
                int offset2 = base.Offset;
                _params["secureserver"] = Response[System.Threading.Interlocked.Increment(ref offset2)].ToString();

                base.Offset += 1;
                //Increment the offset to take the last read byte into account.

                _params["gameversion"] = ReadNextParam();
                // In most cases, the reply will end here. But some servers include an Extra Data Flag along
                // with some extra data, so if we'll read that too, if available.

                if (base.Offset < Response.Length)
                {
                    byte flag = Response[base.Offset + 20];
                    //base.Offset += 1;
                    base.Offset += 20;
                    //  	The server's game port # is included
                    //if ((flag & 0x80) > 0)
                    //{
                        _params["serverport"] = Convert.ToString(BitConverter.ToInt16(Response, base.Offset));
                        base.Offset += 2;
                    //}


                    // The spectator port # and then the spectator server name are included
                    //if ((flag & 0x40) > 0)
                    //{
                        _params["spectatorport"] = Convert.ToString(BitConverter.ToInt16(Response, base.Offset));
                        base.Offset += 2;
                        _params["spectatorname"] = ReadNextParam();
                    //}

                    // The game tag data string for the server is included [future use]
                    //if ((flag & 0x20) > 0)
                    //{
                        _params["gametagdata"] = ReadNextParam();
                    //}
                }
            }
        }

        private void ParseDetailsGoldSource()
        {
            _params["serveraddress"] = ReadNextParam();
            _params["hostname"] = ReadNextParam();
            _params["mapname"] = ReadNextParam();
            _params["mod"] = ReadNextParam();
            _params["modname"] = ReadNextParam();
            // Removed a bunch of System.Math.Max calls here, just like in the Source-class.
            _params["playernum"] = Response[Offset].ToString();
            int location = Offset;
            _params["maxplayers"] = Response[System.Threading.Interlocked.Increment(ref location)].ToString(); //TODO:areed Fix maxplayers
            int offset = Offset;
            _params["protocolver"] = Response[System.Threading.Interlocked.Increment(ref offset)].ToString();

            int i = Offset;
            _params["servertype"] = Strings.ChrW(Response[System.Threading.Interlocked.Increment(ref i)]).ToString();
            int location1 = Offset;
            _params["serveros"] = Strings.ChrW(Response[System.Threading.Interlocked.Increment(ref location1)]).ToString();
            int offset1 = Offset;
            _params["passworded"] = Response[System.Threading.Interlocked.Increment(ref offset1)].ToString();
            int i1 = Offset;
            _params["modded"] = Response[System.Threading.Interlocked.Increment(ref i1)].ToString();

            if (Response[Offset] == 1)
            {
                _params["modwebpage"] = ReadNextParam();
                _params["moddlserver"] = ReadNextParam();
                Offset += 1;
                // Skip the extra null byte here.

                // Certain servers doesnt seem to include this part so we'll
                // check to make sure it does before trying to read it.
                if (Response.Length > base.Offset + 10)
                {
                    _params["modversion"] = BitConverter.ToInt32(Response, Offset).ToString();
                    Offset += 4;
                    _params["modsize"] = BitConverter.ToInt32(Response, Offset).ToString();
                    Offset += 4;
                    _params["serversidemod"] = Response[Offset].ToString();
                    int location2 = Offset;
                    _params["modcustomclientdll"] = Response[System.Threading.Interlocked.Increment(ref location2)].ToString();

                }
            }

            // As with the block of code above, I'm finding that certain servers just doesnt return this
            // information so we'll make sure it does before trying to read it.
            if (Response.Length > base.Offset + 2)
            {
                int location2 = Offset;
                _params["secured"] = Response[System.Threading.Interlocked.Increment(ref location2)].ToString();
                _params["botcount"] = Response[Offset].ToString();
            }

        }

        private void ParseRules()
        {
            string key = null;
            string val = null;
            Offset = 7;

            for (int i = 0; i <= (BitConverter.ToInt16(Response, 5) * 2) - 1; i++)
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

                if (numPlayers > 0)
                {
                    int pNr = 0;
                    // The number of players reported as playing on the server (given by the variable numPlayers)
                    // apparently isnt necessarily the same as the number of players reported in the response.
                    // Thats why a For-loop wont work here, instead we'll have to use a While-loop.
                    do
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
                    } while (pNr < numPlayers - 1 && base.Offset < Response.Length);
                }
            }
        }
    }
}



