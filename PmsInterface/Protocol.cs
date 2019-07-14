using System.Collections.Generic;
using System.Text;
using Interfaces;

namespace hotel_mini_proxy.PmsInterface
{
    public struct InvoiceObject
    {
        public string roomN;
        public string productName;
        public string qty;
        public string price;
        public string tva;
        public int ticket;
        public string group;

    }

    public enum Command
    {
        UnKnown = 0,
        Init,
        CheckIn,
        CheckOut,
        CheckInLock,
        Lock,
        Unlock,
        Move,
        NightAuditStart,
        NightAuditEnd,
        NightAudit,
        AreYouThere,
        AsOk,
        AsNg,

    }

    public sealed class ParserResult
    {
        public string RoomN { get; set; }
        public Command Command { get; set; }
        public int? Ticket { get; set; }

        public ParserResult()
        {
            this.RoomN = string.Empty;
            this.Command = new Command();
            this.Ticket = null;
        }
    }

    public abstract class Protocol
    {
        protected IInitNecessity InitRoutine;

        public bool IsInitExist => InitRoutine.IsInitExist;

        public StringBuilder InitString => InitRoutine.GetInit();
        public abstract string GetInitRequestString();
        public abstract string MakeBillingString(InvoiceObject obj);
        public abstract List<ParserResult> Parcer(string str);
    }
}
