namespace hotel_mini_proxy.Tools
{
    public static class TypeParser
    {

        public static decimal DecTryParse(string str, decimal defValue = (decimal)0)
        {
            decimal result;

            if (!decimal.TryParse(str, out result))
            {
                result = defValue;
            }

            return result;
        }

        public static int Int32TryParse(string str, int defValue = 0)
        {
            int result;
            if (!int.TryParse(str, out result))
            {
                result = defValue;
            }
            return result;
        }

        public static int Int32TryParse(object obj, int defValue = 0)
        {
            var str = obj as string;
            return str != null ? Int32TryParse(str, defValue) : defValue;

        }


        public static double DblTryParse(string str, double defValue = (double)0)
        {
            double result;
            if (!double.TryParse(str, out result))
            {
                result = defValue;
            }
            return result;
        }

    }
}
