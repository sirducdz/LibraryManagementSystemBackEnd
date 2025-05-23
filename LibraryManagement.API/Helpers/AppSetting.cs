﻿namespace LibraryManagement.API.Helpers
{
    public class AppSetting
    {
        public string SecretKey { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int ExpiryInHours { get; set; }
    }
}
