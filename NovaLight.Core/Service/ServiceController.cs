using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IServiceProvider = NovaLight.Core.Service.IServiceProvider;

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