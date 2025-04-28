namespace LibraryManagement.API.Helpers
{
    public class DateTimeHelper
    {
        public static DateTimeOffset ConvertFromUnixSeconds(long unixTimeSeconds)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds).ToUniversalTime();
        }
    }
}
