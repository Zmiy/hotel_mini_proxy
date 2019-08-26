using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using hotel_mini_proxy.PmsInterface;
using hotel_mini_proxy.mail;
using NLog;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using TcpLibrary;
using TcpClient = TcpLibrary.TcpClient;
using static hotel_mini_proxy.Tools.ChrOperation;

namespace hotel_mini_proxy
{
    internal static class Program
    {
        public static MqttClient _clientMqtt;
        private static readonly TcpClient HotelPmsClient = new TcpClient();
        private static readonly TcpServer HotelListener = new TcpServer();
        public static Config Config;
        public static Protocol Prot;
        private static X509Certificate2 _clientCert;
        private static X509Certificate _caCert;
        public static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static void Connect2Mqtt()
        {
            //Task tsk = new Task(TryConnect2Mqtt);
            Task tsk = new Task(CreateMqttClient);
            tsk.Start();

            Task.WaitAll(tsk);
        }

        //create new MQTTC client
        private static void CreateMqttClient()
        {
            while (_clientMqtt == null)
            {
                try
                {
                    //_clientCert = new X509Certificate2("cert/client.pfx", "tkphbv#1");
                    _caCert = X509Certificate.CreateFromCertFile("cert/server.crt");
                    _clientCert = new X509Certificate2("cert/client.key");

                    _clientMqtt = new MqttClient(Config.MqttHost, Config.MqttPort, Config.UseSsl, _caCert, _clientCert, MqttSslProtocols.TLSv1_2);

                    _clientMqtt.MqttMsgSubscribed += _clientMqtt_MqttMsgSubscribed;
                    _clientMqtt.ConnectionClosed += _clientMqtt_ConnectionClosed;
                    _clientMqtt.ProtocolVersion = MqttProtocolVersion.Version_3_1_1;

                }

                catch (Exception ex)
                {
                    //Console.WriteLine($"Issues creating MQTT Client{ex.Message}\n{ex.InnerException}");
                    Logger.Error($"Issues creating MQTT Client{ex.Message}\n{ex.InnerException}");
                    Thread.Sleep(30 * 1000);
                }
            }
            TryConnect2Mqtt();

        }

        private static void TryConnect2Mqtt()
        {
            var atempt = 0;
            Logger.Info("Try connect to MQTT");
            const string clientId = "hotel_mini_proxy"; //Guid.NewGuid().ToString();
            _clientMqtt.Unsubscribe(new[] { Config.SubscribeTopic });
            while (!_clientMqtt.IsConnected)
            {
                try
                {
                    Logger.Trace($"MQTT Try to connect... {++atempt}, { Config.UserName},{Config.Password}");
                    Thread.Sleep(15 * 1000);
                    //connect to MQTT by SSL or not by Config
                    var code = Config.UseAutorization ? _clientMqtt.Connect(clientId + atempt, Config.UserName, Config.Password, true, 60) : _clientMqtt.Connect(clientId + atempt, null, null, true, 60);

                    //139.162.222.115, MATZI
                    //matzi /
                    Logger.Info($"connection code: {code}");
                }

                catch (Exception ex)
                {
                    //Console.WriteLine($"{ex.Message}\n {ex.InnerException}\nSleep 30sec");
                    Logger.Error($"{ex}", "Failed connect to MQTT");
                    Thread.Sleep(15 * 1000);
                }

            }
            Logger.Trace("Subscribing to the topic: {0} ", Config.SubscribeTopic);
            var msgId = _clientMqtt.Subscribe(new[] { Config.SubscribeTopic }, new[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            Logger.Trace($"Client mqtt subscribed with id {msgId}");
        }

        static void Main(string[] args)
        {

            Logger.Info("------------Started------------");
            Config = new Config(); //read a configuration info
            //Client to PMS
            HotelPmsClient.Connected += pmsRoutine.PmsRoutine.HotelPmsClient_Connected;
            HotelPmsClient.DataArrival += pmsRoutine.PmsRoutine.HotelPmsClient_DataArrival;
            HotelPmsClient.Disconnect += pmsRoutine.PmsRoutine.HotelPmsClient_Disconnect;
            //HotelPmsClient.Connect(_config.HotelHost, _config.HotelPort);
            pmsRoutine.PmsRoutine.Connect2Pms();

            // Listener for hotel 
            HotelListener.DataArrival += pmsRoutine.PmsRoutine._hotelListener_DataArrival;
            HotelListener.Connected += pmsRoutine.PmsRoutine._hotelListener_Connected;
            IPAddress ipListener = IPAddress.Any;//LocalIpAddress();
            HotelListener.Port(ipListener, Config.ListenerPort);
            HotelListener.StartListen();
            //Console.WriteLine("Start listening on {0}:{1}", ipListener, _config.ListenerPort);
            Logger.Info("Start listening on {0}:{1}", ipListener, Config.ListenerPort);

            //MQTT connect
            //_clientCert = new X509Certificate2("cert/client2048.pfx", "tkphbv#1");

            _clientCert = null;//new X509Certificate2("cert/client.crt");
            _caCert = null;// X509Certificate.CreateFromCertFile("cert/server.crt");
            _clientMqtt = new MqttClient(Config.MqttHost, Config.MqttPort, Config.UseSsl, _caCert, _clientCert, MqttSslProtocols.TLSv1_2);

            _clientMqtt.MqttMsgSubscribed += _clientMqtt_MqttMsgSubscribed;
            _clientMqtt.ConnectionClosed += _clientMqtt_ConnectionClosed;
            _clientMqtt.ProtocolVersion = MqttProtocolVersion.Version_3_1_1;
            Connect2Mqtt();

            //Console.WriteLine("End Of main");
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



        private static void _clientMqtt_ConnectionClosed(object sender, EventArgs e)
        {
            var mail = new Smtpmail.SendingMail(Config.SendTo, "A connect with the MQTT's broker has lost.")
            {
                Subj = "Connectin with MQTT was dropped"
            };

            mail.SendMail();
            Logger.Warn("--------MQTT Connection closed-----------");
            _clientMqtt.Unsubscribe(new string[] { Config.SubscribeTopic });
            _clientMqtt = null;
            Connect2Mqtt();
        }

        private static void _clientMqtt_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                var msg = Encoding.UTF8.GetString(e.Message);
                var s = msg.Trim(STX, ETX).Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (s[0] == "PS" && Config.Interface == "BestBar")
                {
                    var fias = new FiasTcp();
                    var obj = fias.ParceBilingString(msg);
                    msg = Prot.MakeBillingString(obj);
                }
                if (s[0] == "LA")
                {
                    _clientMqtt.Publish(Config.PublicTopic, Encoding.UTF8.GetBytes($"{ETX}{msg}{STX}"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
                    Logger.Trace($"Sent to Mqtt LA's answer: <ETX>{msg}<ETX>");
                }
                if (HotelPmsClient.IsConnected)
                {
                    HotelPmsClient.SendData($"{STX}{msg}{ETX}");
                }
                Logger.Trace($"Received From MQTT (topic:{e.Topic}): {Encoding.UTF8.GetString(e.Message)}\n\t\tSend to PMS: <STX>{msg.Trim(new char[] { ETX, STX })}<ETX>, clientId={_clientMqtt.ClientId}");

            }
            catch (Exception ex)
            {
                Logger.Error($" Error parse incoming MQTT message {ex.Message}\n\t\t {ex.Data}");
            }


        }

        //subscribed to MQTT
        private static void _clientMqtt_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            Logger.Trace($"Subscribed To Mqtt Broker {_clientMqtt.WillTopic}, {e.MessageId}");
            _clientMqtt.MqttMsgPublishReceived += _clientMqtt_MqttMsgPublishReceived;
            _clientMqtt.MqttMsgPublished += _clientMqtt_MqttMsgPublished;
            _clientMqtt.MqttMsgUnsubscribed += _clientMqtt_MqttMsgUnsubscribed;

        }

        private static void _clientMqtt_MqttMsgUnsubscribed(object sender, MqttMsgUnsubscribedEventArgs e)
        {
            Logger.Warn("MQTT client unsubscribed");
        }

        private static void _clientMqtt_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
            Logger.Trace($"MQTT client: Mesage sent = {e.MessageId}, messageId:{e.MessageId}");
        }


    }
}
