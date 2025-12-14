using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaLight.Core.Service
{
    public class BaseServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _registrations = [];

        public void RegisterService<TService>(TService service) where TService : class
        {
            Type serviceType = typeof(TService);
            RegisterService(serviceType, service);
        }
        public void RegisterService(Type serviceType, object service)
        {
            _registrations[serviceType] = service;
        }
        public void UnregisterService<TService>(TService? service = null) where TService : class
        {
            Type serviceType = typeof(TService);
            UnregisterService(serviceType);
        }
        public void UnregisterService(Type serviceType)
        {
            _registrations.Remove(serviceType);
        }

        public TService? GetService<TService>() where TService : class
        {
            Type serviceType = typeof(TService);
            TService? service = GetService(serviceType) as TService;
            return service;
        }
        public object? GetService(Type serviceType)
        {
            if (_registrations.TryGetValue(serviceType, out object? service))
                return service;
            if (serviceType.IsAbstract || serviceType.IsInterface)
                foreach (object value in _registrations.Values)
                {
                    Type currentType = value.GetType();
                    if (serviceType.IsAssignableFrom(currentType))
                        return value;
                }
            return null;
        }
    }
}