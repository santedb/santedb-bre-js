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
 * Date: 2018-6-21
 */
using Newtonsoft.Json;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.BusinessRules.JavaScript
{


    /// <summary>
    /// Represents a rule service base binding
    /// </summary>
    public class RuleServiceBase<TBinding> : IBusinessRulesService<TBinding> where TBinding : IdentifiedData
    {
        /// <summary>
        /// Gets or sets the next binding to run
        /// </summary>
        public IBusinessRulesService<TBinding> Next { get; set; }

        /// <summary>
        /// Invokes the specified trigger if one is registered
        /// </summary>
        /// <param name="triggerName">The name of the trigger to run</param>
        /// <param name="data">The data to be used in the trigger</param>
        /// <returns>The result of the trigger</returns>
        private TBinding InvokeTrigger(String triggerName, TBinding data)
        {
            if (JavascriptBusinessRulesEngine.Current.HasRule<TBinding>(triggerName, data?.GetType()))
                using (var instance = JavascriptBusinessRulesEngine.GetThreadInstance())
                    return instance.Invoke(triggerName, data);
            else
                return data;
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
        public IEnumerable<TBinding> AfterQuery(IEnumerable<TBinding> results)
        {
            // Invoke the business rule
            if (results.Any() && JavascriptBusinessRulesEngine.Current.HasRule<TBinding>("AfterQuery", typeof(Bundle)))
                using (var instance = JavascriptBusinessRulesEngine.GetThreadInstance())
                {
                    var retVal = instance.Invoke("AfterQuery", new Bundle() { Item = results.OfType<IdentifiedData>().ToList() }).Item.OfType<TBinding>();
                    return this.Next?.AfterQuery(retVal) ?? retVal;
                }
            else
                return this.Next?.AfterQuery(results) ?? results;

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
            using (var instance = JavascriptBusinessRulesEngine.GetThreadInstance())
            {
                var retVal = instance.Validate(data);
                return retVal.Union(this.Next?.Validate(data)).ToList() ?? retVal;
            }
        }

    }
}
