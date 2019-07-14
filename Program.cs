using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using hotel_mini_proxy.PmsInterface;
using hotel_mini_proxy.Tools;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using TcpLibrary;
using TcpClient = TcpLibrary.TcpClient;
using static hotel_mini_proxy.Tools.ChrOperation;

namespace hotel_mini_proxy
{
    internal static class Program
    {
        private static MqttClient _clientMqtt = null;
        //private static readonly ManualResetEvent WaitForSubscribed = new ManualResetEvent(false);
        //private static readonly object ConLock = new object();
        private static readonly TcpClient HotelPmsClient = new TcpClient();
        private static readonly TcpServer HotelListener = new TcpServer();
        private static Config _config;
        //private static readonly ManualResetEvent Done = new ManualResetEvent(false);
        private static Protocol _prot;
        private static X509Certificate2 _clientCert;
        private static X509Certificate _caCert;


        private static void TryConnect2Pms()
        {
            Console.WriteLine($"Try connect to the PMS of the hotel: {_config.HotelHost}:{_config.HotelPort}");
            int atempt = 0;
            while (!HotelPmsClient.IsConnected)
            {
                Console.WriteLine($"TCP Try ...{++atempt}");
                HotelPmsClient.Connect(_config.HotelHost, _config.HotelPort);
                Thread.Sleep(10 * 1000);
            }
        }

        private static void Connect2Pms()
        {
            Task tsk = new Task(TryConnect2Pms);
            tsk.Start();
            //Task.WaitAll(tsk);
            //await Task.Run(() => TryConnect2Pms());
        }

        private static void Connect2Mqtt()
        {
            Task tsk = new Task(TryConnect2Mqtt);
            tsk.Start();

            Task.WaitAll(tsk);
        }

        private static void TryConnect2Mqtt()
        {
            int atempt = 0;

            Console.WriteLine("Try to connect to mqtt");
            var clientId = "hotel_mini_proxy";//Guid.NewGuid().ToString();
            _clientMqtt.Unsubscribe(new string[] { _config.SubscribeTopic });

            while (!_clientMqtt.IsConnected)
            {

                try
                {
                    Console.WriteLine($"MQTT Try ...{++atempt}");
                    Thread.Sleep(30 * 1000);
                    _clientMqtt.Connect(clientId + atempt);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message}\n {ex.InnerException}\nSleep 30sec");
                    Thread.Sleep(30 * 1000);
                }

            }
            Console.WriteLine("Subscribe to the topic: {0} ", _config.SubscribeTopic);

            //$"{_mqttPlaceId}/{_mqttGroupId}/{_mqttTopic}");
            _clientMqtt.Subscribe(new string[] { _config.SubscribeTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        static void Main(string[] args)
        {

            _config = new Config(); //read a configuration info
            //Client to PMS
            HotelPmsClient.Connected += HotelPmsClient_Connected;
            HotelPmsClient.DataArrival += HotelPmsClient_DataArrival;
            HotelPmsClient.Disconnect += HotelPmsClient_Disconnect;
            //HotelPmsClient.Connect(_config.HotelHost, _config.HotelPort);
            Connect2Pms();

            // Listener for hotel 
            HotelListener.DataArrival += _hotelListener_DataArrival;
            HotelListener.Connected += _hotelListener_Connected;
            IPAddress ipListener = IPAddress.Any;//LocalIpAddress();
            HotelListener.Port(ipListener, _config.ListenerPort);
            HotelListener.StartListen();
            Console.WriteLine("Start listening on {0}:{1}", ipListener, _config.ListenerPort);


            //MQTT connect
            _clientCert = new X509Certificate2("cert/client2048.pfx", "tkphbv#1");
            _caCert = X509Certificate.CreateFromCertFile("cert/3pi-solutions-CA.crt");
            // Then create the client referencing the certs
            _clientMqtt = new MqttClient(_config.MqttHost, _config.MqttPort, true, null, null, MqttSslProtocols.TLSv1_2);

            //_clientMqtt = new MqttClient(_config.MqttHost, _config.MqttPort, true,X509Certificate.CreateFromCertFile("cert/3pi-solutions-CA.crt"), new X509Certificate(), MqttSslProtocols.TLSv1_2);
            //_clientMqtt = new MqttClient(_config.MqttHost, _config.MqttPort, true, X509Certificate.CreateFromCertFile("cert/3pi-solutions-CA.crt"), X509Certificate.CreateFromCertFile("cert/client2048.crt"), MqttSslProtocols.TLSv1_2);
            _clientMqtt.MqttMsgSubscribed += Client_MqttMsgSubscribed;
            _clientMqtt.ConnectionClosed += _client_ConnectionClosed;
            _clientMqtt.ProtocolVersion = MqttProtocolVersion.Version_3_1_1;
            Connect2Mqtt();
            
            Console.WriteLine("End Of main");
        }

        private static bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            var certificateToValidate = new X509Certificate2(certificate);

            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority; // We have an untrusted root ca certificate
            chain.ChainPolicy.VerificationTime = DateTime.Now;
            chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 0, 0);

            chain.ChainPolicy.ExtraStore.Add(_clientCert);
            bool isChainValid = chain.Build(certificateToValidate);
            if (!isChainValid)
            {
                string[] errors = chain.ChainStatus
                    .Select(x => $"{x.StatusInformation.Trim()} ({x.Status})")
                    .ToArray();

                string certificateErrorsString = "Unknown errors.";
                if (errors.Length > 0)
                {
                    certificateErrorsString = string.Join(", ", errors);
                }

                throw new Exception("Trust chain did not complete to the known authority anchor. Errors: " + certificateErrorsString);
            }

            // Check if chain contains our root ca certificate
            var valid = chain.ChainElements
                .Cast<X509ChainElement>()
                .Any(x => x.Certificate.Thumbprint == _clientCert.Thumbprint);

            return valid;
        }


        private static void _client_ConnectionClosed(object sender, EventArgs e)
        {
            Console.WriteLine("--------MQTT Connection closed-----------");
            _clientMqtt.Unsubscribe(new string[] { _config.SubscribeTopic });
            Connect2Mqtt();
        }

        private static void HotelPmsClient_Disconnect()
        {
            Connect2Pms();
        }

        private static void _hotelListener_Connected(TcpSocket client)
        {
            Console.WriteLine("Hotel Listener: client ({0}) connected", client.Tag);
        }

        private static void _hotelListener_DataArrival(TcpSocket client, long available)
        {

            var trame = string.Empty;
            //string s = "";
            do
            {
                if (available == 0)
                    continue;
                var c = Convert.ToInt32(client.GetData(1).GetValue(0));
                switch (c)
                {
                    case 2:
                        {
                            trame = ((char)c).ToString();
                            break;
                        }

                    case 3:
                        {
                            trame += ((char)c).ToString();
                            try
                            {
                                if (client.GetData().Length > 0)
                                {
                                    c = (int)client.GetData(1).GetValue(0);
                                    trame += ((char)c).ToString();
                                }
                            }
                            catch
                            {
                                c = 0;
                            }

                            break;
                        }

                    case 5:
                    case 6:
                        {
                            trame = ((char)c).ToString();
                            break;
                        }

                    default:
                        {
                            trame += ((char)c).ToString();
                            break;
                        }
                }
            } while (client.BytesAvailable != 0);
            Console.WriteLine($"Received from minibar client: {trame}");
            Recoimessagefiastcp(trame);
        }

        private static string Fdate()
        {
            return DateTime.Now.ToString("yyyyMMdd");
        }
        private static string Ftime()
        {
            return DateTime.Now.ToString("hhmmss");
        }

        private static void SendData(string message)
        {
            if (HotelListener.Clients.Count > 0)
            {
                HotelListener.Clients[0].SendData(STX + message + ETX);
            }

        }

        private static void Recoimessagefiastcp(string mess)
        {
            if (string.IsNullOrEmpty(mess))
            {
                return;
            }
            //var messFias = mess.Substring(1, mess.Length - 1);
            var s = mess.Trim(STX, ETX).Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            switch (s[0])
            {
                case "LS":
                    {
                        SendData("LS|DA" + Fdate() + "|TI" + Ftime() + "|");
                        break;
                    }

                case "LA":
                    {
                        SendData("LA|DA" + Fdate() + "|TI" + Ftime() + "|");
                        break;
                    }

                case "PS":
                    {
                        //SendData($"PA|DA{Fdate()}|RN{s[3].Substring(2)}|P#{s[4].Substring(2)}|PI{Ftime()}|ASOK|");
                        HotelPmsClient.SendData(mess);
                        break;
                    }

                case "LE":
                    {
                        break;
                    }
            }
        }

        private static void HotelPmsClient_DataArrival(long available)
        {
            string s = "";

            for (long i = 1L; i < available; i++)
            {
                var c = Convert.ToInt32(HotelPmsClient.GetData(1).GetValue(0));
                if (c != 0)
                {
                    s += ChrOperation.Chr(c);
                }
            }
            string[] answers = s.Split(new char[] { ETX }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var answer in answers)
            {
                List<ParserResult> results = _prot.Parcer($"{answer}{ETX}");
                foreach (var command in results)
                {
                    switch (command.Command)
                    {
                        case Command.Init:
                            {
                                var initStrings = _prot.InitString.ToString().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var currentInitStr in initStrings)
                                {
                                    HotelPmsClient.SendData(currentInitStr);

                                    Console.WriteLine($"Current init string: <STX>{currentInitStr.TrimStart(STX).TrimEnd(ETX)}<ETX>");
                                }

                                break;
                            }
                        case Command.AsOk:
                        case Command.AsNg:
                            {
                                if (command.Ticket >= _config.MqttClientTicketStart)
                                {
                                    var splited = answer.Trim(STX, ETX).Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (splited[0] == "PS" && _config.Interface == "BestBar")
                                    {
                                        _clientMqtt.Publish(_config.PublicTopic,
                                            splited.Contains("AN")
                                                ? Encoding.UTF8.GetBytes(answer.Replace("|AN", "|AS").Trim(STX, ETX))
                                                : Encoding.UTF8.GetBytes(answer.Trim(STX, ETX)),
                                            MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
                                    }


                                }
                                else
                                {
                                    //send answer to tcp
                                    if (HotelListener.Clients.Count > 0)
                                    {
                                        HotelListener.Clients[0].SendData($"{answer}{ETX}");
                                    }


                                }
                                break;
                            }
                        default:
                            {
                                //return answer to TCP and Mqtt
                                _clientMqtt.Publish(_config.PublicTopic, Encoding.UTF8.GetBytes(answer.Trim(STX, ETX)), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
                                if (HotelListener.Clients.Count > 0)
                                {
                                    foreach (TcpSocket client in HotelListener.Clients)
                                    {
                                        client.SendData($"{answer}{ETX}");
                                    }
                                }

                                break;
                            }
                    }
                    
                    Console.WriteLine($"{answer}: {command.Command}");
                }

            }


        }

        private static bool _fiasConnectionEstablished;
        private static void HotelPmsClient_Connected()
        {
            switch (_config.Interface)
            {
                case "BestBar":
                    {
                        _prot = new BestBar();
                        break;
                    }
                case "Homi":
                    {
                        _prot = new FiasTcp();
                        break;
                    }
                default:
                    {
                        _prot = new FiasTcp();
                        break;
                    }
            }
            Console.WriteLine("Client Connected");
            Thread.Sleep(5100);
            _fiasConnectionEstablished = false;
            if (!_fiasConnectionEstablished)
            {
                Console.WriteLine($"Send to the PMS: {_prot.GetInitRequestString()}");
                HotelPmsClient.SendData(_prot.GetInitRequestString());
            }

        }

        private static void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {

            var msg = Encoding.UTF8.GetString(e.Message);
            var s = msg.Trim(STX, ETX).Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (s[0] == "PS" && _config.Interface == "BestBar")
            {
                var fias = new FiasTcp();
                var obj = fias.ParceBilingString(msg);
                msg = _prot.MakeBillingString(obj);
            }
            if (HotelPmsClient.IsConnected)
            {
                HotelPmsClient.SendData($"{STX}{msg}{ETX}");
            }
            Console.WriteLine($"Received From MQTT (topic:{e.Topic}): {Encoding.UTF8.GetString(e.Message)} and send to PMS: <STX>{msg.Trim(new char[] { ETX, STX })}<ETX>, clientId={_clientMqtt.ClientId}");
        }

        private static void Client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {

            //throw new NotImplementedException();
            Console.WriteLine($"Subscribed To Mqtt Broker {_clientMqtt.WillTopic}");

            _clientMqtt.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
            _clientMqtt.MqttMsgPublished += _client_MqttMsgPublished;
            _clientMqtt.MqttMsgUnsubscribed += _client_MqttMsgUnsubscribed;

        }

        private static void _client_MqttMsgUnsubscribed(object sender, MqttMsgUnsubscribedEventArgs e)
        {
            Console.WriteLine("Unsubscribe");


        }

        private static void _client_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
            //throw new NotImplementedException();

            Console.WriteLine("Mesage sent, messageId:{0}", e.MessageId);
        }

        //private static void DemandWorked(Task task)
        //{
        //    task.Wait();


        //}

        private static IPAddress LocalIpAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }


    }
}
