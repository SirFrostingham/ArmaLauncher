using System;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ArmaLauncher.Models
{
    public class Arma2FeedModel
    {
        [Serializable]
        public class NewDataSet
        {
            private List<Servers> _itemsField;

            
            [XmlElement("servers")]
            public List<Servers> Items
            {
                get { return _itemsField; }
                set { _itemsField = value; }
            }
        }

        public class Servers
        {
            
            [XmlElement("script", Form = XmlSchemaForm.Unqualified)]
            public List<ServersScript> Script { get; set; }

            
            [XmlElement("server", Form = XmlSchemaForm.Unqualified)]
            public List<ServersServer> Server { get; set; }

            
            [XmlAttribute]
            public string Type { get; set; }
        }
        [Serializable]
        public class ServersScript
        {
            private string _idField;

            
            [XmlAttribute]
            public string Id
            {
                get { return _idField; }
                set { _idField = value; }
            }
        }
        [Serializable]
        public class ServersServer
        {
            private List<ServersServerBattleye> _battleyeField;
            private string _countryField;

            private List<ServersServerCreatedat> _createdatField;

            private List<ServersServerDedicated> _dedicatedField;
            private string _gamenameField;

            private string _hostField;

            private List<ServersServerId> _idField;
            private string _islandField;

            private List<ServersServerLanguage> _languageField;

            private List<ServersServerLat> _latField;

            private List<ServersServerLng> _lngField;
            private string _missionField;

            private string _modField;

            private List<ServersServerMode> _modeField;

            private List<ServersServerModhash> _modhashField;
            private string _nameField;

            private List<ServersServerPassworded> _passwordedField;
            private string _platformField;

            private List<ServersServerPlayers> _playersField;

            private List<ServersServerPort> _portField;

            private List<ServersServerRequiredversion> _requiredversionField;

            private List<ServersServerSignatures> _signaturesField;
            private string _stateField;

            private List<ServersServerUpdatedat> _updatedatField;

            private List<ServersServerVerifysignatures> _verifysignaturesField;
            private string _versionField;

            [XmlElement(Form = XmlSchemaForm.Unqualified)]
            public string Country
            {
                get { return _countryField; }
                set { _countryField = value; }
            }

            [XmlElement(Form = XmlSchemaForm.Unqualified)]
            public string Gamename
            {
                get { return _gamenameField; }
                set { _gamenameField = value; }
            }

            
            [XmlElement(Form = XmlSchemaForm.Unqualified)]
            public string Host
            {
                get { return _hostField; }
                set { _hostField = value; }
            }

            
            [XmlElement(Form = XmlSchemaForm.Unqualified)]
            public string Island
            {
                get { return _islandField; }
                set { _islandField = value; }
            }

            
            [XmlElement(Form = XmlSchemaForm.Unqualified)]
            public string Mission
            {
                get { return _missionField; }
                set { _missionField = value; }
            }

            
            [XmlElement(Form = XmlSchemaForm.Unqualified)]
            public string Mod
            {
                get { return _modField; }
                set { _modField = value; }
            }

            
            [XmlElement(Form = XmlSchemaForm.Unqualified)]
            public string Name
            {
                get { return _nameField; }
                set { _nameField = value; }
            }

            
            [XmlElement(Form = XmlSchemaForm.Unqualified)]
            public string Platform
            {
                get { return _platformField; }
                set { _platformField = value; }
            }

            
            [XmlElement(Form = XmlSchemaForm.Unqualified)]
            public string State
            {
                get { return _stateField; }
                set { _stateField = value; }
            }

            
            [XmlElement(Form = XmlSchemaForm.Unqualified)]
            public string Version
            {
                get { return _versionField; }
                set { _versionField = value; }
            }

            
            [XmlElement("battleye", Form = XmlSchemaForm.Unqualified
                ,
                IsNullable = true)]
            public List<ServersServerBattleye> Battleye
            {
                get { return _battleyeField; }
                set { _battleyeField = value; }
            }

            
            [XmlElement("created-at",
                Form = XmlSchemaForm.Unqualified,
                IsNullable = true)]
            public List<ServersServerCreatedat> Createdat
            {
                get { return _createdatField; }
                set { _createdatField = value; }
            }

            
            [XmlElement("dedicated",
                Form = XmlSchemaForm.Unqualified,
                IsNullable = true)]
            public List<ServersServerDedicated> Dedicated
            {
                get { return _dedicatedField; }
                set { _dedicatedField = value; }
            }

            
            [XmlElement("id", Form = XmlSchemaForm.Unqualified,
                IsNullable = true)]
            public List<ServersServerId> Id
            {
                get { return _idField; }
                set { _idField = value; }
            }

            
            [XmlElement("language", Form = XmlSchemaForm.Unqualified
                ,
                IsNullable = true)]
            public List<ServersServerLanguage> Language
            {
                get { return _languageField; }
                set { _languageField = value; }
            }

            
            [XmlElement("lat", Form = XmlSchemaForm.Unqualified,
                IsNullable = true)]
            public List<ServersServerLat> Lat
            {
                get { return _latField; }
                set { _latField = value; }
            }

            
            [XmlElement("lng", Form = XmlSchemaForm.Unqualified,
                IsNullable = true)]
            public List<ServersServerLng> Lng
            {
                get { return _lngField; }
                set { _lngField = value; }
            }

            
            [XmlElement("mode", Form = XmlSchemaForm.Unqualified,
                IsNullable = true)]
            public List<ServersServerMode> Mode
            {
                get { return _modeField; }
                set { _modeField = value; }
            }

            
            [XmlElement("modhash", Form = XmlSchemaForm.Unqualified,
                IsNullable = true)]
            public List<ServersServerModhash> Modhash
            {
                get { return _modhashField; }
                set { _modhashField = value; }
            }

            
            [XmlElement("passworded",
                Form = XmlSchemaForm.Unqualified,
                IsNullable = true)]
            public List<ServersServerPassworded> Passworded
            {
                get { return _passwordedField; }
                set { _passwordedField = value; }
            }

            
            [XmlElement("players", Form = XmlSchemaForm.Unqualified,
                IsNullable = true)]
            public List<ServersServerPlayers> Players
            {
                get { return _playersField; }
                set { _playersField = value; }
            }

            
            [XmlElement("port", Form = XmlSchemaForm.Unqualified,
                IsNullable = true)]
            public List<ServersServerPort> Port
            {
                get { return _portField; }
                set { _portField = value; }
            }

            
            [XmlElement("required-version",
                Form = XmlSchemaForm.Unqualified, IsNullable = true)]
            public List<ServersServerRequiredversion> Requiredversion
            {
                get { return _requiredversionField; }
                set { _requiredversionField = value; }
            }

            
            [XmlElement("signatures",
                Form = XmlSchemaForm.Unqualified,
                IsNullable = true)]
            public List<ServersServerSignatures> Signatures
            {
                get { return _signaturesField; }
                set { _signaturesField = value; }
            }

            
            [XmlElement("updated-at",
                Form = XmlSchemaForm.Unqualified,
                IsNullable = true)]
            public List<ServersServerUpdatedat> Updatedat
            {
                get { return _updatedatField; }
                set { _updatedatField = value; }
            }

            
            [XmlElement("verify-signatures",
                Form = XmlSchemaForm.Unqualified, IsNullable = true)]
            public List<ServersServerVerifysignatures> Verifysignatures
            {
                get { return _verifysignaturesField; }
                set { _verifysignaturesField = value; }
            }
        }

        [Serializable]
        public class ServersServerBattleye
        {
            private string _nilField;
            private string _typeField;

            private string _valueField;

            
            [XmlAttribute]
            public string Type
            {
                get { return _typeField; }
                set { _typeField = value; }
            }

            
            [XmlAttribute]
            public string Nil
            {
                get { return _nilField; }
                set { _nilField = value; }
            }

            
            [XmlText]
            public string Value
            {
                get { return _valueField; }
                set { _valueField = value; }
            }
        }
        [Serializable]
        public class ServersServerCreatedat
        {
            private string _typeField;

            private string _valueField;

            
            [XmlAttribute]
            public string Type
            {
                get { return _typeField; }
                set { _typeField = value; }
            }

            
            [XmlText]
            public string Value
            {
                get { return _valueField; }
                set { _valueField = value; }
            }
        }
        [Serializable]
        public class ServersServerDedicated
        {
            private string _nilField;
            private string _typeField;

            private string _valueField;

            
            [XmlAttribute]
            public string Type
            {
                get { return _typeField; }
                set { _typeField = value; }
            }

            
            [XmlAttribute]
            public string Nil
            {
                get { return _nilField; }
                set { _nilField = value; }
            }

            
            [XmlText]
            public string Value
            {
                get { return _valueField; }
                set { _valueField = value; }
            }
        }
        [Serializable]
        public class ServersServerId
        {
            private string _typeField;

            private string _valueField;

            
            [XmlAttribute]
            public string Type
            {
                get { return _typeField; }
                set { _typeField = value; }
            }

            
            [XmlText]
            public string Value
            {
                get { return _valueField; }
                set { _valueField = value; }
            }
        }
        [Serializable]
        public class ServersServerLanguage
        {
            private string _typeField;

            private string _valueField;

            
            [XmlAttribute]
            public string Type
            {
                get { return _typeField; }
                set { _typeField = value; }
            }

            
            [XmlText]
            public string Value
            {
                get { return _valueField; }
                set { _valueField = value; }
            }
        }
        [Serializable]
        public class ServersServerLat
        {
            private string _nilField;
            private string _typeField;

            private string _valueField;

            
            [XmlAttribute]
            public string Type
            {
                get { return _typeField; }
                set { _typeField = value; }
            }

            
            [XmlAttribute]
            public string Nil
            {
                get { return _nilField; }
                set { _nilField = value; }
            }

            
            [XmlText]
            public string Value
            {
                get { return _valueField; }
                set { _valueField = value; }
            }
        }
        [Serializable]
        public class ServersServerLng
        {
            private string _nilField;
            private string _typeField;

            private string _valueField;

            
            [XmlAttribute]
            public string Type
            {
                get { return _typeField; }
                set { _typeField = value; }
            }

            
            [XmlAttribute]
            public string Nil
            {
                get { return _nilField; }
                set { _nilField = value; }
            }

            
            [XmlText]
            public string Value
            {
                get { return _valueField; }
                set { _valueField = value; }
            }
        }
        [Serializable]
        public class ServersServerMode
        {
            private string _nilField;

            private string _valueField;

            
            [XmlAttribute]
            public string Nil
            {
                get { return _nilField; }
                set { _nilField = value; }
            }

            
            [XmlText]
            public string Value
            {
                get { return _valueField; }
                set { _valueField = value; }
            }
        }

        [Serializable]
        public class ServersServerModhash
        {
            private string _nilField;

            private string _valueField;

            
            [XmlAttribute]
            public string Nil
            {
                get { return _nilField; }
                set { _nilField = value; }
            }

            
            [XmlText]
            public string Value
            {
                get { return _valueField; }
                set { _valueField = value; }
            }
        }

        [Serializable]
        public class ServersServerPassworded
        {
            private string _typeField;

            private string _valueField;

            
            [XmlAttribute]
            public string Type
            {
                get { return _typeField; }
                set { _typeField = value; }
            }

            
            [XmlText]
            public string Value
            {
                get { return _valueField; }
                set { _valueField = value; }
            }
        }

        [Serializable]
        public class ServersServerPlayers
        {
            private string _typeField;

            private string _valueField;

            
            [XmlAttribute]
            public string Type
            {
                get { return _typeField; }
                set { _typeField = value; }
            }

            
            [XmlText]
            public string Value
            {
                get { return _valueField; }
                set { _valueField = value; }
            }
        }

        [Serializable]
        public class ServersServerPort
        {
            private string _typeField;

            private string _valueField;

            
            [XmlAttribute]
            public string Type
            {
                get { return _typeField; }
                set { _typeField = value; }
            }

            
            [XmlText]
            public string Value
            {
                get { return _valueField; }
                set { _valueField = value; }
            }
        }

        [Serializable]
        public class ServersServerRequiredversion
        {
            private string _nilField;

            private string _valueField;

            
            [XmlAttribute]
            public string Nil
            {
                get { return _nilField; }
                set { _nilField = value; }
            }

            
            [XmlText]
            public string Value
            {
                get { return _valueField; }
                set { _valueField = value; }
            }
        }

        [Serializable]
        public class ServersServerSignatures
        {
            private string _nilField;

            private string _valueField;

            
            [XmlAttribute]
            public string Nil
            {
                get { return _nilField; }
                set { _nilField = value; }
            }

            
            [XmlText]
            public string Value
            {
                get { return _valueField; }
                set { _valueField = value; }
            }
        }

        [Serializable]
        public class ServersServerUpdatedat
        {
            private string _typeField;

            private string _valueField;

            
            [XmlAttribute]
            public string Type
            {
                get { return _typeField; }
                set { _typeField = value; }
            }

            
            [XmlText]
            public string Value
            {
                get { return _valueField; }
                set { _valueField = value; }
            }
        }

        [Serializable]
        public class ServersServerVerifysignatures
        {
            private string _nilField;
            private string _typeField;

            private string _valueField;

            
            [XmlAttribute]
            public string Type
            {
                get { return _typeField; }
                set { _typeField = value; }
            }

            
            [XmlAttribute]
            public string Nil
            {
                get { return _nilField; }
                set { _nilField = value; }
            }

            
            [XmlText]
            public string Value
            {
                get { return _valueField; }
                set { _valueField = value; }
            }
        }
    }
}