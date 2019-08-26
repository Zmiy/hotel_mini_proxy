using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using hotel_mini_proxy.mail;
using hotel_mini_proxy.PmsInterface;
using hotel_mini_proxy.Tools;
using NLog;
using TcpLibrary;
using uPLibrary.Networking.M2Mqtt.Messages;
using static hotel_mini_proxy.Tools.ChrOperation;

namespace hotel_mini_proxy.pmsRoutine
{
    static class PmsRoutine
    {

        private static readonly TcpClient HotelPmsClient = new TcpClient();
        private static readonly TcpServer HotelListener = new TcpServer();
        //async connect to PMS - try until connect
        private static void TryConnect2Pms()
        {

            Program.Logger.Info($"Try connect to the PMS of the hotel: {Program.Config.HotelHost}:{Program.Config.HotelPort}");
            int atempt = 0;
            while (!HotelPmsClient.IsConnected)
            {
                Program.Logger.Trace($"TCP: pms client Try to connect ... {++atempt}");
                HotelPmsClient.Connect(Program.Config.HotelHost, Program.Config.HotelPort);
                Thread.Sleep(10 * 1000);
            }
        }

        public static void Connect2Pms()
        {
            HotelPmsClient.Connected += HotelPmsClient_Connected;
            HotelPmsClient.DataArrival += HotelPmsClient_DataArrival;
            HotelPmsClient.Disconnect += HotelPmsClient_Disconnect;
            Task tsk = new Task(TryConnect2Pms);
            tsk.Start();
            //Task.WaitAll(tsk);
            //await Task.Run(() => TryConnect2Pms());
        }

        public static void HotelPmsClient_Disconnect()
        {
            Program.Logger.Warn("A connect with the hotel's PMS had lost. Try to reconnect");
            var mail = new Smtpmail.SendingMail(Program.Config.SendTo, "A connect with the hotel's PMS had lost.")
            {
                Subj = "Connectin with PMS was dropped"
            };

            mail.SendMail();
            Connect2Pms();
        }

        public static void _hotelListener_Connected(TcpSocket client)
        {
            Program.Logger.Trace("Hotel Listener: client ({0}) connected", client.Tag);
        }

        public static void _hotelListener_DataArrival(TcpSocket client, long available)
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
            Program.Logger.Info($"Received from minibar client: {logMessage}");
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

        public static void HotelPmsClient_DataArrival(long available)
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
            Program.Logger.Trace($"Received form the hotel's PMS: {s}");
            ParsePmsAnswer(s);
        }

        private static void ParsePmsAnswer(string answerMsg)
        {

            string[] answers = answerMsg.Split(new char[] { ETX }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                foreach (var answer in answers)
                {
                    List<ParserResult> results = Program.Prot.Parcer($"{answer}{ETX}");
                    foreach (var command in results)
                    {
                        Program.Logger.Trace($"Pms client received:{answer}: {command.Command}");
                        switch (command.Command)
                        {
                            case Command.Init:
                                {
                                    var initStrings = Program.Prot.InitString.ToString().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                    foreach (var currentInitStr in initStrings)
                                    {
                                        HotelPmsClient.SendData(currentInitStr);
                                        Program.Logger.Trace($"Current init string: <STX>{currentInitStr.TrimStart(STX).TrimEnd(ETX)}<ETX>");
                                    }

                                    break;
                                }
                            case Command.AsOk:
                            case Command.AsNg:
                                {
                                    if (command.Ticket >= Program.Config.MqttClientTicketStart)
                                    {
                                        var splited = answer.Trim(STX, ETX).Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                                        //int ticket = command.Ticket - _config.MqttClientTicketStart ?? 0;
                                        string answ = answer.Trim(STX, ETX);
                                        if (splited[0] == "PA" && Program.Config.Interface == "BestBar")
                                        {
                                            answ = answer.Contains("AN") ? answer.Replace("|AN", "|AS") : answ;
                                        }
                                        Program._clientMqtt.Publish(Program.Config.PublicTopic, Encoding.UTF8.GetBytes(answ), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
                                        Program.Logger.Trace($"Sent PS's answer: {answ} to mqtt");
                                    }
                                    else
                                    {
                                        //send answer to tcp
                                        if (HotelListener.Clients.Count > 0)
                                        {
                                            HotelListener.Clients[0].SendData($"{answer}{ETX}");
                                            Program.Logger.Trace($"Sent PS's answer:{answer} to minibar client");
                                        }


                                    }
                                    break;
                                }
                            default:
                                {//return answer to TCP and Mqtt
                                    Program._clientMqtt.Publish(Program.Config.PublicTopic, Encoding.UTF8.GetBytes(answer.Trim(STX, ETX)), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
                                    Program.Logger.Trace($"Sent answer: {answer} to mqtt");
                                    if (HotelListener.Clients.Count > 0)
                                    {
                                        foreach (TcpSocket client in HotelListener.Clients)
                                        {
                                            client.SendData($"{answer}{ETX}");
                                            Program.Logger.Trace($"Sent answer: {answer} to minibar client");
                                        }
                                    }
                                    break;
                                }
                        }

                    }

                }
            }
            catch (Exception ex)
            {

                Program.Logger.Error($"Error resolve PMS's answer: {ex}");
            }


        }

        private static bool _fiasConnectionEstablished;
        public static void HotelPmsClient_Connected()
        {
            switch (Program.Config.Interface)
            {
                case "BestBar":
                    {
                        Program.Prot = new BestBar();
                        break;
                    }
                case "Homi":
                    {
                        Program.Prot = new FiasTcp();
                        break;
                    }
                default:
                    {
                        Program.Prot = new FiasTcp();
                        break;
                    }
            }
            //Console.WriteLine("Client Connected");
            Thread.Sleep(5100);
            _fiasConnectionEstablished = false;
            if (!_fiasConnectionEstablished)
            {
                Program.Logger.Trace($"Send to the PMS: {Program.Prot.GetInitRequestString()}");
                HotelPmsClient.SendData(Program.Prot.GetInitRequestString());
            }

        }

    }
}
