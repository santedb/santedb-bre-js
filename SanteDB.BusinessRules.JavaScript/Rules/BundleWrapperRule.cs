﻿using SanteDB.BusinessRules.JavaScript.JNI;
using SanteDB.Core;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            if (data.Item.Select(i => i.GetType()).Distinct().Any(t =>
               {
                   var jreType = typeof(JavascriptBusinessRule<>).MakeGenericType(t);
                   var bres = ApplicationServiceContext.Current.GetBusinessRuleService(t);
                   while (bres != null)
                   {
                       if (jreType.IsAssignableFrom(bres.GetType()))
                           return true;
                       bres = bres.Next;
                   }
                   return false;
               }))
            {
                return base.InvokeTrigger(triggerName, data);
            }
            else
            {
                return data;
            }
        }
    }
}