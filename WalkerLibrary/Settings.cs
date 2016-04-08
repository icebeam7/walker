using System;

namespace WalkerLibrary
{
    public class Settings
    {
        public string PhoneNumber { get; set; }
        public bool Verified { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }       //
        public int Interval { get; set; }
        public int Steps { get; set; }
        public int ReminderType { get; set; }
        public bool BandRegistered { get; set; }    //
        public bool ServiceEnabled { get; set; }    //

        public static Settings GetDefaultSetting()
        {
            DateTime currentTime = DateTime.Now;

            Settings setting = new Settings()
            {
                BandRegistered = false,
                StartTime = new TimeSpan(currentTime.Hour, currentTime.Minute, 0),
                Interval = 1,
                PhoneNumber = "",
                ReminderType = 0,
                ServiceEnabled = false,
                Steps = 100,
                Verified = false
            };

            setting.EndTime = setting.StartTime.Add(new TimeSpan(1, 0, 0));

            return setting;
        }

        public static Settings SetSetting(bool isBandRegistered, TimeSpan endTime, TimeSpan startTime, int interval,
    string phoneNumber, int remdinderType, bool isServiceEnabled, string steps, bool isVerified)
        {
            return new Settings()
            {
                BandRegistered = isBandRegistered,
                EndTime = endTime,
                StartTime = startTime,
                Interval = interval,
                PhoneNumber = phoneNumber,
                ReminderType = remdinderType,
                ServiceEnabled = isServiceEnabled,
                Steps = int.Parse(steps),
                Verified = isVerified
            };
        }


    }
}
