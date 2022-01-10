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
    /// Represents a rule which is tied JavaScript which executes a bundle 
    /// </summary>
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
