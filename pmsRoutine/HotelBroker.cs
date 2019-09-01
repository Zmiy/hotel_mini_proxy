using System;
using System.Net;
using hotel_mini_proxy.PmsInterface;
using NLog;
using TcpLibrary;
using static hotel_mini_proxy.Tools.ChrOperation;

namespace hotel_mini_proxy.pmsRoutine
{
    public class SendDataToPmsEventArgs : EventArgs
    {
        public string Message { get; internal set; }
        public string TypeOfMessage { get; internal set; }
    }

    class HotelBroker
    {
        //private readonly Logger _logger;
        private readonly TcpServer _hotelListener = new TcpServer();
        private readonly Config _config;
        private Protocol _prot;
        public delegate void SendDataToPmsEventHandler(object sender, SendDataToPmsEventArgs e);
        private static readonly Logger BillingLogger = LogManager.GetLogger("Billing Hotel Broker");
        private static readonly Logger HotelLogger = LogManager.GetLogger("Hotel Broker");
        public event SendDataToPmsEventHandler MessageForPms;

        public HotelBroker(Config config, Logger logger, Protocol protocol)
        {
            _config = config;
            //_logger = logger;
            _prot = protocol;
        }

        public void ListenHotelRequests()
        {
            _hotelListener.DataArrival += _hotelListener_DataArrival;
            _hotelListener.Connected += _hotelListener_Connected;
            IPAddress ipListener = IPAddress.Any;//LocalIpAddress();
            _hotelListener.Port(ipListener, _config.ListenerPort);
            _hotelListener.StartListen();

            HotelLogger.Info("Start listening on {0}:{1}", ipListener, _config.ListenerPort);
        }

        private void _hotelListener_Connected(TcpSocket client)
        {
            HotelLogger.Trace("Hotel Listener: client ({0}) connected", client.Tag);
        }

        private void _hotelListener_DataArrival(TcpSocket client, long available)
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

            var logMessage = (trame.Length == 1 & trame.Equals("6")) ? "<Ask>" : $"<STX>{trame.Trim(new char[] { STX, ETX })}<ETX>";
            HotelLogger.Info($" Received from minibar client: {logMessage}");
            ParseMessageFromHotelSide(trame);
        }

        private string Fdate()
        {
            return DateTime.Now.ToString("yyyyMMdd");
        }
        private string Ftime()
        {
            return DateTime.Now.ToString("hhmmss");
        }

        public void SendToHotel(string message)
        {
            if (_hotelListener.Clients.Count > 0 /*&& _hotelListener.IsListening*/)
            {
                if (message.Contains("PA"))
                {
                    BillingLogger.Info($"Sent PA's answer <STX>{message.Trim(STX, ETX)}<ETX> from the PMS to the minibar's client");
                }
                HotelLogger.Trace($"Sent a PMS's message:{message} to the minibar client");
                _hotelListener.Clients[0].SendData(message);

            }

        }

        private void SendToHotel(string message, string descrMessage2Pms)
        {
            if (_hotelListener.Clients.Count > 0)
            {

                _hotelListener.Clients[0].SendData(message);
                HotelLogger.Info($"Sent {descrMessage2Pms}'s message for the hotel: {message} ");
            }

        }
        /// <summary>
        /// fire event for send message to the pms
        /// </summary>
        /// <param name="message2Pms">message to pms</param>
        /// <param name="typeMessage4Pms">describe of type of message</param>
        private void FirePmsMessage(string message2Pms, string typeMessage4Pms)
        {
            if (MessageForPms != null)
            {
                HotelLogger.Info($"Fire {typeMessage4Pms}'s message for the PMS: {message2Pms} ");
                SendDataToPmsEventArgs e = new SendDataToPmsEventArgs()
                {
                    Message = message2Pms,
                    TypeOfMessage = typeMessage4Pms
                };
                MessageForPms(this, e);
            }
        }



        /// <summary>
        /// Parse message from a hotel side, suitable answer to back or forward message to the PMS
        /// </summary>
        /// <param name="mess"></param>
        private void ParseMessageFromHotelSide(string mess)
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
                        SendToHotel($"{STX}LS|DA{Fdate()}|TI{Ftime()}|{ETX}", s[0]);

                        break;
                    }

                case "LA":
                    {
                        SendToHotel($"{STX}LA|DA{Fdate()}|TI{Ftime()}|{ETX}", s[0]);
                        break;
                    }

                case "PS":
                    {
                        BillingLogger.Info($"Sending bill to the PMS:{mess}");
                        FirePmsMessage(mess, s[0]);
                        break;
                    }

                case "LE":
                    {
                        break;
                    }
            }
        }
    }
}
