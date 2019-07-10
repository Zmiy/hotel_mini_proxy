using System.IO;
using Newtonsoft.Json;

namespace hotel_mini_proxy
{
    internal class Config
    {
        //private readonly int _mqttPort;
        //private readonly string _mqttHost;
        internal int MqttPort { get; private set; }
        internal string MqttHost { get; private set; }
        internal string HotelHost { get; private set; }
        internal int HotelPort { get; private set; }
        internal int ListenerPort { get; private set; }
        internal int MqttClientTickerStart { get; private set; }
        internal string Interface { get; private set; }
        private readonly string _mqttGroupId;
        private readonly string _mqttPlaceId;
        private readonly string _mqttTopicIn;
        private readonly string _mqttTopicOut;

        //private readonly int _hotelPort;
        //private readonly int _listenerPort;


        public Config()
        {
            using (var reader = File.OpenText("config.json"))
            {
                dynamic config = JsonConvert.DeserializeObject(reader.ReadToEnd());
                this.MqttPort = config["mqttClientOptions"]["port"]; //port of MQTT brocker
                this.MqttHost = config["mqttClientOptions"]["host"]; //MQTT brocker's host
                this._mqttTopicIn = config["mqttTopicIn"]; //Incomming Topic name
                this._mqttTopicOut = config["mqttTopicOut"];//Outcomming topic name
                this._mqttGroupId = config["mqttGroupId"]; //Group ID
                this._mqttPlaceId = config["mqttPlaceId"]; // PlaceID for Chanel
                this.HotelHost = config["hotelClientOptions"]["hotelHost"]; //PMS's host
                this.HotelPort = config["hotelClientOptions"]["hotelPort"]; //PMS's host
                this.ListenerPort = config["tcpListener"]["listenerPort"];  //Port for the Proxy's TCP listhener. proxy listen this port on any intefaces.
                this.MqttClientTickerStart = config["MqttClientTickerStart"]; //Min ticket number of MQTT client
                this.Interface = config["hotelClientOptions"]["interface"];
            }
        }

        public string SubscribeTopic => $"{_mqttPlaceId}/{_mqttGroupId}/{_mqttTopicIn}";
        public string PublicTopic => $"{_mqttPlaceId}/{_mqttGroupId}/{_mqttTopicOut}";

    }
}
