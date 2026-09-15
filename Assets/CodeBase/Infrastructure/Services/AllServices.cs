using System;
using System.Collections.Generic;
using CodeBase.Experimental;
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

        public void RegisterService<TIService, TService>() where TIService : class, IService where TService : class,  TIService
        {
            TIService serviceInstance = ServicesResolver.ResolveServiceWithTypes<TIService, TService>();
            _services.Add(typeof(TIService), serviceInstance);
            Debug.Log($"[Services] Registered service of type {typeof(TIService).Name}");
        }

        public TService GetService<TService>() where TService : class, IService
        {
            TService service = _services[typeof(TService)] as TService;
            return service;
        }
    }
}