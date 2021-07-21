using System.IO;
using Newtonsoft.Json;

namespace hotel_mini_proxy
{
    internal class Config
    {
        internal int MqttPort { get; private set; }
        internal string MqttHost { get; private set; }

        internal string RabbitHost { get; private set; }
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

        internal string RabbitTopicIn { get; private set; }
        internal string RabbitTopicOut { get; private set; }

        private readonly bool? _useSsl;
        private readonly bool? _useAuthorization;
        private readonly bool? _useRabbitAuthorization;
        private readonly string _userName;
        private readonly string _password;
        private readonly string _rabbitExchange;
        private readonly string _rabbitUserName;
        private readonly string _rabbitPassword;
        private readonly string _mqttGroupId;
        private readonly string _mqttPlaceId;
        private readonly string _mqttTopicIn;
        private readonly string _mqttTopicOut;
        private readonly string _mqttTopicLastWill;
        private readonly string _mqttTopicNewBorn;
        private readonly string _mqttTopicAlert;
        private readonly string _mqttTopicPing;
        private readonly string _mqttTopicInf;

        public Config()
        {
            using (var reader = File.OpenText("config.json"))
            {
                dynamic config = JsonConvert.DeserializeObject(reader.ReadToEnd());
                if (config != null)
                {
                    this.MqttPort = config["mqttClientOptions"]["port"]; //port of MQTT broker
                    this.MqttHost = config["mqttClientOptions"]["host"]; //MQTT broker's host
                    this._useSsl = config["mqttClientOptions"]["useSsl"];
                    this._useAuthorization = config["mqttClientOptions"]["useAuthorization"];
                    this._userName = config["mqttClientOptions"]["userName"];
                    this._password = config["mqttClientOptions"]["password"];
                    this._mqttTopicIn = config["mqttTopicIn"]; //In coming Topic name
                    this._mqttTopicOut = config["mqttTopicOut"]; //Out coming topic name
                    this._mqttGroupId = config["mqttGroupId"]; //Group ID
                    this._mqttPlaceId = config["mqttPlaceId"]; // PlaceID for Chanel
                    this._mqttTopicLastWill = config["mqttTopicLastWill"];
                    this._mqttTopicNewBorn = config["mqttTopicNewBorn"];
                    this._mqttTopicAlert = config["mqttTopicAlert"];
                    this._mqttTopicPing = config["mqttTopicPing"];
                    this._mqttTopicInf = config["mqttTopicInf"];
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
                    this._useRabbitAuthorization = config["useRabbitAuthorization"];
                    // this.QueueTopicIn = config["queue"]["topic"];
                    this.RabbitTopicIn = config["rabbitTopicIn"];
                    this.RabbitTopicOut = config["rabbitTopicOut"];
                    this.RabbitHost = config["rabbitClientOptions"]["host"];
                    this._rabbitPassword = config["rabbitClientOptions"]["password"];
                    this._rabbitUserName = config["rabbitClientOptions"]["userName"];
                }
            }
        }

        public string SubscribeTopic => $"{_mqttPlaceId}/{_mqttGroupId}/{_mqttTopicIn}";
        public string PublicTopic => $"{_mqttPlaceId}/{_mqttGroupId}/{_mqttTopicOut}";
        public string LastWillTopic => $"{_mqttPlaceId}/{_mqttGroupId}/{_mqttTopicLastWill}";
        public string NewBornTopic => $"{_mqttPlaceId}/{_mqttGroupId}/{_mqttTopicNewBorn}";
        public string AlertTopic => $"{_mqttPlaceId}/{_mqttGroupId}/{_mqttTopicAlert}";
        public string PingTopic => $"{_mqttPlaceId}/{_mqttGroupId}/{_mqttTopicPing}";
        public string InfTopic => $"{_mqttPlaceId}/{_mqttGroupId}/{_mqttTopicInf}";
        public bool UseSsl => this._useSsl ?? true;
        public bool UseAuthorization => this._useAuthorization ?? false;
        public string UserName => this._userName ?? "";
        public string Password => this._password ?? "";
        public bool UseRabbitAuthorization => this._useRabbitAuthorization ?? true;
        public string RabbitUserName => this._rabbitUserName ?? "guest";
        public string RabbitPassword => this._rabbitPassword ?? "guest";
        public string RabbitExchange => this._rabbitExchange ?? "";

    }
}
