using System;

namespace hotel_mini_proxy.Tools
{
    public static class ChrOperation
    {
        private static readonly System.Text.Encoding Encd = System.Text.Encoding.GetEncoding(1252);
        public static char Chr(int code)
        {
            return Encd.GetChars(new byte[] { (byte)code })[0];
        }

        public static char Chr(short code)
        {
            return Encd.GetChars(new byte[] { (byte)code })[0];
        }
        public static char Chr(byte code)
        {
            return Encd.GetChars(new byte[] { (byte)code })[0];
        }
        public static char Chr(double code)
        {
            return Encd.GetChars(new byte[] { Convert.ToByte(code) })[0];
        }
        public static char Chr(string code)
        {
            return Encd.GetChars(new byte[] { Convert.ToByte(code) })[0];
        }

        public static byte Asc(char chr)
        {
            return Encd.GetBytes(new char[] { chr })[0];
        }


    }
}
