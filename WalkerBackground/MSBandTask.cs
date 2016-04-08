using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Band;
using Microsoft.Band.Sensors;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Newtonsoft.Json;
using WalkerLibrary;

namespace WalkerBackground
{
    public sealed class MSBandTask : IBackgroundTask, IDisposable
    {
        private const string FileName = "WalkerData.json";

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        private IBandClient bandClient;
        private BandData bandData;
        private BackgroundTaskDeferral deferral;
        private IBandInfo bandInfo;
        private List<BandData> RecordedData { get; set; }

        private bool isDistanceReceived;
        private bool isPedometerReceived;
        private bool isDistanceOn;
        private bool isPedometerOn;

        private bool isDataSaving;
        private Timer recordingTimer;

        private Settings setting;
        private DateTime? startTime, endTime;
        private long startSteps, endSteps;
        private int interval, seconds;
        private bool changeDay = true;
        private bool firstTime = true;

        public bool IsDataReceived
        {
            get
            {
                if (!this.isDistanceOn)
                    this.isDistanceReceived = true;

                if (!this.isPedometerOn)
                    this.isPedometerReceived = true;

                return this.isDistanceReceived && this.isPedometerReceived;
            }
        }
        
        public bool AnySensorsOn { get { return this.isDistanceOn || this.isPedometerOn; } }
        
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            this.deferral = taskInstance.GetDeferral();

            taskInstance.Canceled += this.OnTaskCanceled;

            try { await this.SetupBand(); }
            catch { this.CompleteDeferral(); }
        }

        private async Task SetupBand()
        {
            try { this.RecordedData = await LoadRecordedData(); }
            catch { this.RecordedData = new List<BandData>(); }

            await GetSetting();

            this.bandInfo = (await BandClientManager.Instance.GetBandsAsync()).FirstOrDefault();

            if (this.bandInfo == null)
                throw new InvalidOperationException("No Microsoft Band available to connect to.");

            var isConnecting = false;

            using (new DisposableAction(() => isConnecting = true, () => isConnecting = false))
            {
                this.bandClient = await BandClientManager.Instance.ConnectAsync(this.bandInfo);

                if (this.bandClient == null)
                    throw new InvalidOperationException("Could not connect to the Microsoft Band available.");

                this.ResetReceivedFlags();
                this.bandData = new BandData();

                await this.SetupSensors();
            }
        }

        private async Task GetSetting()
        {
            var storage = new StorageService();
            setting = await storage.RetrieveObjectAsync<Settings>("setting");

            if (setting == null)
                setting = Settings.GetDefaultSetting();

            interval = setting.Interval;

            seconds = 0;
            firstTime = true;

            if (changeDay)
            {
                DateTime now = DateTime.Now;

                endTime = new DateTime(now.Year, now.Month, now.Day, setting.EndTime.Hours, setting.EndTime.Minutes, 0);
                startTime = new DateTime(now.Year, now.Month, now.Day, setting.StartTime.Hours, setting.StartTime.Minutes, 0);
                changeDay = false;

                if (setting.EndTime.Subtract(setting.StartTime).TotalMinutes < 0)
                    endTime = endTime.Value.AddDays(1);
            }
        }

        private async void SaveData(object state)
        {
            await this.SaveData();
        }

        private async Task SaveData()
        {
            if (this.isDataSaving)
                return;

            if (this.IsDataReceived)
            {
                this.isDataSaving = true;
                this.bandData.CapturedAt = DateTime.Now;

                var health = this.bandData.Clone();

                if (this.RecordedData != null)
                    if (!this.RecordedData.Contains(health))
                        this.RecordedData.Add(health);

                if (health.CapturedAt >= startTime && health.CapturedAt <= endTime)
                {
                    if (seconds == 0)
                    {
                        await CheckAndSave(health, false);
                        this.isDataSaving = false;
                        return;
                    }
                    else if (seconds >= interval * 60)
                    {
                        health.Period = interval;
                        await CheckAndSave(health, true);
                        this.isDataSaving = false;
                        return;
                    }
                }
                else
                    changeDay = true;

                await CheckAndSave(health, false);
            }

            this.isDataSaving = false;
        }

        async Task CheckAndSave(BandData health, bool save)
        {
            seconds += 30;

            if (save)
            {
                endSteps = health.TotalSteps;
                health.TodaySteps = endSteps - startSteps;
                health.Result = (health.TodaySteps >= setting.Steps);

                if (setting.Verified)
                    await new NexmoSMSVoice().Send(setting.PhoneNumber,
                        (health.Result) ? String.Format("Hello. This is Walker. Well done! You walked {0} steps in the last {1} minutes. Congratulations!", health.TodaySteps, interval)
                                        : String.Format("Hello. This is Walker. Remember to walk at least {0} steps in the next {1} minutes. You can do it!", setting.Steps, interval), 
                        setting.ReminderType);

                // enviar mensahe a la band
                await GetSetting();

                seconds = 0;
                startSteps = health.TotalSteps;

                await this.SaveRecordedHealth(save);
            }
        }

        private void OnDistanceChanged(object sender, BandSensorReadingEventArgs<IBandDistanceReading> e)
        {
            if (this.isDataSaving)
                return;

            this.bandData.TotalDistance = e.SensorReading.TotalDistance;
            this.isDistanceReceived = true;
        }

        private void onPedometerChanged(object sender, BandSensorReadingEventArgs<IBandPedometerReading> e)
        {
            if (this.isDataSaving)
                return;

            this.bandData.TotalSteps = e.SensorReading.TotalSteps;

            if (firstTime)
            {
                startSteps = this.bandData.TotalSteps;
                firstTime = false;
            }

            this.isPedometerReceived = true;
        }

        private async Task SetupSensors()
        {
            this.recordingTimer = new Timer(this.SaveData, null, Timeout.Infinite, Timeout.Infinite);

            try
            {
                if (this.AnySensorsOn)
                    await this.StopSensorsRunning();

                await this.StartSensorsRunning();

                this.recordingTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(30));
            }
            catch { this.CompleteDeferral(); }
        }

        private async Task StartSensorsRunning()
        {
            try
            {
                this.bandClient.SensorManager.Distance.ReadingChanged += this.OnDistanceChanged;
                await this.bandClient.SensorManager.Distance.StartReadingsAsync();

                this.isDistanceOn = true;
            }
            catch { this.isDistanceOn = false; }

            try
            {
                this.bandClient.SensorManager.Pedometer.ReadingChanged += onPedometerChanged;
                await this.bandClient.SensorManager.Pedometer.StartReadingsAsync();

                this.isPedometerOn = true;
            }
            catch { this.isPedometerOn = false; }
        }

        private void ResetReceivedFlags()
        {
            this.isDistanceReceived = false;
            this.isPedometerReceived = false;
        }

        private async void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            await this.CompleteDeferral();
        }

        private async Task CompleteDeferral()
        {
            try { await this.SaveRecordedHealth(false); }
            catch (Exception ex) { /* Handle saving failure */ }

            if (this.bandClient != null)
            {
                await this.StopSensorsRunning();

                this.bandClient.Dispose();
                this.bandClient = null;
            }

            BackgroundTaskProvider.UnregisterBandDataTask();

            this.deferral.Complete();
        }

        private static async Task<List<BandData>> LoadRecordedData()
        {
            var files = await ApplicationData.Current.LocalFolder.GetFilesAsync();
            var file = files.FirstOrDefault(f => f.Name == FileName);

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

        private async Task SaveRecordedHealth(bool save)
        {
            await this.semaphore.WaitAsync();

            try
            {
                var formatting = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
                var json = JsonConvert.SerializeObject(this.RecordedData, formatting);
                var bytes = Encoding.UTF8.GetBytes(json);

                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                    FileName, CreationCollisionOption.ReplaceExisting);

                await FileIO.WriteBytesAsync(file, bytes);
            }
            catch { }
            finally { this.semaphore.Release(); }
        }

        private async Task StopSensorsRunning()
        {
            try { await this.bandClient.SensorManager.Distance.StopReadingsAsync(); }
            catch { }
            finally
            {
                this.bandClient.SensorManager.Distance.ReadingChanged -= this.OnDistanceChanged;
                this.isDistanceOn = false;
            }

            try { await this.bandClient.SensorManager.Pedometer.StopReadingsAsync(); }
            catch { }
            finally
            {
                this.bandClient.SensorManager.Pedometer.ReadingChanged -= this.onPedometerChanged;
                this.isPedometerOn = false;
            }
        }

        public async void Dispose()
        {
            if (null != this.bandClient)
                await this.CompleteDeferral();
        }
    }
}