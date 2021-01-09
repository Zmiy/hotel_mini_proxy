using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using hotel_mini_proxy.mail;
using hotel_mini_proxy.PmsInterface;
using hotel_mini_proxy.Tools;
using NLog;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace hotel_mini_proxy.SmartThingsProtocol.mqttRoutine
{
    //public class MessageToPmsEventArgs : EventArgs
    //{
    //    public string Message { get; set; }
    //}

    internal class MqttBroker : SmartThing
    {
        // public delegate void MessageToPmsHandler(object sender, MessageToPmsEventArgs e);

        // public event SmartThing.MessageToPmsHandler SmartThingToPms;
        public override event MessageToPmsHandler SmartThingToPms;
        // public event MessageToPmsHandler MqttToPms;

        // private readonly Logger _logger;
        private MqttClient _clientMqtt;
        private readonly Config _config;
        private readonly Protocol _prot;
        private X509Certificate2 _clientCert;
        private X509Certificate _caCert;
        private static readonly Logger BillingLogger = LogManager.GetLogger("Billing Mqtt Broker");
        private static readonly Logger MqttLogger = LogManager.GetLogger("Mqtt Broker");
        public MqttBroker(Config config, Logger logger, Protocol protocol)
        {
            _config = config;
            //_logger = logger;
            _prot = protocol;
        }

        public override void Connect2SmartProtocol()
        {
            //Task tsk = new Task(TryConnect2Mqtt);
            Task tsk = new Task(CreateMqttClient);
            tsk.Start();

            Task.WaitAll(tsk);
        }

        //create new MQTTC client
        private void CreateMqttClient()
        {
            while (_clientMqtt == null)
            {
                try
                {

                    _caCert = null;//X509Certificate.CreateFromCertFile("cert/server.crt");
                    _clientCert = null;//new X509Certificate2("cert/client.key");

                    _clientMqtt = new MqttClient(_config.MqttHost, _config.MqttPort, _config.UseSsl, _caCert, _clientCert, MqttSslProtocols.TLSv1_2);

                    _clientMqtt.MqttMsgSubscribed += _clientMqtt_MqttMsgSubscribed;
                    _clientMqtt.ConnectionClosed += _clientMqtt_ConnectionClosed;
                    _clientMqtt.ProtocolVersion = MqttProtocolVersion.Version_3_1_1;

                }

                catch (Exception ex)
                {
                    MqttLogger.Error($"Issues creating MQTT Client{ex.Message}\n{ex.InnerException}");
                    Thread.Sleep(30 * 1000);
                }
            }
            TryConnect2Mqtt();

        }

        private void TryConnect2Mqtt()
        {
            var atempt = 0;
            MqttLogger.Info("Try connect to MQTT");
            string clientId = $"hotel_proxy_{Guid.NewGuid()}"; //Guid.NewGuid().ToString();
            _clientMqtt.Unsubscribe(new[] { _config.SubscribeTopic });
            while (!_clientMqtt.IsConnected)
            {
                try
                {
                    MqttLogger.Trace($"MQTT Try to connect... {++atempt}, { _config.UserName},{_config.Password}");
                    Thread.Sleep(15 * 1000);
                    //connect to MQTT by SSL or not by Config
                    var lastWillMessage = $"{_config.HotelName}: proxy offline";
                    var lastWillTopic = _config.LastWillTopic;
                    var userName = _config.UseAutorization ? _config.UserName : null;
                    var password = _config.UseAutorization ? _config.Password : null;
                    var code = _clientMqtt.Connect(clientId + atempt, userName, password, false,
                            MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true, lastWillTopic, lastWillMessage, true,
                            60);
                    //var code = _config.UseAutorization ? _clientMqtt.Connect(clientId + atempt, _config.UserName, _config.Password, false, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true, "rothschild22/proxy/lwt", "proxy offline", true, 60) : _clientMqtt.Connect(clientId + atempt, null, null, false, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true, "lwt", "proxy offline", true, 60);
                    MqttLogger.Info($"Connection code: {code}");
                }

                catch (Exception ex)
                {
                    //Console.WriteLine($"{ex.Message}\n {ex.InnerException}\nSleep 30sec");
                    MqttLogger.Error($"{ex}", "Failed connect to MQTT");
                    Thread.Sleep(15 * 1000);
                }

            }
            MqttLogger.Trace("Subscribing to the topic: {0} ", _config.SubscribeTopic);
            var msgId = _clientMqtt.Subscribe(new[] { _config.SubscribeTopic }, new[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            MqttLogger.Trace($"Client mqtt subscribed with id {msgId}");
        }

        private void _clientMqtt_ConnectionClosed(object sender, EventArgs e)
        {
            var mail = new Smtpmail.SendingMail(_config.SendTo, "A connect with the MQTT's broker has lost.")
            {
                Subj = "Connectin with MQTT was dropped"
            };

            mail.SendMail();
            MqttLogger.Warn("--------MQTT Connection closed-----------");
            _clientMqtt.Unsubscribe(new string[] { _config.SubscribeTopic });
            _clientMqtt = null;
            Connect2SmartProtocol();
        }

        private void _clientMqtt_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                var msg = Encoding.UTF8.GetString(e.Message).Trim(ChrOperation.STX, ChrOperation.ETX);
                var s = msg.Trim(ChrOperation.STX, ChrOperation.ETX).Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                MqttLogger.Trace($"Received From MQTT (topic:{e.Topic}): <STX>{msg}<ETX>, clientId={_clientMqtt.ClientId}");
                switch (s[0])
                {

                    case "PS":

                        if (_config.Interface == "BestBar")
                        {
                            var fias = new FiasTcp();
                            var obj = fias.ParceBilingString(msg);
                            msg = _prot.MakeBillingString(obj).Trim(ChrOperation.STX, ChrOperation.ETX);
                        }
                        BillingLogger.Info($"Fire PS's message: {$"{ChrOperation.STX}{msg}{ChrOperation.ETX}"} to the PMS");
                        break;
                    case "LA":
                        _clientMqtt.Publish(_config.PublicTopic, Encoding.UTF8.GetBytes($"{ChrOperation.STX}{msg}{ChrOperation.ETX}"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                        MqttLogger.Trace($"Sent LA's answer to Mqtt: <STX>{msg}<ETX>");
                        break;
                }
                SmartThingToPms?.Invoke(this, new SmartThingsProtocol.MessageToPmsEventArgs() { Message = $"{ChrOperation.STX}{msg}{ChrOperation.ETX}" });
                MqttLogger.Info($"Fire message: <STX>{msg}<ETX> to the PMS");
                //if (MqttToPms != null)
                //{
                //    MqttLogger.Info($"Fire message: <STX>{msg}<ETX> to the PMS");
                //    MessageToPmsEventArgs ev = new MessageToPmsEventArgs()
                //    {
                //        Message = $"{STX}{msg}{ETX}"
                //    };
                //    MqttToPms(this, ev);
                //}

            }
            catch (Exception ex)
            {
                MqttLogger.Error($"Error parse incoming MQTT message {ex.Message}\n\t\t {ex.Data}");
            }


        }

        public override void SendToMqtt(string message)
        {
            if (message.Contains("|PA|"))
            {
                BillingLogger.Info($"Send PA's answer: <STX>{message.Trim(ChrOperation.STX, ChrOperation.ETX)}<ETX> to the MQTT brocker");
            }
            _clientMqtt.Publish(_config.PublicTopic, Encoding.UTF8.GetBytes($"{message}"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
            MqttLogger.Trace($"Sent message from PMS to Mqtt: <STX>{message.Trim(ChrOperation.STX, ChrOperation.ETX)}<ETX>");
        }

        //subscribed to MQTT
        private void _clientMqtt_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            MqttLogger.Trace($"Subscribed To Mqtt Broker {_clientMqtt.WillTopic}, {e.MessageId}");
            _clientMqtt.MqttMsgPublishReceived += _clientMqtt_MqttMsgPublishReceived;
            _clientMqtt.MqttMsgPublished += _clientMqtt_MqttMsgPublished;
            _clientMqtt.MqttMsgUnsubscribed += _clientMqtt_MqttMsgUnsubscribed;
            _clientMqtt.Publish(_config.NewBornTopic, Encoding.UTF8.GetBytes($"{_config.HotelName}: proxy online"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);

        }

        private void _clientMqtt_MqttMsgUnsubscribed(object sender, MqttMsgUnsubscribedEventArgs e)
        {
            MqttLogger.Warn("MQTT client unsubscribed");
        }

        private void _clientMqtt_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
            MqttLogger.Trace($"MQTT client: messageId:{e.MessageId}");
        }

    }
}
