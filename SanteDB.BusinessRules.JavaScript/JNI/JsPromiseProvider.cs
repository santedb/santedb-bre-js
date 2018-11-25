/*
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
 * User: justin
 * Date: 2018-7-23
 */
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
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
        private ManualResetEvent m_completeEvent = new ManualResetEvent(false);

        /// <summary>
        /// Public constructor for promise
        /// </summary>
        public JsPromiseProvider(Action<JsPromiseCallback, JsPromiseCallback> asyncFunc)
        {

            this.m_tracer.TraceVerbose("Creating async promise on thread pool");
            var threadPool = (IThreadPoolService)ApplicationServiceContext.Current.GetService(typeof(IThreadPoolService));

            var tsync = s_syncObject;

            threadPool.QueueUserWorkItem(
                (o) =>
                {
                    var worker = o as Action<JsPromiseCallback, JsPromiseCallback>;
                    lock (tsync)
                    {
                        worker((f) =>
                        {
                            this.m_thenCallback(f);
                        }, (r) =>
                        {
                            this.m_catchCallback(r);
                        });
                    }
                    this.m_completed = true;
                    this.m_completeEvent.Set();
                }, asyncFunc);
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
                t.m_completeEvent.WaitOne();
        }
    }
}
