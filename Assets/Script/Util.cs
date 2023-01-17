
namespace MyUtil
{
    public static class Util
    {
        public static bool TrueFalseTranslater(bool boolean)
        {
            if (boolean)
                boolean = false;
            else if (!boolean)
                boolean = true;
            return boolean;
        }
    }
}