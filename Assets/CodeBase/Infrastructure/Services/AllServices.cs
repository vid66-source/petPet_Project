using System;
using System.Collections.Generic;
using UnityEngine;

namespace CodeBase.Infrastructure.Services
{
    public class AllServices
    {
        private readonly Dictionary<Type, IService> _services;

        public static AllServices Instance { get; } = new AllServices();

        private AllServices()
        {
            _services = new Dictionary<Type, IService>();
        }

        public void RegisterService<TService>(TService service) where TService : class, IService
        {
            _services.Add(typeof(TService), service);
            Debug.Log($"[Services] Registered service of type {typeof(TService).Name}");
        }

        public TService GetService<TService>() where TService : class, IService
        {
            var service = _services[typeof(TService)] as TService;
            return service;
        }
    }
}