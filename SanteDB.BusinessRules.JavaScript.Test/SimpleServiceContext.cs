/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-5-19
 */
using SanteDB.Core;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace SanteDB.BusinessRules.JavaScript.Test
{
    /// <summary>
    /// Represents a simple service provider
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SimpleServiceContext : IServiceProvider, IServiceManager, IApplicationServiceContext
    {
        /// <summary>
        /// Represents a simple service provder
        /// </summary>
        private List<Object> m_services = new List<object>();

        public Guid ActivityUuid => Guid.NewGuid();

        /// <summary>
        /// CTOR adds itself to provider
        /// </summary>
        public SimpleServiceContext()
        {
            this.m_services.Add(this);
        }

        public bool IsRunning => true;

        public String ApplicationName => "Simple";

        public OperatingSystemID OperatingSystem => OperatingSystemID.Win32;

        public SanteDBHostType HostType => SanteDBHostType.Server;

        /// <summary>
        /// Gets the start time
        /// </summary>
        public DateTime StartTime { get; private set; }

#pragma warning disable CS0067
        /// <inheritdoc/>
        public event EventHandler Starting;

        /// <inheritdoc/>
        public event EventHandler Started;

        /// <inheritdoc/>
        public event EventHandler Stopping;

        /// <inheritdoc/>
        public event EventHandler Stopped;
#pragma warning restore 

        /// <summary>
        /// Add a service provider class
        /// </summary>
        public void AddServiceProvider(Type serviceType)
        {
            this.m_services.Add(Activator.CreateInstance(serviceType));
        }

        public void AddServiceProvider(object serviceInstance)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> CreateAll<T>(params object[] parms)
        {
            throw new NotImplementedException();
        }

        public object CreateInjected(Type type)
        {
            throw new NotImplementedException();
        }

        public TObject CreateInjected<TObject>()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TType> CreateInjectedOfAll<TType>(Assembly fromAssembly = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get all types
        /// </summary>
        public IEnumerable<Type> GetAllTypes()
        {
            return AppDomain.CurrentDomain.GetAllTypes();
        }

        /// <summary>
        /// Service type
        /// </summary>
        public object GetService(Type serviceType)
        {
            return this.m_services.FirstOrDefault(o => serviceType.IsAssignableFrom(o.GetType()));
        }

        public IEnumerable<object> GetServices()
        {
            return this.m_services;
        }

        /// <summary>
        /// Remove service provider
        /// </summary>
        public void RemoveServiceProvider(Type serviceType)
        {
            this.m_services.RemoveAll(o => o.GetType() == serviceType);
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void NotifyStartupProgress(string taskIdentifier, float startupProgress, string startupChangeText)
        {
            throw new NotImplementedException();
        }
    }
}