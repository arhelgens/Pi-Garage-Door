using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Threading;

namespace DroidPiInterface
{
    [Activity(Label = "DroidPiInterface", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {

        Timer timer;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            timer = new Timer(x => UpdateStatus(), null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1));
            Button btn_SendSignal = FindViewById<Button>(Resource.Id.btn_SendSignal);
            btn_SendSignal.Click += Btn_SendSignal_Click;
        }

        private void Btn_SendSignal_Click(object sender, EventArgs e)
        {
            var client = new PiServer.Service();
            client.UpdateStatusAsync("SignalGarageDoor", "Sent");
        }

        private void UpdateStatus()
        {
            CheckBox cb_Signal = FindViewById<CheckBox>(Resource.Id.cb_Signal);
            CheckBox cb_DoorClosed = FindViewById<CheckBox>(Resource.Id.cb_Door);
            CheckBox cb_CarHere = FindViewById<CheckBox>(Resource.Id.cb_Car);
            CheckBox cb_PhoneHome = FindViewById<CheckBox>(Resource.Id.cb_PhoneHome);
            Button btn_SendSignal = FindViewById<Button>(Resource.Id.btn_SendSignal);
            var client = new PiServer.Service();

            List<Sensors> sensorList = JsonConvert.DeserializeObject<List<Sensors>>(client.GetSensorStatus());
            RunOnUiThread(() =>
            {
                foreach (var sensor in sensorList)
                {
                    switch (sensor.Sensor)
                    {
                        case "SignalGarageDoor":
                            if (sensor.Status == "Idle")
                            {
                                cb_Signal.Checked = false;
                                btn_SendSignal.Enabled = true;
                            }
                            else
                            {
                                cb_Signal.Checked = true;
                                btn_SendSignal.Enabled = false;
                            }
                                
                            break;
                        case "GarageDoor":
                            if (sensor.Status == "Open")
                                cb_DoorClosed.Checked = false;
                            else
                                cb_DoorClosed.Checked = true;
                            break;
                        case "CarPresent":
                            if (sensor.Status == "Yes")
                                cb_CarHere.Checked = true;
                            else
                                cb_CarHere.Checked = false;
                            break;
                        default:
                            break;
                    }
                }
                if (client.CheckForPhone())
                    cb_PhoneHome.Checked = true;
                else
                    cb_PhoneHome.Checked = false;
            }); }
        private class Sensors
        {
            public string Sensor;
            public string Status;
        }
    }
}

