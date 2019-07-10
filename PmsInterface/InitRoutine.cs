using System;
using System.Text;
using hotel_mini_proxy.Tools;
using Interfaces;

namespace hotel_mini_proxy.PmsInterface
{
    internal class FiasInit : IInitNecessity
    {
        public bool IsInitExist => true;

        public StringBuilder GetInit()
        {
            var init = new StringBuilder();
            var dt = DateTime.Now;
            init.AppendLine(ChrOperation.Chr(2) + "LD|DA" + dt.ToString("MMddyyyy") + "|TI" + dt.ToString("HHmmss") + "|V#1.01|IFMB|" + ChrOperation.Chr(3));
            init.AppendLine(ChrOperation.Chr(2) + "LR|RIGI|FLRNMRSFNP|" + ChrOperation.Chr(3));
            init.AppendLine(ChrOperation.Chr(2) + "LR|RIGO|FLRNSF|" + ChrOperation.Chr(3));
            init.AppendLine(ChrOperation.Chr(2) + "LR|RIGC|FLRNROMRNP|" + ChrOperation.Chr(3));
            init.AppendLine(ChrOperation.Chr(2) + "LR|RIPS|FLDAPTRNP#TATIX1|" + ChrOperation.Chr(3));
            init.AppendLine(ChrOperation.Chr(2) + "LR|RIPA|FLDARNP#TATIAS|" + ChrOperation.Chr(3));
            init.AppendLine(ChrOperation.Chr(2) + "LR|RINS|FLDATI|" + ChrOperation.Chr(3)); //Night audit start
            init.AppendLine(ChrOperation.Chr(2) + "LR|RINE|FLDATI|" + ChrOperation.Chr(3)); //Night audit end
            init.AppendLine(ChrOperation.Chr(2) + "LA|DA" + dt.ToString("MMddyyyy") + "|TI" + dt.ToString("HHmmss") + "|" + ChrOperation.Chr(3));
            return init;

        }
    }

    internal class FiasModefiedInit : IInitNecessity
    {
        public bool IsInitExist
        {
            get { return true; }
        }

        public StringBuilder GetInit()
        {
            var init = new StringBuilder();
            var dt = DateTime.Now;
            init.AppendLine(ChrOperation.Chr(2) + "LD|DA" + dt.ToString("MMddyyyy") + "|TI" + dt.ToString("HHmmss") + "|V#1.01|IFMB|" + ChrOperation.Chr(3));
            init.AppendLine(ChrOperation.Chr(2) + "LR|RIGI|FLRNMRSFNP|" + ChrOperation.Chr(3));
            init.AppendLine(ChrOperation.Chr(2) + "LR|RIGO|FLRNSF|" + ChrOperation.Chr(3));
            init.AppendLine(ChrOperation.Chr(2) + "LR|RIGC|FLRNROMRNP|" + ChrOperation.Chr(3));
            init.AppendLine(ChrOperation.Chr(2) + "LR|RIPS|FLDAPTRNP#TATIX1SO|" + ChrOperation.Chr(3));
            init.AppendLine(ChrOperation.Chr(2) + "LR|RIPA|FLDARNP#TATIAS|" + ChrOperation.Chr(3));
            init.AppendLine(ChrOperation.Chr(2) + "LR|RINS|FLDATI|" + ChrOperation.Chr(3)); //Night audit start
            init.AppendLine(ChrOperation.Chr(2) + "LR|RINE|FLDATI|" + ChrOperation.Chr(3)); //Night audit end
            init.AppendLine(ChrOperation.Chr(2) + "LA|DA" + dt.ToString("MMddyyyy") + "|TI" + dt.ToString("HHmmss") + "|" + ChrOperation.Chr(3));
            return init;

        }
    }



    internal class ProtelInit : IInitNecessity
    {
        public bool IsInitExist
        {
            get { return true; }
        }

        public StringBuilder GetInit()
        {
            var init = new StringBuilder();
            var dt = DateTime.Now;
            init.AppendLine(ChrOperation.Chr(2) + "LD|DA" + dt.ToString("MMddyyyy") + "|TI" + dt.ToString("HHmmss") + "|V#1.01|IFMB|" + ChrOperation.Chr(3));
            init.AppendLine(ChrOperation.Chr(2) + "LR|RIGI|FLRNMRSFNP|" + ChrOperation.Chr(3));
            init.AppendLine(ChrOperation.Chr(2) + "LR|RIGO|FLRNSF|" + ChrOperation.Chr(3));
            init.AppendLine(ChrOperation.Chr(2) + "LR|RIGC|FLRNROMRNP|" + ChrOperation.Chr(3));
            init.AppendLine(ChrOperation.Chr(2) + "LR|RIPS|FLDAPTRNP#TATIX1CTSO|" + ChrOperation.Chr(3));
            init.AppendLine(ChrOperation.Chr(2) + "LR|RIPA|FLDARNP#TATIAS|" + ChrOperation.Chr(3));
            init.AppendLine(ChrOperation.Chr(2) + "LR|RINS|FLDATI|" + ChrOperation.Chr(3)); //Night audit start
            init.AppendLine(ChrOperation.Chr(2) + "LR|RINE|FLDATI|" + ChrOperation.Chr(3)); //Night audit end
            init.AppendLine(ChrOperation.Chr(2) + "LA|DA" + dt.ToString("MMddyyyy") + "|TI" + dt.ToString("HHmmss") + "|" + ChrOperation.Chr(3));
            return init;
        }
    }
}
