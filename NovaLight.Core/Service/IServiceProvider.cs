using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaLight.Core.Service
{
    public interface IServiceProvider
    {
        public void RegisterService<TService>(TService service) where TService : class;
        public void RegisterService(Type serviceType, object service);
        public void UnregisterService<TService>(TService? service = null) where TService : class;
        public void UnregisterService(Type serviceType);

        public TService? GetService<TService>() where TService : class;
        public object? GetService(Type serviceType);
    }
}