using System.IO;
using Newtonsoft.Json;

namespace hotel_mini_proxy
{
    internal class Config
    {
        internal int MqttPort { get; private set; }
        internal string MqttHost { get; private set; }
        internal string HotelHost { get; private set; }
        internal int HotelPort { get; private set; }
        internal int ListenerPort { get; private set; }
        internal int MqttClientTicketStart { get; private set; }
        internal string Interface { get; private set; }
        internal string SmartThingInterface { get; private set; }
        internal string HotelName { get; private set; }
        internal string SenderEmail { get; private set; }
        internal bool EnableSsl { get; private set; }
        internal string SmtpServer { get; private set; }
        internal string SmtpUser { get; private set; }
        internal int SmtpPort { get; private set; }
        internal string SmtpPassword { get; private set; }
        internal string SendTo { get; private set; }

        internal string QueueTopic { get; private set; }

        private readonly bool? _useSsl;
        private readonly bool? _useAutorization;
        private readonly string _userName;
        private readonly string _password;
        private readonly string _mqttGroupId;
        private readonly string _mqttPlaceId;
        private readonly string _mqttTopicIn;
        private readonly string _mqttTopicOut;
        private readonly string _mqttTopicLastWill;
        private readonly string _mqttTopicNewBorn;

        public Config()
        {
            using (var reader = File.OpenText("config.json"))
            {
                dynamic config = JsonConvert.DeserializeObject(reader.ReadToEnd());
                this.MqttPort = config["mqttClientOptions"]["port"]; //port of MQTT brocker
                this.MqttHost = config["mqttClientOptions"]["host"]; //MQTT brocker's host
                this._useSsl = config["mqttClientOptions"]["useSsl"];
                this._useAutorization = config["mqttClientOptions"]["useAutorization"];
                this._userName = config["mqttClientOptions"]["userName"];
                this._password = config["mqttClientOptions"]["password"];
                this._mqttTopicIn = config["mqttTopicIn"]; //Incomming Topic name
                this._mqttTopicOut = config["mqttTopicOut"]; //Outcomming topic name
                this._mqttGroupId = config["mqttGroupId"]; //Group ID
                this._mqttPlaceId = config["mqttPlaceId"]; // PlaceID for Chanel
                this._mqttTopicLastWill = config["mqttTopicLastWill"];
                this._mqttTopicNewBorn = config["mqttTopicNewBorn"];
                this.HotelHost = config["hotelClientOptions"]["hotelHost"]; //PMS's host
                this.HotelPort = config["hotelClientOptions"]["hotelPort"]; //PMS's host
                this.ListenerPort = config["tcpListener"]["listenerPort"];
                this.MqttClientTicketStart = config["MqttClientTicketStart"]; //Min ticket number of MQTT client
                this.Interface = config["hotelClientOptions"]["interface"];
                this.SmartThingInterface = config["smartThingInterface"];

                this.HotelName = config["smtpMailOptions"]["HotelName"];
                this.SenderEmail = config["smtpMailOptions"]["SenderEmail"];
                this.EnableSsl = config["smtpMailOptions"]["EnableSsl"];
                this.SmtpServer = config["smtpMailOptions"]["SMTPServer"];
                this.SmtpPort = config["smtpMailOptions"]["SMTPPort"];
                this.SmtpUser = config["smtpMailOptions"]["SMTPUser"];
                this.SmtpPassword = config["smtpMailOptions"]["SMTPPassword"];
                this.SendTo = config["smtpMailOptions"]["SendTo"];

                this.QueueTopic = config["queue"]["topic"];
            }
        }

        public string SubscribeTopic => $"{_mqttPlaceId}/{_mqttGroupId}/{_mqttTopicIn}";
        public string PublicTopic => $"{_mqttPlaceId}/{_mqttGroupId}/{_mqttTopicOut}";
        public string LastWillTopic => $"{_mqttPlaceId}/{_mqttGroupId}/{_mqttTopicLastWill}";
        public string NewBornTopic => $"{_mqttPlaceId}/{_mqttGroupId}/{_mqttTopicNewBorn}";
        public bool UseSsl => this._useSsl ?? true;
        public bool UseAutorization => this._useAutorization ?? false;
        public string UserName => this._userName ?? "";
        public string Password => this._password ?? "";

    }
}
