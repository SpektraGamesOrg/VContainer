using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace VContainer.Internal
{
    sealed class ReflectionInjector : IInjector
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReflectionInjector Build(Type type)
        {
            var injectTypeInfo = TypeAnalyzer.AnalyzeWithCache(type);
            return new ReflectionInjector(injectTypeInfo);
        }

        readonly InjectTypeInfo injectTypeInfo;

        ReflectionInjector(InjectTypeInfo injectTypeInfo)
        {
            this.injectTypeInfo = injectTypeInfo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Inject(object instance, IObjectResolver resolver, IReadOnlyList<IInjectParameter> parameters)
        {
            InjectFields(instance, resolver, parameters);
            InjectProperties(instance, resolver, parameters);
            InjectMethods(instance, resolver, parameters);
        }

        public object CreateInstance(IObjectResolver resolver, IReadOnlyList<IInjectParameter> parameters)
        {
            var parameterInfos = injectTypeInfo.InjectConstructor.ParameterInfos;
            var parameterValues = CappedArrayPool<object>.Shared8Limit.Rent(parameterInfos.Length);
            try
            {
                for (var i = 0; i < parameterInfos.Length; i++)
                {
                    var parameterInfo = parameterInfos[i];
                    parameterValues[i] = resolver.ResolveOrParameter(
                        null,
                        parameterInfo.ParameterType,
                        parameterInfo.Name,
                        parameters,
                        out bool success);
                }

                var instance = injectTypeInfo.InjectConstructor.ConstructorInfo.Invoke(parameterValues);
                Inject(instance, resolver, parameters);
                return instance;
            }
            catch (VContainerException ex)
            {
                throw new VContainerException(ex.InvalidType,
                    $"Failed to resolve {injectTypeInfo.Type} : {ex.Message}");
            }
            finally
            {
                CappedArrayPool<object>.Shared8Limit.Return(parameterValues);
            }
        }

        void InjectFields(object obj, IObjectResolver resolver, IReadOnlyList<IInjectParameter> parameters)
        {
            if (injectTypeInfo.InjectFields == null)
                return;

            foreach (var x in injectTypeInfo.InjectFields)
            {
                var fieldValue = resolver.ResolveOrParameter(x.Attribute, x.Data.FieldType, x.Data.Name, parameters,
                    out bool success);
                if (success)
                    x.Data.SetValue(obj, fieldValue);
            }
        }

        void InjectProperties(object obj, IObjectResolver resolver, IReadOnlyList<IInjectParameter> parameters)
        {
            if (injectTypeInfo.InjectProperties == null)
                return;

            foreach (var x in injectTypeInfo.InjectProperties)
            {
                var propValue = resolver.ResolveOrParameter(x.Attribute, x.Data.PropertyType, x.Data.Name, parameters,
                    out bool success);
                if (success)
                x.Data.SetValue(obj, propValue);
            }
        }

        void InjectMethods(object obj, IObjectResolver resolver, IReadOnlyList<IInjectParameter> parameters)
        {
            if (injectTypeInfo.InjectMethods == null)
                return;

            foreach (var method in injectTypeInfo.InjectMethods)
            {
                var parameterInfos = method.Data.ParameterInfos;
                var parameterValues = CappedArrayPool<object>.Shared8Limit.Rent(parameterInfos.Length);
                try
                {
                    for (var i = 0; i < parameterInfos.Length; i++)
                    {
                        var parameterInfo = parameterInfos[i];
                        parameterValues[i] = resolver.ResolveOrParameter(
                            method.Attribute,
                            parameterInfo.ParameterType,
                            parameterInfo.Name,
                            parameters,
                            out bool success);
                    }

                    method.Data.MethodInfo.Invoke(obj, parameterValues);
                }
                catch (VContainerException ex)
                {
                    throw new VContainerException(ex.InvalidType,
                        $"Failed to resolve {injectTypeInfo.Type} : {ex.Message}");
                }
                finally
                {
                    CappedArrayPool<object>.Shared8Limit.Return(parameterValues);
                }
            }
        }
    }
}