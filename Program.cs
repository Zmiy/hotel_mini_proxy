using System;
using System.Security.Cryptography.X509Certificates;
using hotel_mini_proxy.PmsInterface;
using hotel_mini_proxy.pmsRoutine;
using hotel_mini_proxy.Rabbit;
using hotel_mini_proxy.SmartThingsProtocol;
using hotel_mini_proxy.SmartThingsProtocol.mqttRoutine;
using hotel_mini_proxy.SmartThingsProtocol.Rabbit;
using NLog;
using uPLibrary.Networking.M2Mqtt;
using TcpLibrary;
using TcpClient = TcpLibrary.TcpClient;
using System.Reflection;

namespace hotel_mini_proxy
{
    internal static class Program
    {
        // private static MqttClient _clientMqtt;
        // private static readonly TcpClient HotelPmsClient = new TcpClient();
        // private static readonly TcpServer HotelListener = new TcpServer();
        public static Config Config;
        public static readonly DateTime StartDateTime = DateTime.Now;
        public static DateTime? LastPmsCommunicationTime = null;
        private static Protocol _prot;
        // private static X509Certificate2 _clientCert;
        // private static X509Certificate _caCert;
        private static readonly Logger MainLogger = LogManager.GetLogger("Main Broker");
        private static PmsBroker _pmsBroker;
        private static HotelBroker _hotelBroker;
        // private static MqttBroker _mqttBroker;
        // private static  RabbitMqBroker _rabbitMqBroker;
        private static SmartThing _smartThingBroker;

        private static void InitPmsProtocol()
        {
            switch (Config.Interface.ToLower())
            {
                case "bestbar":
                    {
                        _prot = new BestBar();
                        break;
                    }
                case "homi":
                    {
                        _prot = new FiasTcp();
                        break;
                    }
                case "bartech":
                    {
                        _prot = new Bartech();
                        break;
                    }
                default:
                    {
                        _prot = new FiasTcp();
                        break;
                    }
            }
        }

        private static void InitSmartThingProtocol()
        {
            switch (Config.SmartThingInterface)
            {
                case "mqtt":
                    {
                        _smartThingBroker = new MqttBroker(Config, MainLogger, _prot);
                        break;
                    }
                case "rabbit":
                    {
                        _smartThingBroker = new RabbitMqBroker(Config, _prot);
                        break;
                    }
            }
        }


        static void Main(string[] args)
        {

            MainLogger.Info("------------Started------------");
            var version = Assembly.GetExecutingAssembly().GetName().Version;

            var buildDate = new DateTime(2000, 1, 1)
               .AddDays(version.Build).AddSeconds(version.Revision * 2);

            var displayableVersion = $"{version} ({buildDate})";
            MainLogger.Info(displayableVersion);
            Config = new Config(); //read a configuration info
            InitPmsProtocol();
            //Client to PMS
            _pmsBroker = new PmsBroker(Config, MainLogger, _prot);
            _pmsBroker.Connect2Pms();
            // _pmsBroker.pmsConnected += _pmsBroker_pmsConnected;
            // _pmsBroker.pmsDisconnected += _pmsBroker_pmsDisconnected;
            _pmsBroker.MqttAnswer += PmsBroker_MqttAnswer;
            _pmsBroker.HotelAnswer += PmsBroker_HotelAnswer;
            // Listener for hotel 
            _hotelBroker = new HotelBroker(Config, MainLogger, _prot);
            _hotelBroker.ListenHotelRequests();
            _hotelBroker.MessageForPms += _hotelBroker_messageForPms;

            //MQTT connect
            //_clientCert = new X509Certificate2("cert/client2048.pfx", "tkphbv#1");

            // _mqttBroker = new MqttBroker(Config, MainLogger, _prot);
            // _mqttBroker.Connect2SmartProtocol();
            // _mqttBroker.MqttToPms += _mqttBroker_MqttToPms;
            // _mqttBroker.SmartThingToPms += _mqttBroker_MqttToPms;
            InitSmartThingProtocol();
           _smartThingBroker.Connect2SmartProtocol();
            _smartThingBroker.SmartThingToPms += _mqttBroker_MqttToPms;
            

        }



        private static void _mqttBroker_MqttToPms(object sender, MessageToPmsEventArgs e)
        {
            MainLogger.Info($"Sending a MQTT's message: {e.Message} to the PMS's broker");
            _pmsBroker.SendToPms(e.Message, e.Message.Contains("PS") ? "PS" : "NO", "MQTT Broker");

        }

        private static void _hotelBroker_messageForPms(object sender, SendDataToPmsEventArgs e)
        {
            MainLogger.Info($"Sending a {e.TypeOfMessage}'s request to the PMS's broker");
            _pmsBroker.SendToPms(e.Message, e.TypeOfMessage, "Hotel Broker");
        }

        private static void PmsBroker_HotelAnswer(object sender, AnswerEventArgs e)
        {
            MainLogger.Info($"Sending a {e.TypeOfAnswer}'s answer to the Hotel's broker");
            _hotelBroker.SendToHotel(e.Answer);
            LastPmsCommunicationTime = DateTime.Now;
        }

        private static void PmsBroker_MqttAnswer(object sender, AnswerEventArgs e)
        {
            MainLogger.Info($"Sending a {e.TypeOfAnswer}'s PMS's answer to MQTT broker");
            // _mqttBroker.SendToMqtt(e.Answer);
            _smartThingBroker.SendToMqtt(e.Answer);
            LastPmsCommunicationTime = DateTime.Now;
        }


        //private static bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        //{
        //    return true;
        //    //var certificateToValidate = new X509Certificate2(certificate);

        //    //chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        //    //chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
        //    //chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority; // We have an untrusted root ca certificate
        //    //chain.ChainPolicy.VerificationTime = DateTime.Now;
        //    //chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 0, 0);

        //    //chain.ChainPolicy.ExtraStore.Add(_clientCert);
        //    //bool isChainValid = chain.Build(certificateToValidate);
        //    //if (!isChainValid)
        //    //{
        //    //    string[] errors = chain.ChainStatus
        //    //        .Select(x => $"{x.StatusInformation.Trim()} ({x.Status})")
        //    //        .ToArray();

        //    //    string certificateErrorsString = "Unknown errors.";
        //    //    if (errors.Length > 0)
        //    //    {
        //    //        certificateErrorsString = string.Join(", ", errors);
        //    //    }

        //    //    throw new Exception("Trust chain did not complete to the known authority anchor. Errors: " + certificateErrorsString);
        //    //}

        //    //// Check if chain contains our root ca certificate
        //    //var valid = chain.ChainElements
        //    //    .Cast<X509ChainElement>()
        //    //    .Any(x => x.Certificate.Thumbprint == _clientCert.Thumbprint);

        //    //return valid;
        //}



        //private static void _clientMqtt_ConnectionClosed(object sender, EventArgs e)
        //{
        //    var mail = new Smtpmail.SendingMail(Config.SendTo, "A connect with the MQTT's broker has lost.")
        //    {
        //        Subj = "Connectin with MQTT was dropped"
        //    };

        //    mail.SendMail();
        //    Logger.Warn("--------MQTT Connection closed-----------");
        //    _clientMqtt.Unsubscribe(new string[] { Config.SubscribeTopic });
        //    _clientMqtt = null;
        //    Connect2SmartProtocol();
        //}

        //private static void _clientMqtt_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        //{
        //    try
        //    {
        //        var msg = Encoding.UTF8.GetString(e.Message);
        //        var s = msg.Trim(STX, ETX).Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        //        if (s[0] == "PS" && Config.Interface == "BestBar")
        //        {
        //            var fias = new FiasTcp();
        //            var obj = fias.ParceBilingString(msg);
        //            msg = Prot.MakeBillingString(obj);
        //        }
        //        if (s[0] == "LA")
        //        {
        //            _clientMqtt.Publish(Config.PublicTopic, Encoding.UTF8.GetBytes($"{ETX}{msg}{STX}"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
        //            Logger.Trace($"Sent to Mqtt LA's answer: <ETX>{msg}<ETX>");
        //        }
        //        if (HotelPmsClient.IsConnected)
        //        {
        //            HotelPmsClient.SendData($"{STX}{msg}{ETX}");
        //        }
        //        Logger.Trace($"Received From MQTT (topic:{e.Topic}): {Encoding.UTF8.GetString(e.Message)}\n\t\tSend to PMS: <STX>{msg.Trim(new char[] { ETX, STX })}<ETX>, clientId={_clientMqtt.ClientId}");

        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error($" Error parse incoming MQTT message {ex.Message}\n\t\t {ex.Data}");
        //    }


        //}

        ////subscribed to MQTT
        //private static void _clientMqtt_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        //{
        //    Logger.Trace($"Subscribed To Mqtt Broker {_clientMqtt.WillTopic}, {e.MessageId}");
        //    _clientMqtt.MqttMsgPublishReceived += _clientMqtt_MqttMsgPublishReceived;
        //    _clientMqtt.MqttMsgPublished += _clientMqtt_MqttMsgPublished;
        //    _clientMqtt.MqttMsgUnsubscribed += _clientMqtt_MqttMsgUnsubscribed;

        //}

        //private static void _clientMqtt_MqttMsgUnsubscribed(object sender, MqttMsgUnsubscribedEventArgs e)
        //{
        //    Logger.Warn("MQTT client unsubscribed");
        //}

        //private static void _clientMqtt_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        //{
        //    Logger.Trace($"MQTT client: Mesage sent = {e.MessageId}, messageId:{e.MessageId}");
        //}


    }
}
