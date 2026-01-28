/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using SanteDB.BusinessRules.JavaScript.JNI;
using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Services;
using System.Data;
using System.Linq;

namespace SanteDB.BusinessRules.JavaScript.Rules
{
    /// <summary>
    /// A business rule for <see cref="Bundle"/> instances which performs a look-ahead on bundles
    /// </summary>
    /// <remarks>
    /// <para>Whenever a <see cref="Bundle"/> is submitted for processing to a SanteDB iCDR or dCDR server, 
    /// it is important that any JavaScript based business rules for object WITHIN the bundle are executed. However,
    /// we don't want to implement this in JavaScript itself since, everytime a bundle is sent to the server, 
    /// the entire bundle would be prepared (in JSON), passed to the JavaScript <see cref="JavascriptEngineBridge"/> and then 
    /// parsed back out *regardless* of whether there were any rules or not</para>
    /// <para>This business rule prevents this behavior by performing a look-ahead. It will scan the incoming bundle's objects
    /// to determine whether a JavaScript based business rule has been registered for the item, if it has, it will then 
    /// serialize the bundle, and call the appropriate rule for each object.</para>
    /// </remarks>
    /// TODO : Modify the invoke trigger to handle if the user actually did call a binding to Bundle
    internal class BundleWrapperRule : JavascriptBusinessRule<Bundle>
    {

        /// <summary>
        /// We only want to invoke the trigger when a Javascript business rule has been registered
        /// </summary>
        protected override Bundle InvokeTrigger(string triggerName, Bundle data)
        {
            for(var i = 0; i < data.Item.Count; i++)
            {
                var bres = ApplicationServiceContext.Current.GetBusinessRuleService(data.Item[i].GetType());
                while (bres != null)
                {
                    if(bres is IJavascriptBusinessRule jrb)
                    {
                        data.Item[i] = (IdentifiedData)jrb.InvokeTrigger(triggerName, data.Item[i]);
                    }
                    bres = bres.Next;
                }
            }
            return data;
        }
    }
}
