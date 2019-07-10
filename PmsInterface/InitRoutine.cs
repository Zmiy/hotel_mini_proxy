using System;
using System.Text;
using static hotel_mini_proxy.Tools.ChrOperation;
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
            init.AppendLine(STX + "LD|DA" + dt.ToString("yyMMdd") + "|TI" + dt.ToString("HHmmss") + "|V#1.01|IFMB|" + ETX);
            init.AppendLine(STX + "LR|RIGI|FLRNMRSFNP|" + ETX);
            init.AppendLine(STX + "LR|RIGO|FLRNSF|" + ETX);
            init.AppendLine(STX + "LR|RIGC|FLRNROMRNP|" + ETX);
            init.AppendLine(STX + "LR|RIPS|FLDAPTRNP#TATIX1|" + ETX);
            init.AppendLine(STX + "LR|RIPA|FLDARNP#TATIAS|" + ETX);
            init.AppendLine(STX + "LR|RINS|FLDATI|" + ETX); //Night audit start
            init.AppendLine(STX + "LR|RINE|FLDATI|" + ETX); //Night audit end
            init.AppendLine(STX + "LA|DA" + dt.ToString("yyMMdd") + "|TI" + dt.ToString("HHmmss") + "|" + ETX);
            return init;

        }
    }

    internal class BestBarInit : IInitNecessity
    {

        public bool IsInitExist => true;

        public StringBuilder GetInit()
        {
            var init = new StringBuilder();
            var dt = DateTime.Now;
            init.AppendLine($"{STX}LD|DA{dt.ToString("yyMMdd")}TI{dt.ToString("HHmmss")}| V#1.00|IFMB|{ETX}");
            init.AppendLine($"{STX}LR|RIGI|FLG#RNMRDATI|{ETX}");
            init.AppendLine($"{STX}LR|RIGO|FLG#RNDATI|{ETX}");
            init.AppendLine($"{STX}LR|RIGC|FLG#RNROMRDATI|{ETX}");
            init.AppendLine($"{STX}LR|RIPS|FLRNPTP#TADATICT|{ETX}");
            init.AppendLine($"{STX}LR|RIPA|FLRNASP#DATI|{ETX}");
            init.AppendLine($"{STX}LA|DA{dt.ToString("yyMMdd")}TI{dt.ToString("HHmmss")}|{ETX}");
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
            init.AppendLine(STX + "LD|DA" + dt.ToString("MMddyyyy") + "|TI" + dt.ToString("HHmmss") + "|V#1.01|IFMB|" + ETX);
            init.AppendLine(STX + "LR|RIGI|FLRNMRSFNP|" + ETX);
            init.AppendLine(STX + "LR|RIGO|FLRNSF|" + ETX);
            init.AppendLine(STX + "LR|RIGC|FLRNROMRNP|" + ETX);
            init.AppendLine(STX + "LR|RIPS|FLDAPTRNP#TATIX1SO|" + ETX);
            init.AppendLine(STX + "LR|RIPA|FLDARNP#TATIAS|" + ETX);
            init.AppendLine(STX + "LR|RINS|FLDATI|" + ETX); //Night audit start
            init.AppendLine(STX + "LR|RINE|FLDATI|" + ETX); //Night audit end
            init.AppendLine(STX + "LA|DA" + dt.ToString("MMddyyyy") + "|TI" + dt.ToString("HHmmss") + "|" + ETX);
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
            init.AppendLine(STX + "LD|DA" + dt.ToString("MMddyyyy") + "|TI" + dt.ToString("HHmmss") + "|V#1.01|IFMB|" + ETX);
            init.AppendLine(STX + "LR|RIGI|FLRNMRSFNP|" + ETX);
            init.AppendLine(STX + "LR|RIGO|FLRNSF|" + ETX);
            init.AppendLine(STX + "LR|RIGC|FLRNROMRNP|" + ETX);
            init.AppendLine(STX + "LR|RIPS|FLDAPTRNP#TATIX1CTSO|" + ETX);
            init.AppendLine(STX + "LR|RIPA|FLDARNP#TATIAS|" + ETX);
            init.AppendLine(STX + "LR|RINS|FLDATI|" + ETX); //Night audit start
            init.AppendLine(STX + "LR|RINE|FLDATI|" + ETX); //Night audit end
            init.AppendLine(STX + "LA|DA" + dt.ToString("MMddyyyy") + "|TI" + dt.ToString("HHmmss") + "|" + ETX);
            return init;
        }
    }
}
