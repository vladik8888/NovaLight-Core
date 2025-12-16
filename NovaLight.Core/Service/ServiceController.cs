namespace NovaLight.Core.Service
{
    public abstract class ServiceController
    {
        public ServiceController() { }

        public abstract void OnServiceRun();
        public abstract void OnServiceStop();
        public abstract void OnServiceUnload();
    }
}