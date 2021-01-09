using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using RabbitMQ.Client;

namespace hotel_mini_proxy.Rabbit
{
    public class QueueMessageEventArgs : EventArgs
    {
        public string Message { get; set; }
        // public string[] TypeOfAnswer { get; set; }
    }
    class QueueBroker
    {

        private ConnectionFactory _factory;
        private IConnection _connection;
        private IModel _channel;

        private static readonly Logger PmsLogger = LogManager.GetLogger("Queue Broker");
        public RabbitWorker QuoueWorker { get; private set; }
        private readonly Config _config;

        public delegate void QueueMessageHandler(object sender, QueueMessageEventArgs e);
        public event QueueMessageHandler QueueBrokerMessage;
        public event QueueMessageHandler SmartThingsProtocolEvent;
        public QueueBroker(Config config)
        {
            this._config = config;
            QuoueWorker = new RabbitWorker(_config.QueueTopic);
        }

        public void Connect()
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

        public void GetMessage()
        {
            //Console.WriteLine(" [*] Waiting for messages.");
            PmsLogger.Trace("[*] Waiting for messages.");
            while (_connection.IsOpen)
            {

                BasicGetResult result = _channel.BasicGet(queue: _config.QueueTopic, autoAck: false);
                if (result != null)
                {
                    string message = Encoding.UTF8.GetString(result.Body.ToArray());
                    //Console.WriteLine(" [x] Received {0}", message);
                    PmsLogger.Trace(" [x] Received {0}", message);
                    var e = new QueueMessageEventArgs { Message = message };
                    QueueBrokerMessage?.Invoke(this, e);
                    SmartThingsProtocolEvent?.Invoke(this, e);
                    Random random = new Random();

                    int dots = random.Next(1, 10); //message.Split('.').Length - 1;
                    Thread.Sleep(dots * 1000);
                    //Console.WriteLine(" [x] Done");
                    PmsLogger.Trace(" [x] Done");
                    _channel.BasicAck(deliveryTag: result.DeliveryTag, multiple: false);

                }
            }

        }

    }
}
