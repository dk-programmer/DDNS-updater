using StratoDomainDDNSChanger.Core;
using StratoDomainDDNSChanger.MVVM.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace StratoDomainDDNSChanger.MVVM.ViewModel
{
    class HomeViewModel : ObservableObject
    {
        public static HomeViewModel Instance { get; set; }
        public ConfigData ConfigData { get { return ConfigHandler.Instance.ConfigData; }}
        public RelayCommand GetIpButtonCommand { get; set; }
        public RelayCommand StartDDnsUpdateCommand { get; set; }

        public HomeViewModel (){
            Instance = this;
            GetIpButtonCommand = new RelayCommand(x =>
            {
                HomeHandler.Instance.StartFetchIpAddresses();
            });

            StartDDnsUpdateCommand = new RelayCommand(x =>
            {
                HomeHandler.Instance.StartUpdateDDnsWithConfig();
                StartDDnsRunningImage(this,null);
            });

            if (HomeHandler.Instance.IsGetIpRunning) StartGetIPRunningImage(this,null);
            if (HomeHandler.Instance.IsDDnsUpdateRunning) StartDDnsRunningImage(this, null);
        }

        //Animations
        public void StartDDnsRunningImage(object sender, RoutedEventArgs e)
        {
            // Start the animation
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (HomeView.Instance == null) return;
                DoubleAnimation rotateAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = new Duration(TimeSpan.FromSeconds(2)),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                RotateTransform rotateTransform = (RotateTransform)HomeView.Instance.DDnsRunningImage;
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
            });
        }
        public void StopDDnsRunningImage(object sender, RoutedEventArgs e)
        {
            // Stop the animation
            Application.Current.Dispatcher.Invoke(() =>
            {
                HomeView.Instance?.DDnsRunningImage?.BeginAnimation(RotateTransform.AngleProperty, null);
            });
        }

        public void StartGetIPRunningImage(object sender, RoutedEventArgs e)
        {
            // Start the animation
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (HomeView.Instance == null) return;
                DoubleAnimation rotateAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = new Duration(TimeSpan.FromSeconds(2)),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                RotateTransform rotateTransform = (RotateTransform)HomeView.Instance.GetIpImage;
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
            });
        }
        public void StopGetIPRunningImage(object sender, RoutedEventArgs e)
        {
            // Stop the animation
            Application.Current.Dispatcher.Invoke(() =>
            {
                HomeView.Instance?.GetIpImage?.BeginAnimation(RotateTransform.AngleProperty, null);
            });
        }

    }
}
