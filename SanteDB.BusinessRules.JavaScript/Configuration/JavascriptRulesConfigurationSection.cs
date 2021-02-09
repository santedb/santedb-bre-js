using Newtonsoft.Json;
using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.BusinessRules.JavaScript.Configuration
{
    /// <summary>
    /// Javascript rules configuration section
    /// </summary>
    [XmlType(nameof(JavascriptRulesConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class JavascriptRulesConfigurationSection : IConfigurationSection
    {
        /// <summary>
        /// Concurrency
        /// </summary>
        [XmlAttribute("workerPoolSize"), JsonProperty("workerPoolSize")]
        public int WorkerInstances { get; set; }

        /// <summary>
        /// True if debug mode
        /// </summary>
        [XmlAttribute("debugMode"), JsonProperty("debugMode")]
        public bool DebugMode { get; set; }
    }
}
