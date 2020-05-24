/*
 * Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
 * Copyright (C) 2019 - 2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Linq;
using System.Threading;

namespace SanteDB.BusinessRules.JavaScript.JNI
{

    /// <summary>
    /// Javascript promise callback
    /// </summary>
    /// <param name="result">The result of the callback</param>
    public delegate void JsPromiseCallback(Object result);

    /// <summary>
    /// Represents a class which provides basic promise support
    /// </summary>
    public class JsPromiseProvider
    {

        // Synclock object
        [ThreadStatic]
        private static object s_syncObject = new object();
        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(JsPromiseProvider));
        // Then callback
        private Action<Object> m_thenCallback = null;
        // Catch callback
        private Action<Object> m_catchCallback = null;
        // Async result
        private Object m_asyncResult = null;
        private Object m_asyncReject = null;
        private bool m_completed = false;
        private ManualResetEventSlim m_completeEvent = new ManualResetEventSlim(false);
        private ManualResetEventSlim m_thenSetEvent = new ManualResetEventSlim(false);

        /// <summary>
        /// Public constructor for promise
        /// </summary>
        public JsPromiseProvider(Action<JsPromiseCallback, JsPromiseCallback> asyncFunc)
        {

            this.m_tracer.TraceVerbose("Creating async promise on thread pool");
            var threadPool = (IThreadPoolService)ApplicationServiceContext.Current.GetService(typeof(IThreadPoolService));

            if (s_syncObject == null)
                s_syncObject = new object();
            var tsync = s_syncObject;

            Action<Object> workerCallback = (o) =>
            {
                try
                {
                    AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
                    var worker = o as Action<JsPromiseCallback, JsPromiseCallback>;
                    lock (tsync)
                    {
                        worker((f) =>
                        {
                            this.m_asyncResult = f;
                            this.m_thenCallback?.Invoke(f);
                        }, (r) =>
                        {
                            this.m_asyncReject = r;
                            this.m_catchCallback?.Invoke(r);
                        });
                    }
                    this.m_completed = true;
                    this.m_completeEvent.Set();
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error in Promise: {0}", e);
                    this.m_completeEvent.Set();

                }
            };

            if (threadPool != null)
                threadPool.QueueUserWorkItem(workerCallback, asyncFunc);
            else
            {
                workerCallback.BeginInvoke(asyncFunc, null, null);
            }
        }

        /// <summary>
        /// The then callback
        /// </summary>
        /// <param name="jsCallback"></param>
        public Object then(Action<Object> jsCallback)
        {
            if (this.m_asyncResult != null)
                jsCallback(this.m_asyncResult);
            this.m_thenCallback = jsCallback;
            return this;
        }

        /// <summary>
        /// Catch callback
        /// </summary>
        /// <param name="jsCallback"></param>
        public Object @catch(Action<Object> jsCallback)
        {
            if (this.m_asyncReject != null)
                jsCallback(this.m_asyncReject);
            this.m_catchCallback = jsCallback;
            return this;
        }

        /// <summary>
        /// Wait for all promises to finish before continuing
        /// </summary>
        public static void all(JsPromiseProvider[] promises)
        {
            var unfinished = promises.ToList();
            unfinished.RemoveAll(p => p.m_completed);
            foreach (var t in unfinished)
                t.m_completeEvent.Wait();
        }
    }
}
