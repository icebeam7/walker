using System;

namespace WalkerLibrary
{
    public class BandData
    {
        public long TotalDistance { get; set; }
        public long TodaySteps { get; set; }
        public long TotalSteps { get; set; }
        public DateTime CapturedAt { get; set; }
        public bool Result { get; set; }
        public int Period { get; set; }

        public BandData Clone()
        {
            var bandHealth = new BandData
            {
                CapturedAt = this.CapturedAt,
                TotalDistance = this.TotalDistance,
                TotalSteps = this.TotalSteps,
                TodaySteps = this.TodaySteps,
                Result = this.Result,
                Period = this.Period
            };

            return bandHealth;
        }
    }

}
