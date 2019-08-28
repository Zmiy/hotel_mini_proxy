using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using hotel_mini_proxy.mail;
using hotel_mini_proxy.PmsInterface;
using hotel_mini_proxy.Tools;
using NLog;
using TcpLibrary;
using static hotel_mini_proxy.Tools.ChrOperation;

namespace hotel_mini_proxy.pmsRoutine
{
    public class AnswerEventArgs : EventArgs
    {
        public string Answer { get; set; }
        public string TypeOfAnswer { get; set; }
    }

    class PmsBroker
    {
        public delegate void AnswerForBrockerHandler(object sender, AnswerEventArgs e);

        public event AnswerForBrockerHandler HotelAnswer;
        public event AnswerForBrockerHandler MqttAnswer;
        private readonly Logger _logger;
        private readonly TcpClient _hotelPmsClient = new TcpClient();
        private readonly Config _config;
        private readonly Protocol _prot;

        private static readonly Logger LoggerBilling = LogManager.GetLogger("Billing");

        public PmsBroker(Config config, Logger logger, Protocol protocol)
        {
            _config = config;
            _logger = logger;
            _prot = protocol;
        }

        private void TryConnect2Pms()
        {

            _logger.Info($"Try connect to the PMS of the hotel: {_config.HotelHost}:{_config.HotelPort}");
            int atempt = 0;
            while (!_hotelPmsClient.IsConnected)
            {
                _logger.Trace($"TCP: pms client Try to connect ... {++atempt}");
                _hotelPmsClient.Connect(_config.HotelHost, _config.HotelPort);
                Thread.Sleep(10 * 1000);
            }
        }

        public void Connect2Pms()
        {
            _hotelPmsClient.Connected += _hotelPmsClient_Connected;
            _hotelPmsClient.DataArrival += _hotelPmsClient_DataArrival;
            _hotelPmsClient.Disconnect += _hotelPmsClient_Disconnect;
            Task tsk = new Task(TryConnect2Pms);
            tsk.Start();

        }

        private bool _fiasConnectionEstablished;
        private void _hotelPmsClient_Connected()
        {
            Thread.Sleep(5100);
            _fiasConnectionEstablished = false;
            if (!_fiasConnectionEstablished)
            {
                _logger.Trace($"Send to the PMS: {_prot.GetInitRequestString()}");
                _hotelPmsClient.SendData(_prot.GetInitRequestString());
            }

        }
        private void _hotelPmsClient_DataArrival(long available)
        {
            string s = "";

            for (long i = 1L; i < available; i++)
            {
                var c = Convert.ToInt32(_hotelPmsClient.GetData(1).GetValue(0));
                if (c != 0)
                {
                    s += ChrOperation.Chr(c);
                }
            }
            _logger.Trace($"Received form the hotel's PMS: {s}");
            ParsePmsAnswer(s);
        }

        private void ParsePmsAnswer(string answerMsg)
        {

            string[] answers = answerMsg.Split(new char[] { ETX }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                foreach (var answer in answers)
                {
                    List<ParserResult> results = _prot.Parcer($"{answer}{ETX}");
                    foreach (var command in results)
                    {
                        _logger.Trace($"Pms client received:{answer}: {command.Command}");
                        switch (command.Command)
                        {
                            case Command.Init:
                                {
                                    var initStrings = _prot.InitString.ToString().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                    foreach (var currentInitStr in initStrings)
                                    {
                                        _hotelPmsClient.SendData(currentInitStr);
                                        _logger.Trace($"Current init string: <STX>{currentInitStr.TrimStart(STX).TrimEnd(ETX)}<ETX>");
                                    }

                                    break;
                                }
                            case Command.AsOk:
                            case Command.AsNg:
                                {
                                    if (command.Ticket >= _config.MqttClientTicketStart)
                                    {
                                        LoggerBilling.Info($"PE answer for the MQTT's broker: {answer}");
                                        var splited = answer.Trim(STX, ETX).Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                                        //int ticket = command.Ticket - _config.MqttClientTicketStart ?? 0;
                                        string answ = answer.Trim(STX, ETX);
                                        if (splited[0] == "PA" && _config.Interface == "BestBar")
                                        {
                                            answ = answer.Contains("AN") ? answ.Replace("|AN", "|AS") : answ;
                                        }
                                        if (MqttAnswer != null)
                                        {
                                            LoggerBilling.Trace($"Fire {command.Command}'s answer: {$"{STX}|{answ}{ETX}"} to mqtt");
                                            _logger.Trace($"Fire {command.Command}'s answer: {$"{STX}|{answ}{ETX}"} to mqtt");
                                            AnswerEventArgs e = new AnswerEventArgs()
                                            {
                                                Answer = $"{STX}|{answ}{ETX}",
                                                TypeOfAnswer = command.Command.ToString()
                                            };
                                            MqttAnswer(this, e);
                                        }

                                        //Program._clientMqtt.Publish(_config.PublicTopic, Encoding.UTF8.GetBytes(answ), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);

                                    }
                                    else
                                    {
                                        if (HotelAnswer != null)
                                        {
                                            LoggerBilling.Info($"Fire {command.Command}'s answer:{$"{answer}{ETX}"} to minibar client");
                                            _logger.Trace($"Fire {command.Command}'s answer:{$"{answer}{ETX}"} to minibar client");
                                            AnswerEventArgs e = new AnswerEventArgs()
                                            {
                                                Answer = $"{answer}{ETX}",
                                                TypeOfAnswer = command.Command.ToString()
                                            };
                                            HotelAnswer(this, e);

                                        }
                                    }
                                    break;
                                }
                            default:
                                {//return answer to TCP and Mqtt
                                    if (HotelAnswer != null)
                                    {
                                        _logger.Trace($"Fire {command.Command}'s answer:{$"{answer}{ETX}"} to minibar client");
                                        AnswerEventArgs e = new AnswerEventArgs()
                                        {
                                            Answer = $"{answer}{ETX}",
                                            TypeOfAnswer = command.Command.ToString()
                                        };
                                        HotelAnswer(this, e);
                                    }
                                    if (MqttAnswer != null)
                                    {
                                        _logger.Trace($"Fire {command.Command}'s answer: {$"{answer}{ETX}"} to mqtt");
                                        AnswerEventArgs e = new AnswerEventArgs()
                                        {
                                            Answer = $"{answer}{ETX}",
                                            TypeOfAnswer = command.Command.ToString()
                                        };
                                        MqttAnswer(this, e);
                                    }
                                    break;
                                }
                        }

                    }

                }
            }
            catch (Exception ex)
            {

                _logger.Error($"Error resolve PMS's answer: {ex}");
            }


        }

        public void SendToPms(string message)
        {
            _hotelPmsClient.SendData(message);
        }

        private void _hotelPmsClient_Disconnect()
        {
            _logger.Warn("A connect with the hotel's PMS had lost. Try to reconnect");
            var mail = new Smtpmail.SendingMail(Program.Config.SendTo, "A connect with the hotel's PMS had lost.")
            {
                Subj = "Connectin with PMS was dropped"
            };

            mail.SendMail();
            Connect2Pms();
        }
    }
}
