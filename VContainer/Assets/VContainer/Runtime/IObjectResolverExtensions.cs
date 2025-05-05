using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace VContainer
{
    public static class IObjectResolverExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Resolve<T>(this IObjectResolver resolver) => (T)resolver.Resolve(typeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryResolve<T>(this IObjectResolver resolver, out T resolved)
        {
            if (resolver.TryResolve(typeof(T), out var r))
            {
                resolved = (T)r;
                return true;
            }

            resolved = default;
            return false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ResolveOrDefault<T>(this IObjectResolver resolver, T defaultValue = default)
        {
            if (resolver.TryResolve(typeof(T), out var value))
            {
                return (T)value;
            }

            return defaultValue;
        }

        // Using from CodeGen
        [Preserve]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object ResolveNonGeneric(this IObjectResolver resolve, Type type) => resolve.Resolve(type);

        public static object ResolveOrParameter(
            this IObjectResolver resolver,
            InjectAttribute injectAttribute,
            Type parameterType,
            string parameterName,
            IReadOnlyList<IInjectParameter> parameters,
            out bool success)
        {
            if (parameters != null)
            {
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < parameters.Count; i++)
                {
                    var parameter = parameters[i];
                    if (parameter.Match(parameterType, parameterName))
                    {
                        success = true;
                        return parameter.GetValue(resolver);
                    }
                }
            }

            if (injectAttribute == null || injectAttribute.ForceRequire)
            {
                success = true;
                return resolver.Resolve(parameterType);
            }

            if (resolver.TryResolve(parameterType, out var result))
            {
                success = true;
                return result;
            }
            
            success = false;
            return null;
        }
    }
}