using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GarageDoorPi
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const int ECHO_PIN = 23;
        private const int TRIGGER_PIN = 18;
        private const int DOOR_PIN = 22;
        private const int RELAY_PIN = 21;
        private GpioPin doorSensor;
        private GpioPin pinEcho;
        private GpioPin pinTrigger;
        private GpioPin relayPin;
        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);
        private Stopwatch sw_Distance = new Stopwatch();
        DispatcherTimer distanceTimer = new Windows.UI.Xaml.DispatcherTimer();
        DispatcherTimer updateTimer = new DispatcherTimer();
        DispatcherTimer checkPhoneTimer = new DispatcherTimer();
        public MainPage()
        {
            this.InitializeComponent();
            InitGPIO();
            updateTimer.Interval = TimeSpan.FromMilliseconds(1000);
            updateTimer.Tick += updateTimer_Tick;
            updateTimer.Start();
            checkPhoneTimer.Interval = TimeSpan.FromMilliseconds(1000);
            checkPhoneTimer.Tick += CheckPhoneTimer_Tick;
            checkPhoneTimer.Start();
            distanceTimer.Interval = TimeSpan.FromMilliseconds(1000);
            distanceTimer.Tick += DistanceTimer_Tick;
            if (pinEcho != null && pinTrigger != null)
            {
                distanceTimer.Start();
            }
            DoorSensor_ValueChanged(doorSensor,null);
        }

        private async void DistanceTimer_Tick(object sender, object e)
        {
            pinTrigger.Write(GpioPinValue.High);
            await Task.Delay(10);
            pinTrigger.Write(GpioPinValue.Low);
            while (pinEcho.Read() == GpioPinValue.Low)
            {
                sw_Distance.Start();
            }

            while (pinEcho.Read() == GpioPinValue.High)
            {
            }
            sw_Distance.Stop();

            var elapsed = sw_Distance.Elapsed.TotalSeconds;
            var distance = elapsed * 34000;
            sw_Distance.Reset();
            distance /= 2;
            var client = new PiServer.ServiceSoapClient();
            if (distance > 20)
            {
                client.UpdateStatusAsync("CarPresent", "No").AsAsyncAction().AsTask().Wait();
                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    el_Car.Fill = grayBrush;
                });
            }
            else
            {
                client.UpdateStatusAsync("CarPresent", "Yes").AsAsyncAction().AsTask().Wait();
                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    el_Car.Fill = redBrush;
                });
            }
        }

        private void CheckPhoneTimer_Tick(object sender, object e)
        {
            var client = new PiServer.ServiceSoapClient();
            var result = client.CheckForPhoneAsync().Result;
            if (result)
                tblk_PhoneHome.Text = "Phone is at home";
            else
                tblk_PhoneHome.Text = "Phone is not at home";
        }

        private async void ProcessSignal()
        {
            var client = new PiServer.ServiceSoapClient();
            relayPin.Write(GpioPinValue.High);
            await Task.Delay(500);
            relayPin.Write(GpioPinValue.Low);
            
            client.UpdateStatusAsync("SignalGarageDoor", "Idle");
            
        }

        private void updateTimer_Tick(object sender, object e)
        {
            List<Sensors> sensorList = JsonConvert.DeserializeObject<List<Sensors>>(GetStatus());
            string signalStatus = sensorList.AsEnumerable().Where(a => a.Sensor == "SignalGarageDoor").Select(f => f.Status).First();
            if (signalStatus == "Idle")
                tblk_SignalReceived.Text = "No Signal";
            else
            {
                tblk_SignalReceived.Text = "Signal Received";
                ProcessSignal();
            }
        }

        private string GetStatus()
        {
            var client = new PiServer.ServiceSoapClient();
            var sensorStatus = client.GetSensorStatusAsync().Result;
            return sensorStatus.Body.GetSensorStatusResult;
        }

        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                //GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }

            doorSensor = gpio.OpenPin(DOOR_PIN);
            pinTrigger = gpio.OpenPin(TRIGGER_PIN);
            pinEcho = gpio.OpenPin(ECHO_PIN);
            relayPin = gpio.OpenPin(RELAY_PIN);
            relayPin.Write(GpioPinValue.Low);
            relayPin.SetDriveMode(GpioPinDriveMode.Output);
            doorSensor.SetDriveMode(GpioPinDriveMode.InputPullUp);
            pinTrigger.SetDriveMode(GpioPinDriveMode.Output);
            pinEcho.SetDriveMode(GpioPinDriveMode.Input);

            pinTrigger.Write(GpioPinValue.Low);
            doorSensor.ValueChanged += DoorSensor_ValueChanged;
        }

        private void DoorSensor_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            var client = new PiServer.ServiceSoapClient();
            if (sender.Read() == GpioPinValue.High)
            {
                client.UpdateStatusAsync("GarageDoor", "Open");
                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    el_GarageDoor.Fill = grayBrush;
                });
            }
            else
            {
                client.UpdateStatusAsync("GarageDoor", "Closed");
                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                  () =>
                  {
                      el_GarageDoor.Fill = redBrush;
                  });
            }
        }

        private class Sensors
        {
            public string Sensor;
            public string Status;
        }

    }
}
