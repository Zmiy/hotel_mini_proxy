using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using hotel_mini_proxy.PmsInterface;
using hotel_mini_proxy.Rabbit;
using hotel_mini_proxy.Tools;
using NLog;
using RabbitMQ.Client;

namespace hotel_mini_proxy.SmartThingsProtocol.Rabbit
{

    class RabbitMqBroker : SmartThing
    {

        private ConnectionFactory _factory;
        private IConnection _connection;
        private IModel _channel;
        private readonly Protocol _prot;
        private static readonly Logger PmsLogger = LogManager.GetLogger("Queue Broker");
        // public RabbitWorker QuoueWorker { get; private set; }
        private readonly Config _config;

        public override event MessageToPmsHandler SmartThingToPms;
        private static readonly Logger BillingLogger = LogManager.GetLogger("Billing Rabbit Broker");
        private static readonly Logger RabbitLogger = LogManager.GetLogger("Rabbit Broker");
        public RabbitMqBroker(Config config, Protocol protocol)
        {
            this._config = config;
            _prot = protocol;
            // QuoueWorker = new RabbitWorker(_config.QueueTopic);
        }

        public override void Connect2SmartProtocol()
        {
            //Task tsk = new Task(TryConnect2Mqtt);
            Connect();
            Task tsk = new Task(GetMessage);
            tsk.Start();

            Task.WaitAll(tsk);
        }

        private void Connect()
        {
            _factory = new ConnectionFactory() { HostName = "localhost" };
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: _config.QueueTopic, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        }

        public void Close()
        {
            _channel.Close(200, "PMS disconnect");
            _connection.Close();
            _connection.Dispose();
            _connection = null;
        }

        private void GetMessage()
        {
            //Console.WriteLine(" [*] Waiting for messages.");
            RabbitLogger.Trace("[*] Waiting for messages.");
            while (_connection.IsOpen)
            {

                BasicGetResult result = _channel.BasicGet(queue: _config.QueueTopic, autoAck: false);
                if (result != null)
                {
                    var msg = Encoding.UTF8.GetString(result.Body.ToArray());
                    var s = msg.Trim(ChrOperation.STX, ChrOperation.ETX).Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    RabbitLogger.Trace(" [x] Received From rabbit Broker{0}", msg);
                    RabbitLogger.Trace($"Received From MQTT (queue:{_config.QueueTopic}): <STX>{msg}<ETX>");
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
                            // _clientMqtt.Publish(_config.PublicTopic, Encoding.UTF8.GetBytes($"{ChrOperation.STX}{msg}{ChrOperation.ETX}"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                            RabbitLogger.Trace($"Sent LA's answer to Mqtt: <STX>{msg}<ETX>");
                            break;
                    }
                    SmartThingToPms?.Invoke(this, new MessageToPmsEventArgs() { Message = $"{ChrOperation.STX}{msg}{ChrOperation.ETX}" });
                    RabbitLogger.Info($"Fire message: <STX>{msg}<ETX> to the PMS");

                    Random random = new Random();

                    int dots = random.Next(1, 10); //message.Split('.').Length - 1;
                    Thread.Sleep(dots * 1000);
                    //Console.WriteLine(" [x] Done");
                    PmsLogger.Trace(" [x] Done");
                    _channel.BasicAck(deliveryTag: result.DeliveryTag, multiple: false);

                }
            }

        }



        public override void SendToMqtt(string answer)
        {
            throw new NotImplementedException();
        }
    }
}
