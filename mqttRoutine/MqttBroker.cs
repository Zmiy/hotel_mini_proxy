using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using hotel_mini_proxy.mail;
using hotel_mini_proxy.PmsInterface;
using NLog;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using static hotel_mini_proxy.Tools.ChrOperation;

namespace hotel_mini_proxy.mqttRoutine
{
    public class MessageToPmsEventArgs : EventArgs
    {
        public string Message { get; set; }
    }

    internal class MqttBroker
    {
        public delegate void MessageToPmsHandler(object sender, MessageToPmsEventArgs e);

        public event MessageToPmsHandler MqttToPms;

        //private readonly Logger _logger;
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

        public void Connect2Mqtt()
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
                    var code = _config.UseAutorization ? _clientMqtt.Connect(clientId + atempt, _config.UserName, _config.Password, true, 60) : _clientMqtt.Connect(clientId + atempt, null, null, true, 60);

                    //139.162.222.115, MATZI
                    //matzi /
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
            Connect2Mqtt();
        }

        private void _clientMqtt_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                var msg = Encoding.UTF8.GetString(e.Message).Trim(STX, ETX);
                var s = msg.Trim(STX, ETX).Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                MqttLogger.Trace($"Received From MQTT (topic:{e.Topic}): <STX>{msg}<ETX>, clientId={_clientMqtt.ClientId}");
                switch (s[0])
                {

                    case "PS":

                        if (_config.Interface == "BestBar")
                        {
                            var fias = new FiasTcp();
                            var obj = fias.ParceBilingString(msg);
                            msg = _prot.MakeBillingString(obj).Trim(STX, ETX);
                        }
                        BillingLogger.Info($"Fire PS's message: {$"{STX}{msg}{ETX}"} to the PMS");
                        break;
                    case "LA":
                        _clientMqtt.Publish(_config.PublicTopic, Encoding.UTF8.GetBytes($"{STX}{msg}{ETX}"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
                        MqttLogger.Trace($"Sent LA's answer to Mqtt: <STX>{msg}<ETX>");
                        break;
                }
                if (MqttToPms != null)
                {
                    MqttLogger.Info($"Fire message: <STX>{msg}<ETX> to the PMS");
                    MessageToPmsEventArgs ev = new MessageToPmsEventArgs()
                    {
                        Message = $"{STX}{msg}{ETX}"
                    };
                    MqttToPms(this, ev);
                }

            }
            catch (Exception ex)
            {
                MqttLogger.Error($"Error parse incoming MQTT message {ex.Message}\n\t\t {ex.Data}");
            }


        }

        public void SendToMqtt(string message)
        {
            if (message.Contains("|PA|"))
            {
                BillingLogger.Info($"Send PA's answer: <STX>{message.Trim(STX, ETX)}<ETX> to the MQTT brocker");
            }
            _clientMqtt.Publish(_config.PublicTopic, Encoding.UTF8.GetBytes($"{message}"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
            MqttLogger.Trace($"Sent message from PMS to Mqtt: <STX>{message.Trim(STX, ETX)}<ETX>");
        }

        //subscribed to MQTT
        private void _clientMqtt_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            MqttLogger.Trace($"Subscribed To Mqtt Broker {_clientMqtt.WillTopic}, {e.MessageId}");
            _clientMqtt.MqttMsgPublishReceived += _clientMqtt_MqttMsgPublishReceived;
            _clientMqtt.MqttMsgPublished += _clientMqtt_MqttMsgPublished;
            _clientMqtt.MqttMsgUnsubscribed += _clientMqtt_MqttMsgUnsubscribed;

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
