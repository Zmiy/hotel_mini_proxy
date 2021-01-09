using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using hotel_mini_proxy.pmsRoutine;

namespace hotel_mini_proxy.SmartThingsProtocol
{
    public class MessageToPmsEventArgs : EventArgs
    {
        public string Message { get; set; }
    }

    public abstract class SmartThing
    {
        public delegate void MessageToPmsHandler(object sender, MessageToPmsEventArgs e);

        public abstract event MessageToPmsHandler SmartThingToPms;
        public abstract void Connect2SmartProtocol();
        public abstract void SendToMqtt(string answer);
    }
}
