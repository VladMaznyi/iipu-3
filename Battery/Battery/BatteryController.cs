using System.Diagnostics;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace Battery
{
    class BatteryController
    {
        string videoIdle = ("asdzxcqweas 00000000005");
        const string Powercfg = "powercfg.exe";
        const int GuidPosition = 3;
        const int HexSize = 8;
        const int SecInMin = 60;
        const int ThreadDelay = 1000;
        const int batteryStatez = 5;
        public Battery _Battery;
        public delegate void UpdateBatteryControllerDelegate(Battery battery);
        public delegate void SetActiveButton();
        public delegate void SetDeActiveButton();
        public event UpdateBatteryControllerDelegate UpdateBatteryEvent;
        public event SetActiveButton SetEnableButtonEvent;
        public event SetDeActiveButton SetDisableButtonEvent;

        public BatteryController()
        {
            _Battery = new Battery();
            _Battery.OldScreenTimeToShutdown = GetScreenTimeToShutdown();
        }

        public void UpdateBatteryController()
        {
            while (true)
            { 
                _Battery.Type = SystemInformation.PowerStatus.PowerLineStatus.ToString().Equals("Online");
                _Battery.ChargeLevel = (int)(SystemInformation.PowerStatus.BatteryLifePercent * 100);
                SetBatteryStatus();      
                UpdateBatteryEvent?.Invoke(_Battery);
                Thread.Sleep(ThreadDelay);         
            }
        }

        private void SetBatteryStatus()
        {
            if (SystemInformation.PowerStatus.PowerLineStatus.ToString().Equals("Online"))
            {
                _Battery.Status = "Charging...";
                SetScreenTimeToShutdown(_Battery.OldScreenTimeToShutdown);
                SetDisableButtonEvent?.Invoke();
            }
            else if (SystemInformation.PowerStatus.BatteryLifeRemaining == -1)
            {
                SetEnableButtonEvent?.Invoke();
                _Battery.Status = "Loading result...";
            }
            else
            {
                _Battery.Status = "Discharging";
                _Battery.TimeToDischarge = SystemInformation.PowerStatus.BatteryLifeRemaining / SecInMin;
            }
        }

        public void SetScreenTimeToShutdown(int minutes)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Powercfg,
                Arguments = "/Change Monitor-Timeout-Dc " + minutes,
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }

        private int GetScreenTimeToShutdown()
        {
            var procCmd = new Process();
            procCmd.StartInfo.UseShellExecute = false;
            procCmd.StartInfo.RedirectStandardOutput = true;
            procCmd.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            procCmd.StartInfo.FileName = "cmd.exe";
            procCmd.StartInfo.Arguments = "powercfg /q";
            procCmd.Start();
            var powerConfig = procCmd.StandardOutput.ReadToEnd();
            var lastString = new Regex("VIDEOIDLE.*(\\n.*){6}");
            var videoidle = lastString.Match(powerConfig).Value;
            var batteryState = videoIdle.Substring(videoIdle.Length - 11).TrimEnd();
            return Convert.ToInt32(batteryState, 16) / SecInMin;
        }
    }
}
