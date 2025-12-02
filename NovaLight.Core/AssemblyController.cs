namespace NovaLight.Core
{
    public abstract class AssemblyController
    {
        internal AssemblyController() { }

        public abstract void OnModuleRun();
        public abstract void OnModuleStop();
    }
}