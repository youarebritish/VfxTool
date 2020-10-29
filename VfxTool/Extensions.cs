using System.Globalization;

namespace VfxTool
{
    public static class Extensions
    {
        public static float ParseFloatRoundtrip(string text)
        {
            if (text == "-0")
            {
                return -0f;
            }

            return float.Parse(text, CultureInfo.InvariantCulture);
        }

        public static double ParseDoubleRoundtrip(string text)
        {
            if (text == "-0")
            {
                return -0f;
            }

            return double.Parse(text, CultureInfo.InvariantCulture);
        }
    }
}
