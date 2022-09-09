/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * Date: 2022-5-30
 */
using SanteDB.BusinessRules.JavaScript.Util;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SanteDB.BusinessRules.JavaScript.JNI
{
    /// <summary>
    /// The Javascript Engine bridge
    /// </summary>
    public class JavascriptEngineBridge
    {
        // Adhoc cache service
        private IAdhocCacheService m_adhocCache;

        // Diagnostics tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(JavascriptEngineBridge));

        // The engine/executor that owns this
        private JavascriptExecutor m_owner;

        // Map of view model names to type names shared between instances
        private ModelSerializationBinder m_binder = new ModelSerializationBinder();

        /// <summary>
        /// Javascript business rules bridge ctor
        /// </summary>
        internal JavascriptEngineBridge(JavascriptExecutor owner)
        {
            this.m_owner = owner;
            this.m_adhocCache = ApplicationServiceContext.Current.GetService<IAdhocCacheService>();
        }

        /// <summary>
        /// Break current execution
        /// </summary>
        public void Break()
        {
            if (System.Diagnostics.Debugger.IsAttached)
                System.Diagnostics.Debugger.Break();
            else
                new JsConsoleProvider().warn("Break was requested however no debugger is attached.");
        }

        /// <summary>
        /// Add a business rule for the specified object
        /// </summary>
        public void AddBusinessRule(String id, String target, String trigger, ExpandoObject guard, Func<object, ExpandoObject> _delegate)
        {
            NameValueCollection guardExpr = null;
            if (guard != null)
            {
                guardExpr = new NameValueCollection();
                foreach (var kv in guard as IDictionary<String, Object>)
                {
                    List<String> vals = null;
                    if (kv.Value is object[])
                        vals = (kv.Value as object[]).Select(o => o.ToString()).ToList();
                    else if (kv.Value is string[])
                        vals = (kv.Value as string[]).ToList();
                    else
                        vals = new List<String>() { kv.Value.ToString() };
                    vals.ForEach(o=>guardExpr.Add(kv.Key, o));
                }
            }
            this.m_owner.RegisterCallback(id, target, trigger, guardExpr, _delegate);
        }

        /// <summary>
        /// Add a business rule for the specified object
        /// </summary>
        public void AddBusinessRule(String target, String trigger, ExpandoObject guard, Func<object, ExpandoObject> _delegate) => this.AddBusinessRule(Guid.NewGuid().ToString(), target, trigger, guard, _delegate);

        /// <summary>
        /// Adds validator
        /// </summary>
        public void AddValidator(String id, String target, Func<object, object[]> _delegate)
        {
            this.m_owner.RegisterCallback(id, target, "Validate", null, _delegate);
        }

        /// <summary>
        /// Adds validator
        /// </summary>
        public void AddValidator(String target, Func<Object, Object[]> _delegate) => this.AddValidator(Guid.NewGuid().ToString(), target, _delegate);

        /// <summary>
        /// Executes the business rule
        /// </summary>
        public object ExecuteRule(String action, Object data)
        {
            var sData = JavascriptUtils.ToModel(data);
            var retVal = this.m_owner.Invoke(action, sData);
            return JavascriptUtils.ToViewModel(retVal);
        }

        /// <summary>
        /// Get the host environment in which the environment is working
        /// </summary>
        public int Environment
        {
            get
            {
                return (int)ApplicationServiceContext.Current.HostType;
            }
        }

        /// <summary>
        /// True if the system is operating on the SanteDB Front end
        /// </summary>
        public bool IsInFrontEnd
        {
            get
            {
                return ApplicationServiceContext.Current.HostType != SanteDBHostType.Server;
            }
        }

        /// <summary>
        /// Saves tags associated with the specified object
        /// </summary>
        public object SaveTags(ExpandoObject obj)
        {
            try
            {
                var modelObj = JavascriptUtils.ToModel(obj) as ITaggable;
                if (modelObj != null)
                {
                    var tags = modelObj.Tags;
                    if (tags.Count() > 0)
                    {
                        var tpi = ApplicationServiceContext.Current.GetService(typeof(ITagPersistenceService)) as ITagPersistenceService;
                        if (tpi == null)
                            return obj;
                        foreach (var t in tags)
                        {
                            t.SourceEntityKey = (modelObj as IIdentifiedData).Key;
                            tpi.Save(t.SourceEntityKey.Value, t);
                        }
                    }
                }
                return obj;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error saving tags: {0}", e);
                throw;
            }
        }

        /// <summary>
        /// Delete cache item
        /// </summary>
        public void DeleteCache(String type, String key)
        {
            Type dataType = this.m_binder.BindToType(null, type);
            if (dataType == null)
                throw new InvalidOperationException($"Cannot find type information for {type}");

            var idpInstance = ApplicationServiceContext.Current.GetService(typeof(IDataCachingService)) as IDataCachingService;
            if (idpInstance == null)
                throw new KeyNotFoundException($"The data caching service for {type} was not found");
            idpInstance.Remove(Guid.Parse(key));
        }

        /// <summary>
        /// Execute bundle rules
        /// </summary>
        public Object ExecuteBundleRules(String trigger, Object bundle)
        {
            try
            {
                IDictionary<String, Object> bdl = bundle as IDictionary<String, Object>;

                object rawItems = null;
                if (!bdl.TryGetValue("resource", out rawItems))
                {
                    this.m_tracer.TraceVerbose("Bundle contains no items: {0}", JavascriptUtils.ProduceLiteral(bdl));
                    return bundle;
                }

                Object[] itms = rawItems as object[];

                for (int i = 0; i < itms.Length; i++)
                {
                    try
                    {
                        itms[i] = this.m_owner.InvokeRaw(trigger, itms[i]);
                    }
                    catch (Exception)
                    {
                        //if (System.Diagnostics.Debugger.IsAttached)
                        throw;
                        //else
                        //    Tracer.GetTracer(typeof(BusinessRulesBridge)).TraceError("Error applying rule for {0}: {1}", itms[i], e);
                    }
                }

                //bdl.Add("resource", itms);
                return bdl;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error executing bundle rules: {0}", e);
                throw;
            }
        }

        /// <summary>
        /// Get service by name
        /// </summary>
        public object GetService(String serviceName)
        {
            var serviceType = typeof(IRepositoryService<>).Assembly.ExportedTypes.FirstOrDefault(o => o.Name == serviceName && o.IsInterface);
            if (serviceType == null)
                return null;
            else
                return ApplicationServiceContext.Current.GetService(serviceType);
        }

        /// <summary>
        /// Generate new guid
        /// </summary>
        public String NewGuid()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Generate new guid
        /// </summary>
        public Guid ParseGuid(String guid)
        {
            return Guid.Parse(guid);
        }

        /// <summary>
        /// Get data asset
        /// </summary>
        public String GetDataAsset(String dataId)
        {
            using (Stream ins = (ApplicationServiceContext.Current.GetService(typeof(IDataReferenceResolver)) as IDataReferenceResolver).Resolve(dataId))
            using (MemoryStream ms = new MemoryStream())
            {
                ins.CopyTo(ms);
                return Encoding.UTF8.GetString(ms.ToArray(), 0, ms.ToArray().Length);
            }
        }

        /// <summary>
        /// Gets the specified data from the underlying data-store
        /// </summary>
        public object Delete(String type, Guid id)
        {
            try
            {
                Type dataType = this.m_binder.BindToType(null, type);

                var idp = typeof(IRepositoryService<>).MakeGenericType(dataType);
                var idpInstance = ApplicationServiceContext.Current.GetService(idp) as IRepositoryService;
                if (idpInstance == null)
                    throw new KeyNotFoundException($"The repository service for {type} was not found. Ensure an IRepositoryService<{type}> is registered");

                return JavascriptUtils.ToViewModel(idpInstance.Delete(id));
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error obsoleting BRE object: {0}/{1} - {2}", type, id, e);
                throw new Exception($"Error obsoleting BRE object: {type}/{id}", e);
            }
        }

        /// <summary>
        /// Gets the specified data from the underlying data-store
        /// </summary>
        public object Get(String type, String id)
        {
            try
            {
                var guidId = Guid.Parse(id);
                var cacheKey = $"{type}.{id}";
                // First, does the object existing the cache
                ExpandoObject retVal = this.m_adhocCache?.Get<ExpandoObject>(cacheKey);
                if (retVal == null)
                {
                    Type dataType = this.m_binder.BindToType(null, type);
                    if (dataType == null)
                        throw new InvalidOperationException($"Cannot find type information for {type}");

                    var idp = typeof(IRepositoryService<>).MakeGenericType(dataType);
                    var idpInstance = ApplicationServiceContext.Current.GetService(idp) as IRepositoryService;
                    if (idpInstance == null)
                        throw new KeyNotFoundException($"The repository service for {type} was not found. Ensure an IRepositoryService<{type}> is registered");

                    retVal = JavascriptUtils.ToViewModel(idpInstance.Get(guidId));
                    this.m_adhocCache?.Add(cacheKey, retVal, new TimeSpan(0, 0, 30));
                }
                return retVal;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error getting BRE object: {0}/{1} - {2}", type, id, e);
                throw new Exception($"Error getting BRE object: {type}/{id}", e);
            }
        }

        /// <summary>
        /// Find object
        /// </summary>
        public object Find(String type, ExpandoObject query)
        {
            var queryStr = query.ToNameValueCollection().ToHttpString();
            return Find(type, queryStr);
        }

        /// <summary>
        /// Add an object to the cache
        /// </summary>
        public void AddCache(ExpandoObject key, ExpandoObject data)
        {
            var queryStr = data.ToNameValueCollection().ToHttpString();
            AddCache(queryStr, data);
        }

        /// <summary>
        /// Add an object to the cache
        /// </summary>
        public void AddCache(String key, ExpandoObject data)
        {
            this.m_adhocCache.Add(key, data);
        }

        /// <summary>
        /// Get the object from cache
        /// </summary>
        public object GetCache(ExpandoObject key)
        {
            var queryStr = key.ToNameValueCollection().ToHttpString();
            return GetCache(queryStr);
        }

        /// <summary>
        /// Add an object to the cache
        /// </summary>
        public object GetCache(String key)
        {
            return this.m_adhocCache?.Get<ExpandoObject>(key);
        }

        /// <summary>
        /// Finds the specified data
        /// </summary>
        public object Find(String type, String query)
        {
            try
            {
                Type dataType = this.m_binder.BindToType(null, type);
                if (dataType == null)
                    throw new InvalidOperationException($"Cannot find type information for {type}");

                var idp = typeof(IRepositoryService<>).MakeGenericType(dataType);
                var idpInstance = ApplicationServiceContext.Current.GetService(idp) as IRepositoryService;
                if (idpInstance == null)
                    throw new KeyNotFoundException($"The repository service for {type} was not found. Ensure an IRepositoryService<{type}> is registered");

                var expr = QueryExpressionParser.BuildLinqExpression(dataType, query.ParseQueryString());
                var results = idpInstance.Find(expr).OfType<IdentifiedData>();
                return JavascriptUtils.ToViewModel(new Bundle()
                {
                    Item = results.ToList(),
                    TotalResults = results.Count()
                });
            }
            catch (TargetInvocationException e)
            {
                this.m_tracer.TraceError("Persistence searching from BRE: {0}?{1} - {2}", type, query, e.InnerException);
                throw new Exception($"Persistence searching from BRE : {type}?{query}", e.InnerException);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error searching from BRE: {0}?{1} - {2}", type, query, e);
                throw new Exception($"Error searching from BRE: {type}?{query}", e);
            }
        }

        /// <summary>
        /// Saves the specified object
        /// </summary>
        public object Save(object value)
        {
            try
            {
                var data = JavascriptUtils.ToModel(value);
                var cacheKey = $"{data.Type}.{data.Key}";
                this.m_adhocCache?.Remove(cacheKey);

                if (data == null) throw new ArgumentException("Could not parse value for save");

                var idp = typeof(IRepositoryService<>).MakeGenericType(data.GetType());
                var idpInstance = ApplicationServiceContext.Current.GetService(idp) as IRepositoryService;
                if (idpInstance == null)
                    throw new KeyNotFoundException($"The repository service for {data.GetType()} was not found. Ensure an IRepositoryService<{data.GetType()}> is registered");

                return JavascriptUtils.ToViewModel(idpInstance.Save(data));
            }
            catch (TargetInvocationException e)
            {
                this.m_tracer.TraceError("Persistence saving in BRE: {0} - {1}", value, e.InnerException);
                throw new Exception($"Persistence saving in  BRE : {value}", e.InnerException);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error saving in  BRE: {0} - {1}", value, e);
                throw new Exception($"Error saving in  BRE: {value}", e);
            }
        }

        /// <summary>
        /// Inserts the specified object
        /// </summary>
        public object Insert(object value)
        {
            try
            {
                var data = JavascriptUtils.ToModel(value);
                if (data == null) throw new ArgumentException("Could not parse value for insert");

                var cacheKey = $"{data.Type}.{data.Key}";
                this.m_adhocCache?.Remove(cacheKey);

                var idp = typeof(IRepositoryService<>).MakeGenericType(data.GetType());
                var idpInstance = ApplicationServiceContext.Current.GetService(idp) as IRepositoryService;
                if (idpInstance == null)
                    throw new KeyNotFoundException($"The repository service for {data.GetType()} was not found. Ensure an IRepositoryService<{data.GetType()}> is registered");

                return JavascriptUtils.ToViewModel(idpInstance.Insert(data));
            }
            catch (TargetInvocationException e)
            {
                this.m_tracer.TraceError("Persistence inserting in BRE: {0} - {1}", value, e.InnerException);
                throw new Exception($"Persistence inserting in  BRE : {value}", e.InnerException);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error inserting in  BRE: {0} - {1}", value, e);
                throw new Exception($"Error inserting in  BRE: {value}", e);
            }
        }
    }
}