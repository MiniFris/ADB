namespace ADB
{
    public class ADBPackage
    {
        public ADBDevice Device { get; private set; }
        public string Name { get; private set; }



        public ADBPackage(ADBDevice device, string name)
        {
            this.Device = device;
            this.Name = name;
        }


        public void Run()
        {
            Device.RunCommandAdbFromDevice($"shell monkey -p {Name} -c android.intent.category.LAUNCHER 1");
        }


        public void KillProcesses()
        {
            foreach (ADBProcess process in Device.GetProcess())
            {
                if(process.Name == Name)
                    process.Kill();
            }
        }


        public void StopService()
        {
            Device.RunCommandAdbFromDevice($"shell am stopservice {Name}");
        }


        public void Close()
        {
            Device.RunCommandAdbFromDevice($"shell am force-stop {Name}");
        }


        public void Uninstall()
        {
            Device.RunCommandAdbFromDevice($"uninstall {Name}");
        }
    }
}
