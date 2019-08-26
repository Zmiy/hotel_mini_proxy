using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using hotel_mini_proxy.PmsInterface;
using NLog;
using TcpLibrary;

namespace hotel_mini_proxy.pmsRoutine
{
    static class pmsRoutine
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

        private static void Connect2Pms()
        {
            Task tsk = new Task(TryConnect2Pms);
            tsk.Start();
            //Task.WaitAll(tsk);
            //await Task.Run(() => TryConnect2Pms());
        }
    }
}
