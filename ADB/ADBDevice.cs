using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

namespace ADB
{
    public class ADBDevice
    {
        public string Model { get; private set; }
        public int TransportId { get; private set; }
        public string DeviceUId { get; private set; }
        public Size Screen { get; private set; }
        public bool ADBKeyboard { get; set; } = true;

        private ADBXmlDocumentObj TempXmlDocumentObj { get; set; }
        private ADB ADBDriver {get; set;}




        public ADBDevice(ADB adb, string model, int transportId, string deviceUId, Size screen)
        {
            this.Model          = model;
            this.TransportId    = transportId;
            this.DeviceUId      = deviceUId;
            this.Screen         = screen;
            this.ADBDriver      = adb;
        }



        public void Click(Point point, uint milliseconds = 0)
        {
            if (milliseconds == 0)
                RunCommandAdbFromDevice($"shell input tap {point.X} {point.Y}");
            else
                Swipe(point, point, milliseconds);
        }


        public void SendText(string text)
        {
            if (new Regex(@"[^\dA-Za-z\s]").IsMatch(text))
                throw new ArgumentException("Invalid value");
            RunCommandAdbFromDevice($"shell input text '{text}'");
        }


        public void SendTextADBKeyboard(string text)
        {
            if (text.IndexOf("'") > 0)
                throw new ArgumentException("Invalid value");

            if (!ADBKeyboard)
            {
                ADBKeyboard = GetPackages().Find(obj => obj.Name == "com.android.adbkeyboard") != null;

                if (!ADBKeyboard)
                {
                    ApkInstall(@"ADBKeyboard.apk");
                    ADBKeyboard = true;
                }

                Thread.Sleep(3000);
            }

            string oldKeyboard = RunCommandAdbFromDevice($"shell settings get secure default_input_method").Trim();
            RunCommandAdbFromDevice($"shell ime set com.android.adbkeyboard/.AdbIME");
            Thread.Sleep(3000);
            RunCommandAdbFromDevice($"shell am broadcast -a ADB_INPUT_TEXT --es msg '{text}'");
            Thread.Sleep(1000);
            RunCommandAdbFromDevice($"shell ime set {oldKeyboard}");
            Thread.Sleep(3000);
        }


        public void Swipe(Point fromPoint, Point toPoint, uint milliseconds = 1000, uint count = 1)
        {
            if (milliseconds < 200)
                throw new ArgumentException("Incorrect milliseconds value");
            for (int i = 0; i < count; i++)
                RunCommandAdbFromDevice($"-s {DeviceUId} shell input swipe {(fromPoint.X > 0 ? --fromPoint.X : 0)} {(fromPoint.Y > 0 ? --fromPoint.Y : 0)} {(toPoint.X > 0 ? --toPoint.X : 0)} {(toPoint.Y > 0 ? --toPoint.Y : 0)} {milliseconds}");
        }


        public void Swipe(string toSwipe, uint milliseconds = 1000, uint count = 1)
        {
            switch (toSwipe)
            {
                case "Left":
                    Swipe(GetPointSwipe("Right"), GetPointSwipe(toSwipe), milliseconds, count);
                    break;

                case "Top":
                    Swipe(GetPointSwipe("Bottom"), GetPointSwipe(toSwipe), milliseconds, count);
                    break;

                case "Right":
                    Swipe(GetPointSwipe("Left"), GetPointSwipe(toSwipe), milliseconds, count);
                    break;

                case "Bottom":
                    Swipe(GetPointSwipe("Top"), GetPointSwipe(toSwipe), milliseconds, count);
                    break;

                default:
                    throw new ArgumentException("Invalid value for ToSwipe");
            }
        }


        public void Swipe(string fromSwipe, string toSwipe, uint milliseconds = 1000, uint count = 1)
        {
            Swipe(GetPointSwipe(fromSwipe), GetPointSwipe(toSwipe), milliseconds, count);
        }


        private Point GetPointSwipe(string toSwipe)
        {
            switch (toSwipe)
            {
                case "Left":
                    return new Point() { Y = Screen.Height / 2, X = Screen.Width / 4 };
                    break;

                case "Right":
                    return new Point() { X = Screen.Width - (Screen.Width / 4), Y = Screen.Height / 2 };
                    break;

                case "Bottom":
                    return new Point() { Y = Screen.Height - (Screen.Height / 4), X = Screen.Width / 2 };
                    break;

                case "Top":
                    return new Point() { X = Screen.Width / 2, Y = Screen.Height / 4 };
                    break;

                case "Center":
                    return new Point() { X = Screen.Width / 2, Y = Screen.Height / 2 };
                    break;

                default:
                    throw new ArgumentException("Invalid value for ToSwipe");
                    break;
            }
        }


        public List<ADBElement> Find(string xpath, ADBElement element = null)
        {
            try
            {
                if (element == null)
                {
                    if (ADBDriver.WaitElement)
                        Wait(xpath);

                    return CreateADBElements(GetDeviceXml().Document.SelectNodes(xpath));
                }
                else
                    return CreateADBElements(element.Xml.SelectNodes(xpath));
            }
            catch { }
            return null;
        }


        public List<ADBElement> GetAllElements(ADBElement element = null)
        {
            try
            {
                string xpath = "/*";
                if (element == null)
                {
                    return CreateADBElements(GetDeviceXml().Document.SelectNodes(xpath));
                }
                else
                    return CreateADBElements(element.Xml.SelectNodes(xpath));
            }
            catch { }
            return null;
        }


        public List<ADBElement> FindByText(string text, ADBElement element = null, bool register = true)
        {
            if (text.IndexOf("'") > -1)
                throw new ArgumentException("Invalid character \"'\"");

            string xpath = "";
            if (register)
                xpath = $"//*[contains(@text, '{text}')]";
            else
                xpath = $"//*[contains(translate(@text, 'ABCDEFGHIJKLMNOPQRSTUVWXYZЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮ', 'abcdefghijklmnopqrstuvwxyzйцукенгшщзхъфывапролджэячсмитьбю'), '{text.ToLower()}')]";

            return Find(xpath, element);
        }


        public ADBElement FindOne(string xpath, ADBElement element = null)
        {
            return Find(xpath, element)[0];
        }


        public bool Check(string xpath, ADBElement element = null)
        {
            if (element == null)
                return GetDeviceXml().Document.SelectNodes(xpath).Count > 0;
            else
                return element.Xml.SelectNodes(xpath).Count > 0;
        }


        public bool CheckByText(string text, ADBElement element = null, bool register = true)
        {
            if (text.IndexOf("'") > -1)
                throw new ArgumentException("Invalid character \"'\"");

            string xpath = "";
            if (register)
                xpath = $"//*[contains(@text, '{text}')]";
            else
                xpath = $"//*[contains(translate(@text, 'ABCDEFGHIJKLMNOPQRSTUVWXYZЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮ', 'abcdefghijklmnopqrstuvwxyzйцукенгшщзхъфывапролджэячсмитьбю'), '{text.ToLower()}')]";

            if (element == null)
                return GetDeviceXml().Document.SelectNodes(xpath).Count > 0;
            else
                return element.Xml.SelectNodes(xpath).Count > 0;
        }


        public void Wait(string xpath)
        {
            int waitTimeOut = 0;
            while (true)
            {
                try
                {
                    if (GetDeviceXml().Document.SelectNodes(xpath).Count > 0)
                        break;
                }
                catch { }
                Thread.Sleep(1000);

                if (waitTimeOut++ >= ADBDriver.WaitTimeout / 1000)
                    throw new TimeoutException("Time out");
            }
        }


        private ADBXmlDocumentObj GetDeviceXml()
        {
            if (TempXmlDocumentObj == null)
                TempXmlDocumentObj = new ADBXmlDocumentObj();

            if (TempXmlDocumentObj.UnixTimestamp + 100 > DateTime.Now.Second)
            {
                XmlDocument doc = new XmlDocument();
                string xml = "";
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        xml = new Regex(@"<\?xml.+>").Match(RunCommandAdbFromDevice($"exec-out uiautomator dump /dev/tty")).Value;
                        doc.LoadXml(xml);

                        if (!String.IsNullOrEmpty(xml))
                            break;
                    }
                    catch
                    {
                        try
                        {
                            xml = new Regex(@"<\?xml.+>").Match(RunCommandAdbFromDevice($"shell uiautomator dump /dev/tty")).Value;
                            doc.LoadXml(xml);

                            if (!String.IsNullOrEmpty(xml))
                                break;
                        }
                        catch { }
                    }
                    Thread.Sleep(1000);
                }
                if (String.IsNullOrEmpty(xml))
                    throw new ArgumentException("Xml parse error");

                TempXmlDocumentObj = new ADBXmlDocumentObj() { Document = doc, UnixTimestamp = DateTime.Now.Second };
            }
            return TempXmlDocumentObj;
        }


        private List<ADBElement> CreateADBElements(XmlNodeList xmlNodes)
        {
            List<ADBElement> adbElements = new List<ADBElement>();

            foreach (XmlNode xml in xmlNodes)
            {
                Point pos = new Point()
                {
                    X = Int32.Parse(new Regex("[^0-9]+").Replace(xml.Attributes["bounds"].Value.Split(']')[0].Split(',')[0], "")),
                    Y = Int32.Parse(new Regex("[^0-9]+").Replace(xml.Attributes["bounds"].Value.Split(']')[0].Split(',')[1], ""))
                };
                Size size = new Size()
                {
                    Width = Int32.Parse(new Regex("[^0-9]+").Replace(xml.Attributes["bounds"].Value.Split(']')[1].Split(',')[0], "")) - pos.X,
                    Height = Int32.Parse(new Regex("[^0-9]+").Replace(xml.Attributes["bounds"].Value.Split(']')[1].Split(',')[1], "")) - pos.Y
                };
                adbElements.Add(new ADBElement(this, xml, pos, size));
            }

            return adbElements;
        }


        public List<ADBPackage> GetPackages()
        {
            List<ADBPackage> packages = new List<ADBPackage>();
            foreach (Match matchTemp in new Regex(@"package:.+\b").Matches(RunCommandAdbFromDevice($"shell pm list packages")))
                packages.Add(new ADBPackage(this, matchTemp.Value.Split(':')[1]));

            return packages;
        }


        public List<ADBProcess> GetProcess()
        {
            List<ADBProcess> process = new List<ADBProcess>();

            List<string> response = RunCommandAdbFromDevice($"shell ps").Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            response.RemoveAt(0);
            foreach (string ps in response)
            {
                MatchCollection match = new Regex(@"\S+").Matches(ps);
                process.Add(new ADBProcess(this, match[0].Value, Int32.Parse(match[1].Value), Int32.Parse(match[2].Value), Int32.Parse(match[3].Value), Int32.Parse(match[4].Value), match[5].Value, match[6].Value, match[8].Value));
            }

            return process;
        }


        public string GetStatus()
        {
            return RunCommandAdbFromDevice($"get-state");
        }


        public void FilePush(string filePath, string toFilePath)
        {
            string response = RunCommandAdbFromDevice($"push {filePath} {toFilePath}");
            if (response.IndexOf("error") > -1)
                throw new ArgumentException(response);
        }


        public void FilePull(string filePath, string toFilePath)
        {
            string response = RunCommandAdbFromDevice($"pull {filePath} {toFilePath}");
            if (response.IndexOf("error") > -1)
                throw new ArgumentException(response);
        }


        public void ApkInstall(string filePath)
        {
            RunCommandAdbFromDevice($"install {filePath}");
        }


        public void Reboot()
        {
            RunCommandAdbFromDevice($"reboot");
        }


        public void RebootToRecovery()
        {
            RunCommandAdbFromDevice($"reboot recovery");
        }


        public void RebootToBootloader()
        {
            RunCommandAdbFromDevice($"reboot bootloader");
        }


        public void ShutdownNow()
        {
            RunCommandAdbFromDevice($"shell reboot -p");
        }


        public void SendKey(int key, int count = 1)
        {
            for (int i = 0; i < count; i++)
                RunCommandAdbFromDevice($"shell input keyevent {key}");
        }


        public string RunCommandAdbFromDevice(string command)
        {
            return ADBDriver.RunCommandADB($"-s {DeviceUId} {command}");
        }
    }
}
