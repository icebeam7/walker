using System;
using System.Linq;
using System.Threading.Tasks;

using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.UI.Popups;

using WalkerBackground;
using WalkerLibrary;

using Microsoft.Band;
using System.Collections.Generic;
using Windows.Storage;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Walker
{
    public static class MicrosoftBandService
    {
        public static bool IsBandRegistered;
        public static bool IsBackgroundTaskRegistered;

        public static async Task RegisterBand()
        {
            var bandInfo = (await BandClientManager.Instance.GetBandsAsync()).FirstOrDefault();
            await GetConsentForMSBand(bandInfo);
        }

        public static async Task RegisterBackgroundTask()
        {
            if (IsBandRegistered)
            {
                var bandInfo = (await BandClientManager.Instance.GetBandsAsync()).FirstOrDefault();

                var device = (await
                     DeviceInformation.FindAllAsync(
                         RfcommDeviceService.GetDeviceSelector(
                             RfcommServiceId.FromUuid(new Guid("A502CA9A-2BA5-413C-A4E0-13804E47B38F")))))
                        .FirstOrDefault(x => x.Name == bandInfo.Name);

                IsBackgroundTaskRegistered = device != null
                                       && await BackgroundTaskProvider.RegisterBandDataTask(
                                           typeof(MSBandTask).FullName, device.Id);

                if (IsBackgroundTaskRegistered)
                    await Utils.ShowDialog("Walker background task has been registered successfully.");
            }
            else IsBackgroundTaskRegistered = false;
        }

        private static async Task GetConsentForMSBand(IBandInfo bandInfo)
        {
            IBandClient bandClient = null;

            bool isRunning = false;

            if (bandInfo != null)
            {
                using (new DisposableAction(() => isRunning = true, () => isRunning = false))
                {
                    try { bandClient = await BandClientManager.Instance.ConnectAsync(bandInfo); }
                    catch (Exception ex)
                    {
                        IsBandRegistered = false;
                        return;
                    }

                    if (bandClient != null)
                    {
                        IsBandRegistered = true;
                        await Utils.ShowDialog("A Microsoft Band was registered successfully.");
                    }
                    else
                    {
                        IsBandRegistered = false;
                        return;
                    }
                }
            }
            else return;

            bandClient.Dispose();
        }

        public static async Task<List<BandData>> LoadRecordedData()
        {
            try
            {
                var files = await ApplicationData.Current.LocalFolder.GetFilesAsync();
                var file = files.FirstOrDefault(f => f.Name == "WalkerData.json");
                var x = files.Count();
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
            catch (Exception ex)
            {
                return new List<BandData>();
            }
        }

    }
}
