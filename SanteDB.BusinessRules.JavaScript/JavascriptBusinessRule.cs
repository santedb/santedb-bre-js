﻿/*
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
 * Date: 2021-8-27
 */

using SanteDB.Core.BusinessRules;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.BusinessRules.JavaScript
{
    /// <summary>
    /// Wrapper for <see cref="IBusinessRulesService"/> which calls one or more JavaScript functions
    /// </summary>
    /// <remarks>
    /// <para>This implementation of the business rules wrapper is used to interface with business rules written in 
    /// <see href="https://help.santesuite.org/developers/applets/business-rules">SanteDB's JavaScript Business Rules Engine</see> and allows 
    /// implementers to customize the behavior of the iCDR or dCDR with JavaScript within their applets. Whenever a call to the <c>SanteDBBre.AddBusinessRule()</c>
    /// interface is called from applet initialization, a new instance of this class is chained into the execution pipeline. From there, the events raised
    /// for before/after insert/update/obsolete/query are translated to JavaScript and the provided callback is executed.</para>
    /// </remarks>
    internal class JavascriptBusinessRule<TBinding> : IBusinessRulesService<TBinding> where TBinding : IdentifiedData
    {
        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(JavascriptBusinessRule<TBinding>));

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => $"Business rules service for {typeof(TBinding).FullName}";

        /// <summary>
        /// Gets the next implementation
        /// </summary>
        IBusinessRulesService IBusinessRulesService.Next => this.Next;

        /// <summary>
        /// Gets or sets the next binding to run
        /// </summary>
        public IBusinessRulesService<TBinding> Next
        {
            get;
            set;
        }

        /// <summary>
        /// Invokes the specified trigger if one is registered
        /// </summary>
        /// <param name="triggerName">The name of the trigger to run</param>
        /// <param name="data">The data to be used in the trigger</param>
        /// <returns>The result of the trigger</returns>
        protected virtual TBinding InvokeTrigger(String triggerName, TBinding data)
        {
            try
            {
                return (TBinding)JavascriptExecutorPool.Current.Execute((e, d) => e.Invoke(triggerName, d), data);
            }
            catch (Exception e)
            {
                // TODO: Refactor this to the DetectedIssue extension
                this.m_tracer.TraceWarning("Error running {0} on {1} - The business rule has been ignored - {2}", triggerName, data, e);
                if (data is Entity entity)
                    entity.Tags.Add(new Core.Model.DataTypes.EntityTag("$bre.error", e.Message));
                else if (data is Act act)
                    act.Tags.Add(new Core.Model.DataTypes.ActTag("$bre.error", e.Message));
                return data;
            }
        }

        /// <summary>
        /// Fire after insert on the type
        /// </summary>
        public TBinding AfterInsert(TBinding data)
        {
            var retVal = this.InvokeTrigger("AfterInsert", data);
            return this.Next?.AfterInsert(retVal) ?? retVal;
        }

        /// <summary>
        /// After obsoletion
        /// </summary>
        public TBinding AfterObsolete(TBinding data)
        {
            var retVal = this.InvokeTrigger("AfterObsolete", data);
            return this.Next?.AfterObsolete(retVal) ?? retVal;
        }

        /// <summary>
        /// After query is complete
        /// </summary>
        public IQueryResultSet<TBinding> AfterQuery(IQueryResultSet<TBinding> results)
        {
            IQueryResultSet<TBinding> resultSet = new NestedQueryResultSet<TBinding>(results, (o) => (TBinding)JavascriptExecutorPool.Current.Execute((e, i) => e.Invoke("AfterQuery", i), o));
            if (this.Next != null)
            {
                resultSet = resultSet.Union(this.Next.AfterQuery(results));
            }
            return results;
        }

        /// <summary>
        /// After retrieve
        /// </summary>
        public TBinding AfterRetrieve(TBinding result)
        {
            var retVal = this.InvokeTrigger("AfterRetrieve", result);
            return this.Next?.AfterRetrieve(retVal) ?? retVal;
        }

        /// <summary>
        /// After update
        /// </summary>
        public TBinding AfterUpdate(TBinding data)
        {
            var retVal = this.InvokeTrigger("AfterUpdate", data);
            return this.Next?.AfterUpdate(retVal) ?? retVal;
        }

        /// <summary>
        /// Before insert
        /// </summary>
        public TBinding BeforeInsert(TBinding data)
        {
            var retVal = this.InvokeTrigger("BeforeInsert", data);
            return this.Next?.BeforeInsert(retVal) ?? retVal;
        }

        /// <summary>
        /// Before an obsoletion
        /// </summary>
        public TBinding BeforeObsolete(TBinding data)
        {
            var retVal = this.InvokeTrigger("BeforeObsolete", data);
            return this.Next?.BeforeObsolete(retVal) ?? retVal;
        }

        /// <summary>
        /// Before Update
        /// </summary>
        public TBinding BeforeUpdate(TBinding data)
        {
            var retVal = this.InvokeTrigger("BeforeUpdate", data);
            return this.Next?.BeforeUpdate(retVal) ?? retVal;
        }

        /// <summary>
        /// Validate the object
        /// </summary>
        public List<DetectedIssue> Validate(TBinding data)
        {
            var retVal = JavascriptExecutorPool.Current.Execute((e, d) => e.Validate(d), data) as List<DetectedIssue>;
            return retVal.Union(this.Next?.Validate(data) ?? new List<DetectedIssue>()).ToList() ?? retVal;
        }

        /// <summary>
        /// After insert trigger generic
        /// </summary>
        public object AfterInsert(object data)
        {
            return this.AfterInsert((TBinding)data);
        }

        /// <summary>
        /// After obsoletion occurs
        /// </summary>
        public object AfterObsolete(object data)
        {
            return this.AfterObsolete((TBinding)data);
        }

        /// <summary>
        /// After query occurs
        /// </summary>
        public IQueryResultSet AfterQuery(IQueryResultSet results)
        {
            IQueryResultSet resultSet = new NestedQueryResultSet(results, (o) => JavascriptExecutorPool.Current.Execute<TBinding>((e, i) => e.Invoke("AfterQuery", (TBinding)i), (TBinding)o));
            if (this.Next != null)
            {
                resultSet = resultSet.Union(this.Next.AfterQuery(results));
            }
            return results;
        }

        /// <summary>
        /// After retrieve
        /// </summary>
        public object AfterRetrieve(object result)
        {
            return this.AfterRetrieve((TBinding)result);
        }

        /// <summary>
        /// After update occurs
        /// </summary>
        public object AfterUpdate(object data)
        {
            return this.AfterUpdate((TBinding)data);
        }

        /// <summary>
        /// Before insert occurs
        /// </summary>
        public object BeforeInsert(object data)
        {
            return this.BeforeInsert((TBinding)data);
        }

        /// <summary>
        /// Before obsoletion occurs
        /// </summary>
        public object BeforeObsolete(object data)
        {
            return this.BeforeObsolete((TBinding)data);
        }

        /// <summary>
        /// Before update occurs
        /// </summary>
        public object BeforeUpdate(object data)
        {
            return this.BeforeUpdate((TBinding)data);
        }

        /// <summary>
        /// Validate the object
        /// </summary>
        public List<DetectedIssue> Validate(object data)
        {
            return this.Validate((TBinding)data);
        }
    }
}