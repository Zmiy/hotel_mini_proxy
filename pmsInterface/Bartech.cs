
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static hotel_mini_proxy.Tools.ChrOperation;

namespace hotel_mini_proxy.PmsInterface
{
    class Bartech : Protocol
    {
        public override string GetInitRequestString()
        {
            throw new NotImplementedException();
        }

        public override string MakeBillingString(InvoiceObject obj)
        {
            return $"{STX}{BuildBillingString(obj)}{ETX}";
        }
        public InvoiceObject ParseBillingString(string str2Parse)
        {
            InvoiceObject result = new InvoiceObject
            {
                roomN = str2Parse.Substring(5, 6),
                productName = str2Parse.Substring(31,15),
                price = str2Parse.Substring(47, 6),
                @group = "1",
                qty = "1",
                tva = "0",
                ticket = Convert.ToInt32(str2Parse.Substring(54, 4))
            };
            //Dictionary<string, string> parsedStr = (str2Parse.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)).Where(str => str.Length >= 2).ToDictionary(str => str.Substring(0, 2).ToUpper(), str => str.Substring(2).ToUpper());
            return result;
        }
        public override List<ParserResult> Parser(string str)
        {
            var result = new List<ParserResult>();
            var currResult = new ParserResult();
            str = str.TrimStart(STX).TrimEnd(ETX);
            switch (str[0])
            {
                case '6':
                    {
                        currResult.Command = Command.Asc;
                        result.Add(currResult);
                        break;
                    }
                case '3':
                    {
                        switch (str[1])
                        {
                            case '1':
                                {
                                    currResult.RoomN = str.Substring(2, 7);
                                    currResult.Command = Command.CheckOut;
                                    result.Add(currResult);
                                    break;
                                }

                            case '2':
                                {
                                    currResult.RoomN = str.Substring(2, 7);
                                    currResult.Command = Command.CheckIn;
                                    result.Add(currResult);
                                    break;
                                }
                            case '3':
                                {
                                    currResult.RoomN = str.Substring(2, 7);
                                    currResult.Command = Command.Lock;
                                    result.Add(currResult);
                                    break;
                                }
                            case '4':
                                {
                                    currResult.RoomN = str.Substring(2, 7);
                                    currResult.Command = Command.Unlock;
                                    result.Add(currResult);
                                    break;
                                }
                            case '5':
                                {
                                    currResult.RoomN = str.Substring(2, 7);
                                    currResult.Command = Command.CheckInLock;
                                    result.Add(currResult);
                                    break;
                                }
                            case '6':
                                {
                                    currResult.Command = Command.NightAudit;
                                    result.Add(currResult);
                                    break;
                                }

                        }

                        break;
                    }
            }

            return result;
        }
        private enum TransactionCode
        {
            Invoice = 0,
            Synchronization = 14,
            Asc = 6,
            RoomStatus = 98,
            CheckOutLocked = 31,
            CheckInUnlocked = 32,
            LockedNoCredit = 33,
            UnlockedCredit = 34,
            CheckInLocked = 35,
            CloseDay = 36
        }
        private static string BuildBillingString(InvoiceObject obj)
        {
            var bill = $"  {TransactionCode.Invoice,2:00} {obj.roomN.PadLeft(6, '0')} {BDate()} {BTime()}  {obj.tva.PadLeft(2,'0')}  {obj.qty, 1} {obj.productName, -15} " +
                $"{obj.price.PadLeft(6,'0')} {obj.ticket, 4:0000}";
            return bill;
        }
        private static string BDate()
        {
            return DateTime.Now.ToString("ddMMyy");
        }

        private static string BTime()
        {
            return DateTime.Now.ToString("HHmm");
        }
    }
}
