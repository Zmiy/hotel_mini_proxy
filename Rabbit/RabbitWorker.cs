using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;


namespace hotel_mini_proxy.Rabbit
{
    class RabbitWorker
    {
        private string Topic { get; set; }
        private ConnectionFactory _factory;
        private IConnection _connection;
        private IModel _channel;
        public RabbitWorker(string topic)
        {
            this.Topic = topic;
        }

        public void Close()
        {

            _connection.Close(1, "PMS disconnect");
            _connection.Dispose();
            _connection = null;
        }

        private void consumer_Received(object model, BasicDeliverEventArgs ea)
        {

        }

        public void Worker()
        {
            _factory = new ConnectionFactory() { HostName = "localhost" };
            using (_connection = _factory.CreateConnection())
            {
                using (_channel = _connection.CreateModel())
                {

                    _channel.QueueDeclare(queue: Topic, durable: true, exclusive: false, autoDelete: false, arguments: null);

                    _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                    Console.WriteLine(" [*] Waiting for messages.");

                    var consumer = new EventingBasicConsumer(_channel);

                    consumer.Received += (model, ea) =>
                    {

                        var body = ea.Body;
                        var message = Encoding.UTF8.GetString(body.ToArray());
                        Console.WriteLine(" [x] Received {0}", message);
                        Random random = new Random();

                        int dots = random.Next(1, 10); //message.Split('.').Length - 1;
                        Thread.Sleep(dots * 1000);

                        Console.WriteLine(" [x] Done");

                        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        //await Task.Yield();
                    };
                    _channel.BasicConsume(queue: Topic, autoAck: false, consumer: consumer);
                    Task.Delay(1000);
                    //Console.WriteLine(" Press [enter] to exit.");
                    Console.ReadLine();
                }
            }

        }

    }

}
