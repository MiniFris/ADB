namespace ADB
{
    public class ADBProcess
    {
        public ADBDevice Device { get; private set; }
        public string User { get; private set; }
        public int PID { get; private set; }
        public int PPID { get; private set; }
        public int VSZ { get; private set; }
        public int RSS { get; private set; }
        public string WCHAN { get; private set; }
        public string ADDR { get; private set; }
        public string Name { get; private set; }



        public ADBProcess(ADBDevice device, string user, int pid, int ppid, int vsz, int rss, string wchan, string addr, string name)
        {
            this.Device     = device;
            this.User       = user;
            this.PID        = pid;
            this.PPID       = ppid;
            this.VSZ        = vsz;
            this.RSS        = rss;
            this.WCHAN      = wchan;
            this.ADDR       = addr;
            this.Name       = name;
        }



        public void Kill()
        {
            Device.RunCommandAdbFromDevice($"shell kill {PID}");
        }
    }
}
