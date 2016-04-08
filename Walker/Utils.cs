using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using WalkerLibrary;
using Windows.Storage;
using Windows.UI.Popups;
using System.Collections;
using System.Text;
using System.Globalization;

namespace Walker
{
    public static class Utils
    {
        public static async Task ShowDialog(string message, Action methodYes, Action methodNo)
        {
            MessageDialog showDialog = new MessageDialog(message);
            showDialog.Commands.Add(new UICommand("Yes") { Id = 0 });
            showDialog.Commands.Add(new UICommand("No") { Id = 1 });

            showDialog.DefaultCommandIndex = 0;
            showDialog.CancelCommandIndex = 1;

            var result = await showDialog.ShowAsync();

            if ((int)result.Id == 0)
                methodYes();
            else
                methodNo();
        }

        public static async Task ShowDialog(string message)
        {
            MessageDialog showDialog = new MessageDialog(message);
            showDialog.Commands.Add(new UICommand("OK"));
            await showDialog.ShowAsync();
        }

        private static async Task<List<BandData>> LoadRecordedData()
        {
            var files = await ApplicationData.Current.LocalFolder.GetFilesAsync();
            var file = files.FirstOrDefault(f => f.Name == "WalkerData.json");

            if (file != null)
            {
                var buffer = await FileIO.ReadBufferAsync(file);
                if (buffer != null)
                {
                    using (var reader = new JsonTextReader(new StreamReader(buffer.AsStream())))
                    {
                        var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.All };
                        return serializer.Deserialize<List<BandData>>(reader);
                    }
                }
            }

            return new List<BandData>();
        }

        public static List<Walk> GetWeekWalks(List<BandData> bandData)
        {
            DateTime now = DateTime.Now;
            DateTime start = now.Date.AddDays(-(int)now.DayOfWeek); // prev sunday 00:00
            DateTime end = start.AddDays(7); // next sunday 00:00

            var weekData = bandData.Where(x => x.CapturedAt.Date >= start && x.CapturedAt.Date < end).OrderByDescending(x =>x.CapturedAt.Date).GroupBy(x => x.CapturedAt.Date).Select(xg => new Walk { Result = xg.Key.ToString("ddd dd"), Count = xg.Sum(s => s.TodaySteps) }).ToList();
            
            return weekData;
        }

        //
        static GregorianCalendar _gc = new GregorianCalendar();
        public static int GetWeekOfMonth(this DateTime time)
        {
            DateTime first = new DateTime(time.Year, time.Month, 1);
            return time.GetWeekOfYear() - first.GetWeekOfYear() + 1;
        }

        static int GetWeekOfYear(this DateTime time)
        {
            return _gc.GetWeekOfYear(time, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
        }
        //

        public static List<Walk> GetMonthWalks(List<BandData> bandData)
        {
            Func<DateTime, int> weekProjector =
                d => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                     d,
                     CalendarWeekRule.FirstFourDayWeek,
                     DayOfWeek.Sunday);

            Func<DateTime, int> monthProjector =
                d => d.GetWeekOfYear() - new GregorianCalendar().GetWeekOfYear(new DateTime(d.Year, d.Month, 1), CalendarWeekRule.FirstDay, DayOfWeek.Sunday);

            //DateTime now = DateTime.Now;
            DateTime now = new DateTime(2016, 2, 16);
            DateTime start = now.Date.AddDays(1 - now.Day);
            DateTime end = start.AddMonths(1);

            var monthData = from m in bandData
                            where m.CapturedAt.Date >= start && m.CapturedAt.Date < end
                            orderby m.CapturedAt.Date ascending
                            group m by weekProjector(m.CapturedAt);

            return monthData.Select(xg => new Walk { Result = "Week " + monthProjector(xg.First().CapturedAt), Count = xg.Sum(s => s.TodaySteps) }).ToList();
        }

        public async static Task SaveRecordedHealth()
        {
            DateTime now = DateTime.Now;

            try
            {
                var RecordedData = await LoadRecordedData();
                RecordedData.Add(new BandData() { CapturedAt = now.AddDays(-15), Period = 1, Result = true, TodaySteps = 100, TotalDistance = 0, TotalSteps = 0 });

                var formatting = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
                var json = JsonConvert.SerializeObject(RecordedData, formatting);
                var bytes = Encoding.UTF8.GetBytes(json);

                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                    "WalkerData.json", CreationCollisionOption.ReplaceExisting);

                await FileIO.WriteBytesAsync(file, bytes);
            }
            catch { }
        }

        public static async Task<List<BandData>> GetBandData()
        {
            return await LoadRecordedData();
        }

        private static List<BandData> GetTodayData(List<BandData> bandData)
        {
            return bandData.Where(x => x.CapturedAt.Date == DateTime.Now.Date).ToList();
        }

        public static long GetTodaySteps(List<BandData> bandData)
        {
            var todayData = GetTodayData(bandData);
            return todayData.Sum(x => x.TodaySteps);
        }

        public static List<Walk> GetTodayWalks(List<BandData> bandData)
        {
            var todayData = GetTodayData(bandData);
            var results = new List<Walk>();

            results.Add(new Walk() { Result = "Success", Count = todayData.Where(x => x.Result).Count() });
            results.Add(new Walk() { Result = "Failure", Count = todayData.Where(x => !x.Result).Count() });

            return results;
        }
    }
}
