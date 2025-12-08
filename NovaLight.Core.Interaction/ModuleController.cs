using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaLight.Core.Interaction
{
    public abstract class ModuleController
    {
        internal ModuleController() { }

        public abstract void OnModuleRun();
        public abstract void OnModuleStop();
    }
}