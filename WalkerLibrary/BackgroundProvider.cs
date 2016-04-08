using System;
using System.Linq;
using System.Threading.Tasks;

using Windows.ApplicationModel.Background;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Rfcomm;

namespace WalkerLibrary
{
    public static class BackgroundTaskProvider
    {
        private const string BandDataTaskId = "WalkerTask";

        public static DeviceUseTrigger DeviceUseTrigger { get; private set; }

        public static bool IsBandDataTaskRegistered { get { return BandDataTask != null; } }

        public static IBackgroundTaskRegistration BandDataTask 
        {
            get
            {
                return BackgroundTaskRegistration.AllTasks.FirstOrDefault(t => t.Value.Name == BandDataTaskId).Value;
            }
        }

        public static async Task<bool> RegisterBandDataTask(string taskName, string deviceId)
        {
            try
            {
                if (IsBandDataTaskRegistered)
                    UnregisterBandDataTask();

                var access = await BackgroundExecutionManager.RequestAccessAsync();

                if ((access == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity)
                    || (access == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity))
                {
                    await BuildBandDataTask(taskName, deviceId);
                    return true;
                }
            }
            catch (Exception)
            {
                UnregisterBandDataTask();
            }

            return false;
        }

        public static void UnregisterBandDataTask()
        {
            if (IsBandDataTaskRegistered)
                BandDataTask.Unregister(false);
        }

        private static async Task BuildBandDataTask(string taskName, string deviceId)
        {
            try
            {
                var access = await BackgroundExecutionManager.RequestAccessAsync();
                if ((access == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity) || (access == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity))
                {
                    var taskBuilder = new BackgroundTaskBuilder { Name = BandDataTaskId, TaskEntryPoint = taskName };
                    var deviceUseTrigger = new DeviceUseTrigger();
                    taskBuilder.SetTrigger(deviceUseTrigger);
                    taskBuilder.Register();

                    var device = (await DeviceInformation.FindAllAsync(RfcommDeviceService.GetDeviceSelector(RfcommServiceId.FromUuid(new Guid("A502CA9A-2BA5-413C-A4E0-13804E47B38F"))))).FirstOrDefault(/*x => x.Name == bandInfo.Name*/);

                    var triggerResult = await deviceUseTrigger.RequestAsync(device.Id);

                    switch (triggerResult)
                    {
                        case DeviceTriggerResult.DeniedByUser:
                            throw new InvalidOperationException("Cannot start the background task. Access denied by user.");
                        case DeviceTriggerResult.DeniedBySystem:
                            throw new InvalidOperationException("Cannot start the background task. Access denied by system.");
                        case DeviceTriggerResult.LowBattery:
                            throw new InvalidOperationException("Cannot start the background task. Low battery.");
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}