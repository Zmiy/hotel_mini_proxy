using System;
using System.Collections.Generic;
using System.Linq;
using hotel_mini_proxy.Tools;

namespace hotel_mini_proxy.PmsInterface
{
    public class FiasTcp : Protocol
    {
        private static readonly char Stx = ChrOperation.Chr(2);
        private static readonly char Etx = ChrOperation.Chr(3);
        public FiasTcp()
        {
            InitRoutine = new FiasInit();
        }

        private static string Fdate()
        {
            return DateTime.Now.ToString("MMddyyyy");
        }



        private static string Ftime()
        {
            return DateTime.Now.ToString("HHmmss");
        }

        private static string BuildBillingString(InvoiceObject obj)
        {
            var bill =
                $"PS|DA{Fdate()}|PTC|RN{obj.roomN}|P#{obj.ticket:0000}|TA{obj.price}|TI{Ftime()}|X1{obj.productName}|";
            return bill;
        }

        public string AAA => $"{Stx}LS|DA{Fdate()}|TI{Ftime()}|{Etx}";
        public override string GetInitRequestString()
        {
            return $"{Stx}LS|DA{Fdate()}|TI{Ftime()}|{Etx}";
        }

        public override string MakeBillingString(InvoiceObject obj)
        {
            return $"{Stx}{BuildBillingString(obj)}{Etx}";
        }

        public override List<ParserResult> Parcer(string str)
        {
            var result = new List<ParserResult>();
            var currResult = new ParserResult();

            str = str.TrimStart(Stx).TrimEnd(Etx);
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
        private static bool NoPostParcer(IEnumerable<string> parcer)
        {
            var result = false; //Regular checkin
            var resultOfParce = parcer.Where(str => str.ToUpper().Contains("NP") || str.ToUpper().Contains("MR")).ToDictionary(str => str.Substring(0, 2).ToUpper(), str => str.Substring(2).ToUpper());
            if (resultOfParce.ContainsKey("NP") && resultOfParce["NP"].Equals("Y")) //check if nopost is true
            {
                result = true;
            }

            if (!result && resultOfParce.ContainsKey("MR") && resultOfParce["MR"].Equals("ML")) // check minibar right
            {
                result = true;
            }
            return result;
        }

        private static string RoomNumberParser(string rnro, IEnumerable<string> parcer)
        {
            var result = "NOTHING";
            var resultOfParce = parcer.Where(str => str.ToUpper().Contains(rnro)).DefaultIfEmpty().ToDictionary(str => str.Substring(0, 2).ToUpper(), str => str.Substring(2).ToUpper());
            if (resultOfParce.Count > 0)
            {
                result = resultOfParce[rnro].TrimStart('0');
            }
            return result;
        }

        private static ParserResult AnswerParser(IEnumerable<string> parser)
        {
            var result = new ParserResult();
            //Dim result1 As Dictionary(Of String, String) = (From str In arr Where str.Length >= 2).ToDictionary(Function(str) str.Substring(0, 2), Function(str) s.Substring(2)) 
            var resultOfParsing =
                parser.Where(str => str.Length >= 2)
                    .ToDictionary(str => str.Substring(0, 2).ToUpper(), str => str.Substring(2).ToUpper());


            if (resultOfParsing.ContainsKey("P#"))
            {
                result.Ticket = TypeParser.Int32TryParse(resultOfParsing["P#"]);
            }
            if (!resultOfParsing.ContainsKey("AS")) return result;
            switch (resultOfParsing["AS"])
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
