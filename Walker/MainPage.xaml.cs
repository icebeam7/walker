using Syncfusion.UI.Xaml.Charts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using WalkerLibrary;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Walker
{
    public sealed partial class MainPage : Page
    {
        public List<Walk> WalkResults { get; set; }
        public List<BandData> BandData { get; set; }

        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
        }


        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await Utils.SaveRecordedHealth();

            BandData = (await Utils.GetBandData()).Where(x => x.Period > 0).ToList();
            
            var txb = FindChildControl<TextBlock>(hubToday, "txbTodaySteps") as TextBlock;
            txb.Text = String.Format("You have walked {0} steps today. Keep up the good work!", Utils.GetTodaySteps(BandData));
            
            var seriesToday = FindChildControl<DoughnutSeries3D>(hubToday, "pieSeries") as DoughnutSeries3D;
            seriesToday.ItemsSource = Utils.GetTodayWalks(BandData);

            var seriesWeek = FindChildControl<BarSeries>(hubHistory, "weekSeries") as BarSeries;
            seriesWeek.ItemsSource = Utils.GetWeekWalks(BandData);

            var seriesArea = FindChildControl<ColumnSeries>(hubHistory, "lineSeries") as ColumnSeries;
            seriesArea.ItemsSource = Utils.GetMonthWalks(BandData);
        }

        private DependencyObject FindChildControl<T>(DependencyObject control, string ctrlName)
        {
            int childNumber = VisualTreeHelper.GetChildrenCount(control);
            for (int i = 0; i < childNumber; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(control, i);
                FrameworkElement fe = child as FrameworkElement;

                if (fe == null) return null;

                if (child is T && fe.Name == ctrlName)
                    return child;
                else
                {
                    DependencyObject nextLevel = FindChildControl<T>(child, ctrlName);
                    if (nextLevel != null)
                        return nextLevel;
                }
            }
            return null;
        }

        private void btnSettings_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
        }
    }
}
