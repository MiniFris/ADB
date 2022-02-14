using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace ADB
{
    public class ADB
    {
        public string PathAdb = @"adb.exe";
        public bool WaitElement { get; set; }
        public uint WaitTimeout { get; set; }



        public ADB(string pathAdb = @"adb.exe", bool waitElement = true, uint waitTimeout = 20000)
        {
            this.PathAdb        = pathAdb;
            this.WaitElement    = waitElement;
            this.WaitTimeout    = waitTimeout;
        }



        public string RunCommandADB(string command = "")
        {
            Process process = new Process() { StartInfo = new ProcessStartInfo() { FileName = PathAdb, Arguments = command, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8 } };
            process.Start();

            string response = process.StandardOutput.ReadToEnd();
            if (response.IndexOf("device") > -1 && response.IndexOf("not found") > -1)
                throw new ArgumentException("Device not found");

            return response;
        }


        public List<ADBDevice> GetDevices()
        {
            List<ADBDevice> devices = new List<ADBDevice>();
            foreach (string dv in RunCommandADB("devices -l").Split('\n'))
            {
                if (dv.IndexOf("model:") > -1)
                {
                    string model    = new Regex(@"model:\S+\s").Matches(dv)[0].Value.Split(':')[1].Trim();
                    string deviceId = new Regex(@"^\S+\D").Matches(dv)[0].Value.Trim();
                    int transportId = Int32.Parse(new Regex(@"transport_id:\d+\D").Matches(dv)[0].Value.Split(':')[1]);
                    string strSize  = RunCommandADB($"-s {deviceId} shell wm size");
                    Size screen     = String.IsNullOrEmpty(strSize) ? default : new Size()
                    {
                        Width = Int32.Parse(new Regex(@"\d+").Matches(strSize)[0].Value),
                        Height = Int32.Parse(new Regex(@"\d+").Matches(strSize)[1].Value)
                    };

                    devices.Add(new ADBDevice(this, model, transportId, deviceId, screen));
                }
            }
            return devices;
        }
    }
}
