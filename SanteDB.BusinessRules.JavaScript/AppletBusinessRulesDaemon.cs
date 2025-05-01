/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-6-21
 */
using SanteDB.BusinessRules.JavaScript.Rules;
using SanteDB.Core;
using SanteDB.Core.Applets;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SanteDB.BusinessRules.JavaScript
{
    /// <summary>
    /// A daemon which loads business rules from the applet manager
    /// </summary>
    [ServiceProvider("Applet JavaScript BRE", Dependencies = new Type[] { typeof(IAppletManagerService) })]
    public class AppletBusinessRulesDaemon : IDaemonService
    {
        private IDataReferenceResolver m_dataResolver;
        private IServiceManager m_serviceManager;
        private readonly IAppletManagerService m_appletManager;
        private readonly IAppletSolutionManagerService m_solutionManager;
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(AppletBusinessRulesDaemon));

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Applet JavaScript BRE";

        /// <summary>
        /// Applet business rules daemon
        /// </summary>
        public AppletBusinessRulesDaemon(IServiceManager serviceManager, IAppletManagerService appletManager, IDataReferenceResolver dataResolver = null, IAppletSolutionManagerService solutionManagerService = null)
        {
            this.m_dataResolver = dataResolver;
            this.m_serviceManager = serviceManager;
            this.m_appletManager = appletManager;
            this.m_solutionManager = solutionManagerService;
        }

        /// <summary>
        /// Indicates whether the service is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Service has started
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// Service is starting
        /// </summary>
        public event EventHandler Starting;
        /// <summary>
        /// Service has stopped
        /// </summary>
        public event EventHandler Stopped;
        /// <summary>
        /// Service is stopping
        /// </summary>
        public event EventHandler Stopping;

        /// <summary>
        /// Start the service which will register items with the business rules handler
        /// </summary>
        public bool Start()
        {
            this.Starting?.Invoke(this, EventArgs.Empty);

            if (this.m_dataResolver == null)
            {
                this.m_serviceManager.AddServiceProvider(typeof(AppletDataReferenceResolver));
            }


            try
            {
                List<ReadonlyAppletCollection> appletsToScan = new List<ReadonlyAppletCollection>()
                {
                    this.m_appletManager.Applets
                };
                if (this.m_solutionManager != null)
                {
                    appletsToScan = appletsToScan.Union(this.m_solutionManager.Solutions.Select(o => this.m_solutionManager.GetApplets(o.Meta.Id))).ToList();
                }
                ApplicationServiceContext.Current.AddBusinessRule(typeof(BundleWrapperRule));
                foreach (var s in appletsToScan)
                {
                    foreach (var itm in s.SelectMany(c => c.Assets).Where(a => a.Name.StartsWith("rules/")))
                    {
                        using (StreamReader sr = new StreamReader(new MemoryStream(s.RenderAssetContent(itm))))
                        {
                            var script = sr.ReadToEnd();
                            JavascriptExecutorPool.Current.ExecuteGlobal(o => o.ExecuteScript(itm.ToString(), script));
                            this.m_tracer.TraceInfo("Added rules from {0}", itm.Name);
                        }
                    }
                }

                //// Instruct the rules engine to load rules
                //SanteDB.BusinessRules.JavaScript.JavascriptBusinessRulesEngine.EngineCreated += (o, e) =>
                //{
                //    foreach (var itm in appletManager.Applets.SelectMany(a => a.Assets).Where(a => a.Name.StartsWith("rules/")))
                //        using (StreamReader sr = new StreamReader(new MemoryStream(appletManager.Applets.RenderAssetContent(itm))))
                //        {
                //            e.CreatedEngine.AddRules(itm.Name, sr);
                //            this.m_tracer.TraceInfo("Added rules from {0}", itm.Name);
                //        }
                //};
            }
            catch (Exception ex)
            {
                this.m_tracer.TraceError("Error on startup: {0}", ex);
                throw new InvalidOperationException("Could not start business rules engine manager service", ex);
            }

            this.Started?.Invoke(this, EventArgs.Empty);

            return true;
        }

        /// <summary>
        /// Stopping
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);
            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }
}
