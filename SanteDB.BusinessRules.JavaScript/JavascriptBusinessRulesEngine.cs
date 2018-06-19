﻿/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * Date: 2017-9-1
 */
using Jint.Native;
using Jint.Runtime;
using Newtonsoft.Json;
using SanteDB.BusinessRules.JavaScript.JNI;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Jint;
using SanteDB.Core.Model;
using System.Text.RegularExpressions;
using SanteDB.Core.Interfaces;
using Jint.Parser.Ast;
using Jint.Parser;
using SanteDB.Core.Exceptions;
using System.Threading;

namespace SanteDB.BusinessRules.JavaScript
{

    /// <summary>
    /// Rules engine was created
    /// </summary>
    public class RulesEngineCreatedArgs : EventArgs
    {

        /// <summary>
        /// Creates new event 
        /// </summary>
        public RulesEngineCreatedArgs(JavascriptBusinessRulesEngine engine)
        {
            this.CreatedEngine = engine;
        }

        /// <summary>
        /// Gets the engine that was created
        /// </summary>
        public JavascriptBusinessRulesEngine CreatedEngine { get; private set; }
    }

    /// <summary>
    /// Represents the JavaScript business rules engine
    /// </summary>
    public class JavascriptBusinessRulesEngine : IDisposable
    {

        // Debug mode
        private static bool s_debugMode = false;

        /// <summary>
        /// Set the global debug mode
        /// </summary>
        /// <param name="debugMode">The debug mode to set</param>
        public static void SetDebugMode(bool debugMode)
        {
            s_debugMode = debugMode;
        }

        // UUID for logging
        private Guid m_engineId = Guid.NewGuid();

        // BRE pool
        private static Stack<JavascriptBusinessRulesEngine> s_brePool = new Stack<JavascriptBusinessRulesEngine>();

        // Instance count
        private int m_instanceCount = 0;

        // Tracer for JSBRE
        private Tracer m_tracer = Tracer.GetTracer(typeof(JavascriptBusinessRulesEngine));

        // Javascript BRE instance
        private static JavascriptBusinessRulesEngine s_instance;

        // Reset event for bre pool
        private static AutoResetEvent s_poolResetEvent = new AutoResetEvent(false);

        /// <summary>
        /// Thread static instance
        /// </summary>
        [ThreadStatic]
        private static JavascriptBusinessRulesEngine s_threadInstance;

        // Sync lock
        private static Object s_syncLock = new object();

        // Local lock
        private object m_localLock = new object();

        // Javascript engine
        private Jint.Engine m_engine = null;

        // Bridge
        private JNI.BusinessRulesBridge m_bridge = new JNI.BusinessRulesBridge();

        // Trigger definitions
        private Dictionary<String, Dictionary<String, List<Func<object, ExpandoObject>>>> m_triggerDefinitions = new Dictionary<string, Dictionary<string, List<Func<object, ExpandoObject>>>>();

        // Validators
        private Dictionary<String, List<Func<Object, Object[]>>> m_validatorDefinitions = new Dictionary<string, List<Func<object, Object[]>>>();

        /// <summary>
        /// Only one BRE can be created
        /// </summary>
        private JavascriptBusinessRulesEngine()
        {

        }

        /// <summary>
        /// Business rules bridge
        /// </summary>
        public BusinessRulesBridge Bridge { get { return this.m_bridge; } }

        /// <summary>
        /// Initialize the business rules engine
        /// </summary>
        public static void InitializeGlobal()
        {
            // Ensure the current exists
            JavascriptBusinessRulesEngine.Current.Initialize();

            
            // Host is server, then initialize a pool
            if (ApplicationServiceContext.HostType == SanteDBHostType.Server)
            {
                s_brePool = new Stack<JavascriptBusinessRulesEngine>(Environment.ProcessorCount * 2);
                for (int i = 0; i < Environment.ProcessorCount * 2; i++)
                {
                    var bre = new JavascriptBusinessRulesEngine();
                    bre.Initialize();
                    s_brePool.Push(bre);
                }
            }

        }

        /// <summary>
        /// Add rules to all global objects
        /// </summary>
        public static void AddRulesGlobal(String ruleId, StreamReader script)
        {
            s_threadInstance = JavascriptBusinessRulesEngine.Current;
            JavascriptBusinessRulesEngine.Current.AddRules(ruleId, script);
            foreach (var i in s_brePool)
            {
                script.BaseStream.Seek(0, SeekOrigin.Begin);
                s_threadInstance = i;
                i.AddRules(ruleId, script);
            }
            s_threadInstance = null;
        }

        /// <summary>
        /// Initialize javascript BRE
        /// </summary>
        private void Initialize()
        {
            if (this.m_engine != null) return; // already initialized

            // Set up javascript ening 
            this.m_tracer.TraceInfo("SanteDB Javascript Business Rules Host Initialize");

            this.m_engine = new Jint.Engine(cfg => cfg.AllowClr(
                    typeof(SanteDB.Core.Model.BaseEntityData).GetTypeInfo().Assembly,
                    typeof(IBusinessRulesService<>).GetTypeInfo().Assembly
                )
                .Strict(false)
#if DEBUG
                .DebugMode(true)
#else
                .DebugMode(s_debugMode)
#endif

                ).SetValue("SanteDBBre", this.m_bridge)
                .SetValue("console", new JsConsoleProvider())
                .SetValue("reflector", new JsObjectProvider());

            foreach (var itm in typeof(JavascriptBusinessRulesEngine).GetTypeInfo().Assembly.GetManifestResourceNames().Where(o => o.EndsWith(".js")))
                using (StreamReader sr = new StreamReader(typeof(JavascriptBusinessRulesEngine).GetTypeInfo().Assembly.GetManifestResourceStream(itm)))
                    this.AddRules(itm.Replace(typeof(JavascriptBusinessRulesEngine).GetTypeInfo().Assembly.FullName, ""), sr);

        }

        /// <summary>
        /// Gets an instance specifically for this executing thread 
        /// </summary>
        public static JavascriptBusinessRulesEngine GetThreadInstance()
        {
            if (ApplicationServiceContext.HostType == SanteDBHostType.Server)
            {
                if (s_threadInstance == null)
                {
                    // This block of code attempts to get a free business rule service from the available pool, if one is not available
                    // it will go into a wait state and will block, re-activating when another engine is disposed.
                    try
                    {
                        Monitor.Enter(s_syncLock);
                        if (s_brePool.Count > 0)
                        {
                            s_threadInstance = s_brePool.Pop();
                            s_threadInstance.m_instanceCount++;
                            Monitor.Exit(s_syncLock); // dispose of lock
                        }
                        else
                        {
                            while (s_brePool.Count == 0)
                            {
                                Monitor.Exit(s_syncLock);
                                s_instance.m_tracer.TraceVerbose("JSBRE Pool exhausted, awaiting free engine");
                                s_poolResetEvent.WaitOne();
                                Monitor.Enter(s_syncLock);
                            }
                            s_threadInstance = s_brePool.Pop();
                            s_threadInstance.m_instanceCount++;

                            Monitor.Exit(s_syncLock);
                        }

                        s_threadInstance.m_tracer.TraceVerbose("Allocated JSBRE Instance - ID # {0}, Pool = {1}", s_threadInstance.m_engineId, s_brePool.Count);

                    }
                    finally
                    {
                        // Release lock
                        if(Monitor.IsEntered(s_syncLock))
                            Monitor.Exit(s_syncLock);
                    }
                }
                else
                    s_threadInstance.m_instanceCount++;
                return s_threadInstance;
            }
            else
                return JavascriptBusinessRulesEngine.Current;
        }

        /// <summary>
        /// Gets the executing file key 
        /// </summary>
        public String ExecutingFile { get; set; }

        /// <summary>
        /// Current BRE
        /// </summary>
        public static JavascriptBusinessRulesEngine Current
        {
            get
            {
                if (s_instance == null)
                    lock (s_syncLock)
                    {
                        s_instance = new JavascriptBusinessRulesEngine();
                        s_instance.Initialize();
                    }
                return s_instance;
            }
        }

        /// <summary>
        /// Gets the engine
        /// </summary>
        public Engine Engine { get { return this.m_engine; } }

        /// <summary>
        /// Add the specified script
        /// </summary>
        public void AddRules(String ruleId, StreamReader script)
        {
            try
            {
                // Already ran
                this.m_tracer.TraceVerbose("Adding rules to BRE: {0}", ruleId);
                var rawScript = script.ReadToEnd();
                // Find all reference paths
                Regex includeReg = new Regex(@"\/\/\/\s*?\<reference\s*?path\=[""'](.*?)[""']\s?\/\>", RegexOptions.Multiline);
                var incMatches = includeReg.Matches(rawScript);

                foreach (Match match in incMatches)
                {
                    var include = match.Groups[1].Value;
                    var incStream = (ApplicationServiceContext.Current.GetService(typeof(IDataReferenceResolver)) as IDataReferenceResolver)?.Resolve(include);
                    if (incStream == null)
                        this.m_tracer.TraceWarning("Include {0} not found", include);
                    else
                        try
                        {
                            using (StreamReader sr = new StreamReader(incStream))
                                this.m_engine.Execute(sr.ReadToEnd());
                        }
                        catch (Exception e)
                        {
                            this.m_tracer.TraceWarning("Will skip {0} due to {1}", include, e.Message);
                        }

                }

                this.m_engine.Execute(rawScript);
            }
            catch (JavaScriptException ex)
            {
                this.m_tracer.TraceError("Error executing JavaScript {0}:{1} > {2}", ex.LineNumber, ex.Column, ex);
                throw ex;
            }
        }

        /// <summary>
        /// Register a validator which is responsible for validation
        /// </summary>
        public void RegisterValidator(string target, Func<object, Object[]> _delegate)
        {

            List<Func<object, Object[]>> validatorFunc = null;
            if (!this.m_validatorDefinitions.TryGetValue(target, out validatorFunc))
            {
                this.m_tracer.TraceVerbose("Will try to create BRE service for {0}", target);
                // We need to create a rule service base and register it!!! :)
                // Find the target type
                var targetType = typeof(Act).GetTypeInfo().Assembly.ExportedTypes.FirstOrDefault(o => o.GetTypeInfo().GetCustomAttribute<JsonObjectAttribute>()?.Id == target);
                if (targetType == null)
                    throw new KeyNotFoundException(target);
                var ruleService = typeof(RuleServiceBase<>).MakeGenericType(targetType);
                var serviceManager = ApplicationServiceContext.Current.GetService(typeof(IServiceManager)) as IServiceManager;

                lock (s_syncLock)
                    if (ApplicationServiceContext.Current.GetService(ruleService) == null)
                        serviceManager.AddServiceProvider(ruleService);

                // Now add
                lock (this.m_localLock)
                    this.m_validatorDefinitions.Add(target, new List<Func<object, Object[]>>() { _delegate });
            }
            else
                validatorFunc.Add(_delegate);

        }

        /// <summary>
        /// Register a rule
        /// </summary>
        public void RegisterRule(string target, string trigger, Func<object, ExpandoObject> _delegate)
        {
            Dictionary<String, List<Func<object, ExpandoObject>>> triggerHandler = null;
            if (!this.m_triggerDefinitions.TryGetValue(target, out triggerHandler))
            {
                this.m_tracer.TraceInfo("Will try to create BRE service for {0}", target);
                // We need to create a rule service base and register it!!! :)
                // Find the target type
                var targetType = typeof(Act).GetTypeInfo().Assembly.ExportedTypes.FirstOrDefault(o => o.GetTypeInfo().GetCustomAttribute<JsonObjectAttribute>()?.Id == target);
                if (targetType == null)
                    throw new KeyNotFoundException(target);
                var ruleService = typeof(RuleServiceBase<>).MakeGenericType(targetType);
                var serviceManager = ApplicationServiceContext.Current.GetService(typeof(IServiceManager)) as IServiceManager;

                lock (s_syncLock)
                    if (ApplicationServiceContext.Current.GetService(ruleService) == null)
                        serviceManager.AddServiceProvider(ruleService);

                // Now add
                lock (this.m_localLock)
                    this.m_triggerDefinitions.Add(target, new Dictionary<string, List<Func<object, ExpandoObject>>>()
                    {
                        { trigger, new List<Func<object, ExpandoObject>>() { _delegate } }
                    });
            }
            else
            {
                List<Func<object, ExpandoObject>> delegates = null;
                if (!triggerHandler.TryGetValue(trigger, out delegates))
                    lock (this.m_localLock)
                        triggerHandler.Add(trigger, new List<Func<object, ExpandoObject>>() { _delegate });
                else
                    delegates.Add(_delegate);
            }
        }

        /// <summary>
        /// Get call list of action
        /// </summary>
        /// <typeparam name="TBinding">The type of the t binding.</typeparam>
        /// <param name="action">The action.</param>
        /// <returns>List&lt;Func&lt;System.Object, ExpandoObject&gt;&gt;.</returns>
        public List<Func<object, ExpandoObject>> GetCallList<TBinding>(String action)
        {
            return this.GetCallList(typeof(TBinding), action);
        }

        /// <summary>
        /// Generic method for bindig call list
        /// </summary>
        /// <param name="tbinding">The tbinding.</param>
        /// <param name="action">The action.</param>
        /// <returns>List&lt;Func&lt;System.Object, ExpandoObject&gt;&gt;.</returns>
        public List<Func<object, ExpandoObject>> GetCallList(Type tbinding, String action)
        {
            var className = tbinding.GetTypeInfo().GetCustomAttribute<JsonObjectAttribute>()?.Id;

            // Try to get the binding
            Dictionary<String, List<Func<object, ExpandoObject>>> triggerHandler = null;
            if (this.m_triggerDefinitions.TryGetValue(className, out triggerHandler))
            {
                List<Func<object, ExpandoObject>> callList = null;
                if (triggerHandler.TryGetValue(action, out callList))
                    return callList;
            }
            return new List<Func<object, ExpandoObject>>();

        }

        /// <summary>
        /// Get all validator functions
        /// </summary>
        public List<Func<Object, Object[]>> GetValidators<TBinding>()
        {
            return this.GetValidators(typeof(TBinding));
        }

        /// <summary>
        /// Get validator functions for binding
        /// </summary>
        public List<Func<Object, Object[]>> GetValidators(Type tbinding)
        {
            var className = tbinding.GetTypeInfo().GetCustomAttribute<JsonObjectAttribute>()?.Id;

            // Try to get the binding
            List<Func<object, Object[]>> callList = null;
            if (this.m_validatorDefinitions.TryGetValue(className, out callList))
                return callList;
            return new List<Func<object, object[]>>();
        }

        /// <summary>
        /// Invoke raw
        /// </summary>
        public object InvokeRaw(String action, Object data)
        {
            lock (this.m_localLock)
                try
                {
                    var binder = new SanteDB.Core.Model.Serialization.ModelSerializationBinder();

                    var sdata = data as IDictionary<String, Object>;
                    if (sdata == null || !sdata.ContainsKey("$type")) return data;

                    var callList = this.GetCallList(binder.BindToType("SanteDB.Core.Model, Version=1.1.0.0", sdata["$type"].ToString()), action);
                    var retVal = data;

                    if (callList.Count > 0)
                    {
                        foreach (var c in callList)
                        {
                            data = c(data);
                        }
                    }

                    return data;
                }
                catch (JavaScriptException e)
                {
                    this.m_tracer.TraceError("JAVASCRIPT ERROR RUNNING {0} OBJECT :::::> {3}@{2}\r\n{1}", action, this.ProduceLiteral(data), e.LineNumber, e);
                    throw new DetectedIssueException(new List<DetectedIssue>()
                    {
                        new DetectedIssue()
                        {
                            Priority = DetectedIssuePriorityType.Error,
                            Text = $"{e.Error.ToString()} @ {e.Location.Start.Line} - {e.Location.End.Line}"
                        }
                    });

                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error running {0} for {1} : {2}", action, this.ProduceLiteral(data), e);
                    throw new BusinessRulesExecutionException($"Error running business rule {action} for {this.ProduceLiteral(data)} - {e.Message}", e);
                }
        }

        /// <summary>
        /// Produce a literal representation of the data
        /// </summary>
        private object ProduceLiteral(object data)
        {
            StringBuilder sb = new StringBuilder();
            var dict = data as IDictionary<String, Object>;
            if (dict != null)
                foreach (var kv in dict)
                    sb.AppendFormat("{0}={{{1}}}", kv.Key, this.ProduceLiteral(kv.Value));
            else
                sb.Append(data?.ToString() ?? "null");
            return sb.ToString();
        }

        /// <summary>
        /// Perform actual invokation on all objects
        /// </summary>
        public TBinding Invoke<TBinding>(string action, TBinding data) where TBinding : IdentifiedData
        {
            lock (this.m_localLock)
                try
                {
                    if (data == default(TBinding)) return data;

                    var callList = this.GetCallList(data.GetType(), action).Union(this.GetCallList<TBinding>(action)).Distinct();
                    var retVal = data;

                    if (callList.Count() > 0)
                    {
                        dynamic viewModel = this.m_bridge.ToViewModel(retVal);
                        foreach (var c in callList)
                        {
                            viewModel = c(viewModel);
                        }
                        retVal = (TBinding)this.m_bridge.ToModel(viewModel);
                    }

                    return retVal;
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error running {0} for {1} : {2}", action, data, e);
                    throw new BusinessRulesExecutionException($"Error running business rule {action} for {data}", e);
                }
        }

        /// <summary>
        /// Validate the data object if validation is available
        /// </summary>
        public List<DetectedIssue> Validate<TBinding>(TBinding data) where TBinding : IdentifiedData
        {
            lock (this.m_localLock)
                try
                {
                    var callList = this.GetValidators(data.GetType()).Union(this.GetValidators<TBinding>()).Distinct();
                    var retVal = new List<DetectedIssue>();
                    var vmData = this.m_bridge.ToViewModel(data);
                    foreach (var c in callList)
                    {
                        object[] issues = null;
                        issues = c(vmData);
                        retVal.AddRange(issues.Cast<IDictionary<String, Object>>().Select(o => new DetectedIssue()
                        {
                            Text = o.ContainsKey("text") ? o["text"]?.ToString() : null,
                            Priority = o.ContainsKey("priority") ? (DetectedIssuePriorityType)(int)(double)o["priority"] : DetectedIssuePriorityType.Informational
                        }));
                    }
                    return retVal;
                }
                catch (JavaScriptException e)
                {
                    this.m_tracer.TraceError("JAVASCRIPT ERROR VALIDATING OBJECT :::::> {1}@{0}", e.LineNumber, e);
                    return new List<DetectedIssue>()
                {
                    new DetectedIssue()
                    {
                        Priority = DetectedIssuePriorityType.Error,
                        Text = $"{e.Message} @ {e.LineNumber}"
                    }
                };

                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error validating {0} : {1}", data, e);
                    return new List<DetectedIssue>()
                    {
                        new DetectedIssue()
                        {
                            Priority = DetectedIssuePriorityType.Error,
                            Text = e.Message
                        }
                    };
                }
        }

        /// <summary>
        /// Dispose of the waiting thread
        /// </summary>
        public void Dispose()
        {
            if (this != JavascriptBusinessRulesEngine.Current) // push the thread instance back on the queue
            {
                if(m_instanceCount <= 1 && !s_brePool.ToArray().Any(o=>o.m_engineId == this.m_engineId))
                    lock (s_syncLock)
                    {
                        s_brePool.Push(this);
                        s_threadInstance = null;
                        this.m_tracer.TraceVerbose("Released JSBRE Instance - ID # {0}, Pool = {1}", this.m_engineId, s_brePool.Count);
                    }
                m_instanceCount--;
                s_poolResetEvent.Set();
            }
        }

        /// <summary>
        /// Indicates whether a rule has been registered for the specified action and type
        /// </summary>
        /// <param name="trigger">The trigger being checked</param>
        /// <param name="instanceType">The type of data being stored</param>
        /// <returns>True if there are any triggers registered for <paramref name="trigger"/></returns>
        public bool HasRule<TBinding>(string trigger, Type instanceType)
        {
            var callList = this.GetCallList<TBinding>(trigger);
            if (instanceType != null)
                callList = callList.Union(this.GetCallList(instanceType, trigger)).ToList();
            return callList.Any();
        }
    }
}
