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
using NLog;
using NLog.Fluent;
using NLog.LayoutRenderers;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using TcpLibrary;
using TcpClient = TcpLibrary.TcpClient;
using static hotel_mini_proxy.Tools.ChrOperation;

namespace hotel_mini_proxy
{
    internal static class Program
    {
        private static MqttClient _clientMqtt;
        private static readonly TcpClient HotelPmsClient = new TcpClient();
        private static readonly TcpServer HotelListener = new TcpServer();
        private static Config _config;
        private static Protocol _prot;
        private static X509Certificate2 _clientCert;
        private static X509Certificate _caCert;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        //async connect to PMS - try until connect
        private static void TryConnect2Pms()
        {

            Logger.Info($"Try connect to the PMS of the hotel: {_config.HotelHost}:{_config.HotelPort}");
            int atempt = 0;
            while (!HotelPmsClient.IsConnected)
            {
                Logger.Trace($"TCP: pms client Try to connect ... {++atempt}");
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

                    _clientMqtt = new MqttClient(_config.MqttHost, _config.MqttPort, _config.UseSsl, _caCert, _clientCert, MqttSslProtocols.TLSv1_2);

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
            _clientMqtt.Unsubscribe(new[] { _config.SubscribeTopic });
            while (!_clientMqtt.IsConnected)
            {
                try
                {
                    Logger.Trace($"MQTT Try to connect... {++atempt}, { _config.UserName},{_config.Password}");
                    Thread.Sleep(15 * 1000);
                    //connect to MQTT by SSL or not by Config
                    var code = _config.UseAutorization ? _clientMqtt.Connect(clientId + atempt, _config.UserName, _config.Password, true, 60) : _clientMqtt.Connect(clientId + atempt, null, null, true, 60);

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
            Logger.Trace("Subscribing to the topic: {0} ", _config.SubscribeTopic);
            var msgId = _clientMqtt.Subscribe(new[] { _config.SubscribeTopic }, new[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            Logger.Trace($"Client mqtt subscribed with id {msgId}");
        }

        static void Main(string[] args)
        {

            Logger.Info("------------Started------------");
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
            //Console.WriteLine("Start listening on {0}:{1}", ipListener, _config.ListenerPort);
            Logger.Info("Start listening on {0}:{1}", ipListener, _config.ListenerPort);

            //MQTT connect
            //_clientCert = new X509Certificate2("cert/client2048.pfx", "tkphbv#1");

            _clientCert = new X509Certificate2("cert/client.crt");
            _caCert = X509Certificate.CreateFromCertFile("cert/server.crt");
            //ServicePointManager.ServerCertificateValidationCallback += (s,  certificate, chain, sslPolicyErrors);//=> true;
            //ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
            //ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);
            _clientMqtt = new MqttClient(_config.MqttHost, _config.MqttPort, _config.UseSsl, _caCert, _clientCert, MqttSslProtocols.TLSv1_2);

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


        private static void HotelPmsClient_Disconnect()
        {
            Logger.Warn("A connect with the hotel's PMS had lost. Try to reconnect");
            Connect2Pms();
        }

        private static void _hotelListener_Connected(TcpSocket client)
        {
            //Console.WriteLine("Hotel Listener: client ({0}) connected", client.Tag);
            Logger.Trace("Hotel Listener: client ({0}) connected", client.Tag);
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
            //Console.WriteLine($"Received from minibar client: {trame}");
            var logMessage = (trame.Length == 1 & trame.Equals("6")) ? "<Ask>" : $"<STX>{trame.Trim(new char[] { STX, ETX })}<ETX>";
            Logger.Info($"Received from minibar client: {logMessage}");
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
            Logger.Trace($"Received form the hotel's PMS: {s}");
            ParsePmsAnswer(s);
        }
        private static void ParsePmsAnswer(string answerMsg)
        {

            string[] answers = answerMsg.Split(new char[] { ETX }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var answer in answers)
            {

                List<ParserResult> results = _prot.Parcer($"{answer}{ETX}");
                foreach (var command in results)
                {
                    Logger.Trace($"Pms client received:{answer}: {command.Command}");
                    switch (command.Command)
                    {
                        case Command.Init:
                            {
                                var initStrings = _prot.InitString.ToString().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var currentInitStr in initStrings)
                                {
                                    HotelPmsClient.SendData(currentInitStr);

                                    //Console.WriteLine($"Current init string: <STX>{currentInitStr.TrimStart(STX).TrimEnd(ETX)}<ETX>");
                                    Logger.Trace($"Current init string: <STX>{currentInitStr.TrimStart(STX).TrimEnd(ETX)}<ETX>");
                                }

                                break;
                            }
                        case Command.AsOk:
                        case Command.AsNg:
                            {
                                if (command.Ticket >= _config.MqttClientTicketStart)
                                {
                                    var splited = answer.Trim(STX, ETX).Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                                    //int ticket = command.Ticket - _config.MqttClientTicketStart ?? 0;
                                    string answ = answer.Trim(STX, ETX);
                                    if (splited[0] == "PA" && _config.Interface == "BestBar")
                                    {
                                        answ = answer.Contains("AN") ? answer.Replace("|AN", "|AS") : answ;
                                    }
                                    _clientMqtt.Publish(_config.PublicTopic, Encoding.UTF8.GetBytes(answ), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
                                    Logger.Trace($"Sent PS's answer: {answ} to mqtt");

                                }
                                else
                                {
                                    //send answer to tcp
                                    if (HotelListener.Clients.Count > 0)
                                    {
                                        HotelListener.Clients[0].SendData($"{answer}{ETX}");
                                        Logger.Trace($"Sent PS's answer:{answer} to minibar client");
                                    }


                                }
                                break;
                            }
                        default:
                            {
                                //return answer to TCP and Mqtt
                                _clientMqtt.Publish(_config.PublicTopic, Encoding.UTF8.GetBytes(answer.Trim(STX, ETX)), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
                                Logger.Trace($"Sent answer: {answer}: to mqtt");
                                if (HotelListener.Clients.Count > 0)
                                {
                                    foreach (TcpSocket client in HotelListener.Clients)
                                    {
                                        client.SendData($"{answer}{ETX}");
                                        Logger.Trace($"Sent answer: {answer} to minibar client");
                                    }
                                }

                                break;
                            }
                    }

                    //Console.WriteLine($"Pms client received:{answer}: {command.Command}");

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
            //Console.WriteLine("Client Connected");
            Thread.Sleep(5100);
            _fiasConnectionEstablished = false;
            if (!_fiasConnectionEstablished)
            {
                //Console.WriteLine($"Send to the PMS: {_prot.GetInitRequestString()}");
                Logger.Trace($"Send to the PMS: {_prot.GetInitRequestString()}");
                HotelPmsClient.SendData(_prot.GetInitRequestString());
            }

        }

        private static void _clientMqtt_ConnectionClosed(object sender, EventArgs e)
        {
            //Console.WriteLine("--------MQTT Connection closed-----------");
            Logger.Warn("--------MQTT Connection closed-----------");
            _clientMqtt.Unsubscribe(new string[] { _config.SubscribeTopic });
            _clientMqtt = null;
            Connect2Mqtt();
        }
        private static void _clientMqtt_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                var msg = Encoding.UTF8.GetString(e.Message);
                var s = msg.Trim(STX, ETX).Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (s[0] == "PS" && _config.Interface == "BestBar")
                {
                    var fias = new FiasTcp();
                    var obj = fias.ParceBilingString(msg);
                    //obj.ticket += _config.MqttClientTicketStart;
                    msg = _prot.MakeBillingString(obj);
                }
                if (s[0] == "LA")
                {
                    _clientMqtt.Publish(_config.PublicTopic, Encoding.UTF8.GetBytes($"{ETX}{msg}{STX}"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
                    Logger.Trace($"Sent to Mqtt LA's answer: <ETX>{msg}<ETX>");
                }
                if (HotelPmsClient.IsConnected)
                {
                    HotelPmsClient.SendData($"{STX}{msg}{ETX}");
                }
                //Console.WriteLine($"Received From MQTT (topic:{e.Topic}): {Encoding.UTF8.GetString(e.Message)} and send to PMS: <STX>{msg.Trim(new char[] { ETX, STX })}<ETX>, clientId={_clientMqtt.ClientId}");
                Logger.Trace($"Received From MQTT (topic:{e.Topic}): {Encoding.UTF8.GetString(e.Message)}\n\t\tSend to PMS: <STX>{msg.Trim(new char[] { ETX, STX })}<ETX>, clientId={_clientMqtt.ClientId}");

            }
            catch (Exception ex)
            {
                Logger.Error($" Error parse incoming MQTT message {ex.Message}\n\t\t {ex.Data}");
            }


        }

        private static void _clientMqtt_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {

            //Console.WriteLine($"Subscribed To Mqtt Broker {_clientMqtt.WillTopic}");
            Logger.Trace($"Subscribed To Mqtt Broker {_clientMqtt.WillTopic}, {e.MessageId}");
            _clientMqtt.MqttMsgPublishReceived += _clientMqtt_MqttMsgPublishReceived;
            _clientMqtt.MqttMsgPublished += _clientMqtt_MqttMsgPublished;
            _clientMqtt.MqttMsgUnsubscribed += _clientMqtt_MqttMsgUnsubscribed;

        }

        private static void _clientMqtt_MqttMsgUnsubscribed(object sender, MqttMsgUnsubscribedEventArgs e)
        {
            //Console.WriteLine("Unsubscribe");
            Logger.Warn("MQTT client unsubscribed");

        }

        private static void _clientMqtt_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
            //Console.WriteLine("MQTT client: Mesage sent, messageId:{0}", e.MessageId);
            Logger.Trace("MQTT client: Mesage sent, messageId:{0}", e.MessageId);
        }


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
