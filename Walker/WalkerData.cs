using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Walker
{
    public class WalkerData
    {
        public long TotalDistance { get; set; }
        public long TodaySteps { get; set; }
        public long TotalSteps { get; set; }
        public DateTime CapturedAt { get; set; }
        public bool Result { get; set; }
        public int Period { get; set; }
    }
}
