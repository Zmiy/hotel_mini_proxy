using System;
using System.Collections.Generic;
using System.Linq;
using hotel_mini_proxy.Tools;
using static hotel_mini_proxy.Tools.ChrOperation;

namespace hotel_mini_proxy.PmsInterface
{
    public class BestBar : Protocol
    {
        public BestBar()
        {
            InitRoutine = new BestBarInit();
        }

        private static string Fdate()
        {
            return DateTime.Now.ToString("yyMMdd");
        }



        private static string Ftime()
        {
            return DateTime.Now.ToString("HHmmss");
        }

        //<STX>PS|RN2215|PTC|P#1|TA1538|DA180323|TI104918|CTGardena|<ETX>
        private static string BuildBillingString(InvoiceObject obj)
        {
            var bill =
                $"PS|DA{Fdate()}|PTC|RN{obj.roomN}|P#{obj.ticket}|TA{obj.price}|TI{Ftime()}|CT{obj.productName}|";
            return bill;
        }


        public override string GetInitRequestString()
        {
            return $"{STX}LS|DA{Fdate()}|TI{Ftime()}|{ETX}";
        }

        public override string MakeBillingString(InvoiceObject obj)
        {
            return $"{STX}{BuildBillingString(obj)}{ETX}";
        }

        public override List<ParserResult> Parser(string str)
        {
            var result = new List<ParserResult>();
            var currResult = new ParserResult();

            str = str.TrimStart(STX).TrimEnd(ETX);
            var parcer = str.Split('|');
            switch (parcer[0])
            {
                case "LS":
                    {
                        currResult.Command = Command.Init;
                        result.Add(currResult);
                        break;
                    }
                case "LA":
                    {
                        currResult.Command = Command.AreYouThere;
                        result.Add(currResult);
                        break;
                    }
                case "GI":
                    {
                        var nopost = NoPostParcer(parcer);
                        var roomN = RoomNumberParser("RN", parcer);
                        if (!roomN.Equals("NOTHING"))
                        {
                            currResult.Command = nopost ? Command.CheckInLock : Command.CheckIn;
                            currResult.RoomN = roomN;
                            result.Add(currResult);
                        }
                        break;
                    }
                case "GO":
                    {
                        var roomN = RoomNumberParser("RN", parcer);
                        if (!roomN.Equals("NOTHING"))
                        {
                            currResult.RoomN = roomN;
                            currResult.Command = Command.CheckOut;
                            result.Add(currResult);
                        }
                        break;
                    }
                case "GC":
                    {
                        var nopost = NoPostParcer(parcer);
                        var newRoomN = RoomNumberParser("RN", parcer);
                        var oldRoomN = RoomNumberParser("RO", parcer);
                        if (!newRoomN.Equals("NOTHING"))
                        {
                            currResult.Command = nopost ? Command.CheckInLock : Command.CheckIn;
                            currResult.RoomN = newRoomN;
                            result.Add(currResult);
                        }
                        if (!newRoomN.Equals("NOTHING"))
                        {
                            currResult = new ParserResult
                            {
                                RoomN = oldRoomN,
                                Command = Command.CheckOut
                            };
                            result.Add(currResult);
                        }
                        if (result.Count == 2)
                        {
                            currResult = new ParserResult
                            {
                                RoomN = newRoomN + "~" + oldRoomN,
                                Command = Command.Move
                            };
                            result.Add(currResult);
                        }
                        break;
                    }
                case "NE":
                    {
                        currResult.Command = Command.NightAuditEnd;
                        result.Add(currResult);
                        break;
                    }

                case "NS":
                    {
                        currResult.Command = Command.NightAuditStart;
                        result.Add(currResult);
                        break;
                    }

                case "PA":
                    {
                        currResult = AnswerParser(parcer);
                        result.Add(currResult);
                        break;
                    }
            }

            return result;
        }
        //Received:GI|RN000402|NPN|MRMU|<>
        //<STX>GI|RN1317 |MRMU|DA180323|TI104921|<ETX>
        private static bool NoPostParcer(IEnumerable<string> parcer)
        {
            var result = false; //Regular checkin
            var resultOfParce = parcer.Where(str => str.ToUpper().Contains("MR")).ToDictionary(str => str.Substring(0, 2).ToUpper(), str => str.Substring(2).ToUpper());

            if (resultOfParce.ContainsKey("MR") && resultOfParce["MR"].Equals("ML")) // check minibar right
            {
                result = true;
            }
            return result;
        }

        private static string RoomNumberParser(string rnro, IEnumerable<string> parcer)
        {
            var result = "NOTHING";
            if (rnro == null) return result;
            var resultOfParce = parcer.Where(str => str.ToUpper().Contains(rnro)).DefaultIfEmpty(defaultValue: $"{rnro}NOTHING").ToDictionary(str => str.Length > 0 ? str.Substring(0, 2).ToUpper() : rnro, str => str.Length > 0 ? str.Substring(2).ToUpper() : "NOTHING");
            if (resultOfParce.Count > 0)
            {
                result = resultOfParce[rnro].TrimStart('0');
            }
            return result;
        }

        private static ParserResult AnswerParser(IEnumerable<string> parser)
        {
            var result = new ParserResult();

            var resultOfParsing =
                parser.Where(str => str.Length >= 2)
                    .ToDictionary(str => str.Substring(0, 2).ToUpper(), str => str.Substring(2).ToUpper());


            if (resultOfParsing.ContainsKey("P#"))
            {
                result.Ticket = TypeParser.Int32TryParse(resultOfParsing["P#"]);
            }
            //if (!resultOfParsing.ContainsKey("AN") || !resultOfParsing.ContainsKey("AS")) return result;
            string answ = resultOfParsing.ContainsKey("AN") ? resultOfParsing["AN"] : resultOfParsing.ContainsKey("AS") ? resultOfParsing["AS"] : "";
            switch (answ)
            {
                case "OK":
                    result.Command = Command.AsOk;
                    break;
                case "NG":
                    result.Command = Command.AsNg;
                    break;
                default:
                    result.Command = Command.UnKnown;
                    break;
            }
            return result;
        }
    }
}
