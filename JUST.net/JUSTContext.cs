﻿using JUST.net.Selectables;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace JUST
{
    public class CustomFunction
    {
        public string AssemblyName { get; set; }
        public string Namespace { get; set; }
        public string MethodName { get; set; }
        public string MethodAlias { get; set; }
        public bool VariableParameters { get; set; }

        public CustomFunction()
        {
        }

        public CustomFunction(string assemblyName, string namespc, string methodName, string methodAlias = null, bool variableParameters = false)
        {
            AssemblyName = assemblyName;
            Namespace = namespc;
            MethodName = methodName;
            MethodAlias = methodAlias;
            VariableParameters = variableParameters;
        }
    }

    [Flags]
    public enum EvaluationMode : short
    {
        FallbackToDefault = 1,
        AddOrReplaceProperties = 2,
        Strict = 4
    }

    public class JUSTContext
    {
        private Dictionary<string, MethodInfo> _customFunctions = new Dictionary<string, MethodInfo>();
        private List<string> _variableParametersCustomFunctions = new List<string>();
        private int _defaultDecimalPlaces = 28;

        internal JToken Input;

        public EvaluationMode EvaluationMode = EvaluationMode.FallbackToDefault;

        public int DefaultDecimalPlaces
        {
            get { return _defaultDecimalPlaces; }
            set
            {
                if (value < 0 || value > 28) { throw new ArgumentException("Value must be between 1 and 28"); }
                _defaultDecimalPlaces = value;
            }
        }

        public JUSTContext() { }

        public JUSTContext(IEnumerable<CustomFunction> customFunctions)
        {
            foreach (var function in customFunctions)
            {
                RegisterCustomFunction(function);
            }
        }

        internal JUSTContext(string inputJson)
        {
            Input = JToken.Parse(inputJson);
        }

        internal bool IsStrictMode()
        {
            return (EvaluationMode & EvaluationMode.Strict) == EvaluationMode.Strict;
        }

        internal bool IsAddOrReplacePropertiesMode()
        {
            return (EvaluationMode & EvaluationMode.AddOrReplaceProperties) == EvaluationMode.AddOrReplaceProperties;
        }

        internal bool IsFallbackToDefault()
        {
            return (EvaluationMode & EvaluationMode.FallbackToDefault) == EvaluationMode.FallbackToDefault;
        }

        public void RegisterCustomFunction(CustomFunction customFunction)
        {
            RegisterCustomFunction(customFunction.AssemblyName, customFunction.Namespace, customFunction.MethodName, customFunction.MethodAlias, customFunction.VariableParameters);
        }

        public void RegisterCustomFunction(string assemblyName, string namespc, string methodName, string methodAlias = null, bool variableParameters = false)
        {
            var methodInfo = ReflectionHelper.SearchCustomFunction(assemblyName, namespc, methodName);
            if (methodInfo == null)
            {
                throw new Exception("Unable to find specified method!");
            }

            _customFunctions.Add(methodAlias ?? methodName, methodInfo);
            if (variableParameters)
                _variableParametersCustomFunctions.Add(methodAlias ?? methodName);
        }

        public void UnregisterCustomFunction(string aliasOrName)
        {
            _customFunctions.Remove(aliasOrName);
        }

        public void ClearCustomFunctionRegistrations()
        {
            _customFunctions.Clear();
        }

        internal MethodInfo GetCustomMethod(string key)
        {
            if (!_customFunctions.TryGetValue(key, out var result))
            {
                throw new Exception($"Custom function {key} is not registered!");
            }
            return result;
        }

        internal bool IsRegisteredCustomFunction(string aliasOrName)
        {
            return _customFunctions.ContainsKey(aliasOrName);
        }

        internal T Resolve<T>(JToken token) where T: ISelectableToken
        {
            T instance = Activator.CreateInstance<T>();
            instance.Token = token;
            return instance;
        }

        public IEnumerable<string> GetVariableParameterCustomFunctions()
        {
            return _variableParametersCustomFunctions;
        }
    }
}
