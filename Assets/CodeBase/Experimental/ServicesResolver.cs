using System;
using System.Reflection;
using CodeBase.Infrastructure.Services;

namespace CodeBase.Experimental
{
    public static class ServicesResolver
    {
        public static TIService ResolveServiceWithTypes<TIService, TService>() where TIService : class, IService where TService : class, TIService
        {
            Type type = typeof(TService);
            object serviceInstant = ResolveRecursively(type);
            return (TIService)serviceInstant;
            // Debug.Log($"{serviceInstant.GetType().Name} registration was resolved");
        }

        private static object ResolveRecursively(Type type)
        {
            ConstructorInfo constructor = type.GetConstructors()[0];
            ParameterInfo[] parameters = constructor.GetParameters();
            object[] arguments = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                arguments[i] = ResolveRecursively(parameters[i].ParameterType);
            return constructor.Invoke(arguments);
        }
    }
}