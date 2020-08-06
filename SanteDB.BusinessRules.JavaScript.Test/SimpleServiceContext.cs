/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Copyright 2019-2019 SanteSuite Contributors (See NOTICE)
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
 * User: Justin Fyfe
 * Date: 2019-8-8
 */
using SanteDB.Core;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.BusinessRules.JavaScript.Test
{
    /// <summary>
    /// Represents a simple service provider
    /// </summary>
    public class SimpleServiceContext : IServiceProvider, IServiceManager, IApplicationServiceContext
    {

        /// <summary>
        /// Represents a simple service provder
        /// </summary>
        private List<Object> m_services = new List<object>();

        /// <summary>
        /// CTOR adds itself to provider
        /// </summary>
        public SimpleServiceContext()
        {
            this.m_services.Add(this);
        }

        public bool IsRunning => true;

        public OperatingSystemID OperatingSystem => OperatingSystemID.Win32;

        public SanteDBHostType HostType => SanteDBHostType.Server;

        /// <summary>
        /// Gets the start time
        /// </summary>
        public DateTime StartTime { get; private set; }

        public event EventHandler Starting;
        public event EventHandler Started;
        public event EventHandler Stopping;
        public event EventHandler Stopped;

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

        /// <summary>
        /// Get all types
        /// </summary>
        public IEnumerable<Type> GetAllTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).SelectMany(a => a.ExportedTypes);
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
    }
}
