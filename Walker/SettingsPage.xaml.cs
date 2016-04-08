using System;
using System.Threading.Tasks;
using Walker.Office365;
using WalkerLibrary;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Walker
{
    public sealed partial class SettingsPage : Page
    {
        StorageService storage;
        NumberVerify nexmo;
        VerifyResponse verifyResponse;

        bool originalIsVerified;
        int interval;
        string phoneNumber;
        bool firstTime = true;

        private string _mailAddress = "tony_articuno@hotmail.com";
        private string _displayName = null;
        private MailHelper _mailHelper = new MailHelper();
        public static ApplicationDataContainer _settings = ApplicationData.Current.RoamingSettings;

        public async Task<bool> SignInCurrentUserAsync()
        {
            var token = await AuthenticationHelper.GetTokenHelperAsync();

            if (token != null)
            {
                string userId = (string)_settings.Values["userID"];
                _mailAddress = (string)_settings.Values["userEmail"];
                _displayName = (string)_settings.Values["userName"];
                return true;
            }
            else
                return false;
        }

        public SettingsPage()
        {
            this.InitializeComponent();

            Loaded += SettingsPage_Loaded;

            Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
            
        }

        private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            firstTime = true;
            storage = new StorageService();
            nexmo = new NumberVerify();
            Settings setting = await storage.RetrieveObjectAsync<Settings>(App.settingKey);

            if (setting == null)
                setting = Settings.GetDefaultSetting();

            btnRegisterMSBand.IsChecked = setting.BandRegistered;
            firstTime = true;
            tmpEnd.Time = setting.EndTime;
            tmpStart.Time = setting.StartTime;
            txtPhoneNumber.Text = setting.PhoneNumber;
            cbxReminderType.SelectedIndex = setting.ReminderType;
            btnEnableBackgroundTask.IsChecked = setting.ServiceEnabled;
            txtSteps.Text = setting.Steps.ToString();
            imgVerify.Visibility = !setting.Verified ? Visibility.Visible : Visibility.Collapsed;
            imgVerified.Visibility = setting.Verified ? Visibility.Visible : Visibility.Collapsed;

            interval = setting.Interval;
            phoneNumber = setting.PhoneNumber;
            originalIsVerified = setting.Verified;

            switch (interval)
            {
                case 1: cbxInterval.SelectedIndex = 0; break;
                case 5: cbxInterval.SelectedIndex = 1; break;
                case 15: cbxInterval.SelectedIndex = 2; break;
                case 60: cbxInterval.SelectedIndex = 3; break;
                case 120: cbxInterval.SelectedIndex = 4; break;
                default: cbxInterval.SelectedIndex = 0; break;
            }
        }

        private async void btnRegisterMSBand_Checked(object sender, RoutedEventArgs e)
        {
            if (firstTime)
            {
                firstTime = false;
                return;
            }

            await MicrosoftBandService.RegisterBand();

            if (MicrosoftBandService.IsBandRegistered)
            {
                btnRegisterMSBand.Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/msbandx.png") };
                btnRegisterMSBand.Label = "Unregister Microsoft Band";
            }
            else
            {
                await Utils.ShowDialog("A Microsoft Band was not found or you decided not to register it with the app. Go to Settings → Privacy → Other Devices → Band → Let these apps use my Band → Turn Walker On to register it.");
                btnRegisterMSBand.IsChecked = false;
            }
        }

        private async void btnRegisterMSBand_Unchecked(object sender, RoutedEventArgs e)
        {
            if (firstTime)
            {
                firstTime = false;
                return;
            }

            await Utils.ShowDialog("Walker will not use your Microsoft Band.");
            MicrosoftBandService.IsBandRegistered = false;
            MicrosoftBandService.IsBackgroundTaskRegistered = false;

            btnRegisterMSBand.Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/msband.png") };
            btnRegisterMSBand.Label = "Register Microsoft Band";
        }

        private async void btnEnableBackgroundTask_Checked(object sender, RoutedEventArgs e)
        {
            await MicrosoftBandService.RegisterBackgroundTask();

            if (MicrosoftBandService.IsBackgroundTaskRegistered)
            {
                btnEnableBackgroundTask.Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/backgroundx.png") };
                btnEnableBackgroundTask.Label = "Enable Background Task";
            }
            else
            {
                await Utils.ShowDialog("A Microsoft Band was not registered first or you decided not to register the background task. Try again.");
                btnEnableBackgroundTask.IsChecked = false;
            }
        }

        private async void btnEnableBackgroundTask_Unchecked(object sender, RoutedEventArgs e)
        {
            await Utils.ShowDialog("Walker will not run in background.");
            MicrosoftBandService.IsBackgroundTaskRegistered = false;

            btnEnableBackgroundTask.Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/background.png") };
            btnEnableBackgroundTask.Label = "Disable Background Task";
        }

        private void tmpStart_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            var diferencia = tmpEnd.Time.Subtract(tmpStart.Time).TotalMinutes;

            if (diferencia >= 0 && diferencia < interval)
                tmpEnd.Time = tmpStart.Time.Add(new TimeSpan(0, interval, 0));
        }

        private void tmpEnd_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            var diferencia = tmpEnd.Time.Subtract(tmpStart.Time).TotalMinutes;

            if (diferencia >= 0 && diferencia < interval)
                tmpStart.Time = tmpEnd.Time.Add(new TimeSpan(0, -interval, 0));
        }

        private void cbxInterval_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cbxInterval.SelectedIndex)
            {
                case 0: interval = 1; break;
                case 1: interval = 5; break;
                case 2: interval = 15; break;
                case 3: interval = 60; break;
                case 4: interval = 120; break;
                default: interval = 1; break;
            }

            tmpStart_TimeChanged(null, null);
        }

        private async void btnSave_Tapped(object sender, TappedRoutedEventArgs e)
        {
            bool isVerified = (imgVerified.Visibility == Visibility.Visible);

            if (isVerified)
                Save();
            else
            {
                Action actionYes = new Action(Save);
                Action actionNo = new Action(ActionNo);

                await Utils.ShowDialog("The app will not work unless you register your phone number in order to receive Walker notifications. Do you want to save these settings?", Save, ActionNo);
            }
        }

        private void txtPhoneNumber_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txtPhoneNumber.Text != phoneNumber || txtPhoneNumber.Text == "")
            {
                imgVerify.Visibility = Visibility.Visible;
                imgVerified.Visibility = Visibility.Collapsed;
            }
            else if (txtPhoneNumber.Text == phoneNumber && txtPhoneNumber.Text != "")
            {
                imgVerify.Visibility = originalIsVerified ? Visibility.Collapsed : Visibility.Visible;
                imgVerified.Visibility = originalIsVerified ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        async void Save()
        {
            bool isVerified = (imgVerified.Visibility == Visibility.Visible);

            Settings setting = Settings.SetSetting(btnRegisterMSBand.IsChecked.Value, tmpEnd.Time, tmpStart.Time,
                interval, txtPhoneNumber.Text, cbxReminderType.SelectedIndex,
                btnEnableBackgroundTask.IsChecked.Value, txtSteps.Text, isVerified);

            await storage.PersistObjectAsync<Settings>(App.settingKey, setting);

            await Utils.ShowDialog("The settings have been saved successfully");
        }

        void ActionNo()
        {

        }

        private async void imgVerify_Tapped(object sender, TappedRoutedEventArgs e)
        {
            verifyResponse = await nexmo.Verify(txtPhoneNumber.Text);

            if (verifyResponse.status != "0")
                await Utils.ShowDialog("The validation code was not sent. Please try again.");

            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private async void btnValidateCode_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var checkResponse = await nexmo.Check(verifyResponse.request_id, txtCode.Text);

            if (checkResponse.status == "0")
            {
                flyVerify.Hide();
                phoneNumber = txtPhoneNumber.Text;
                originalIsVerified = true;
                imgVerify.Visibility = Visibility.Collapsed;
                imgVerified.Visibility = Visibility.Visible;
            }
            else
                txbMessage.Visibility = Visibility.Visible;
        }

        private async void btnResendCode_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var controlResponse = await nexmo.Control(verifyResponse.request_id);

            if (controlResponse.status == "0")
            {
                txbDirections.Text = "A validation code was sent again to your phone number. Please write it down.";
                txbMessage.Visibility = Visibility.Collapsed;
            }
            else
                await Utils.ShowDialog("The validation code was not sent. Please try again.");
        }

        private void btnCancel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            flyVerify.Hide();
        }

        
        

    private void App_BackRequested(object sender,
Windows.UI.Core.BackRequestedEventArgs e)
    {
        Frame rootFrame = Window.Current.Content as Frame;
        if (rootFrame == null)
            return;

        // Navigate back if possible, and if the event has not 
        // already been handled .
        if (rootFrame.CanGoBack && e.Handled == false)
        {
            e.Handled = true;
            rootFrame.GoBack();
        }
    }

        private async void cbxReminderType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbxReminderType.SelectedIndex == 2)
            {
                if (await SignInCurrentUserAsync())
                {
                    _mailAddress = "icebeam@icebeam.onmicrosoft.com";
                    string asunto = "Walker reminder";

                    try
                    {
                        string mensaje = "Hello, this is Walker. Remember to reach your steps goal!";
                        await _mailHelper.ComposeAndSendMailAsync(asunto, mensaje, _mailAddress);
                        await Utils.ShowDialog("Email was correctly sent");
                    }
                    catch (Exception ex)
                    {
                        await Utils.ShowDialog("Error: " + ex.ToString());
                    }
                }
            }
        }
    }
}
