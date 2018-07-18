using System;
using System.Reflection;
using System.Threading;
using Jint.Runtime.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanteDB.BusinessRules.JavaScript.JNI;
using SanteDB.Core;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Services;

namespace SanteDB.BusinessRules.JavaScript.Test
{
    /// <summary>
    /// Tests business rules javascript promises to do operations asynchronously 
    /// </summary>
    [TestClass]
    public class TestPromises
    {

        // The JInt Engine used for testing promises
        private static Jint.Engine s_engine;

        /// <summary>
        /// Load simple rules
        /// </summary>
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ApplicationServiceContext.Current = new SimpleServiceContext();
            (ApplicationServiceContext.Current as IServiceManager).AddServiceProvider(typeof(SanteDBThreadPool));

            s_engine = new Jint.Engine(cfg => cfg.AllowClr(
                    typeof(SanteDB.Core.Model.BaseEntityData).GetTypeInfo().Assembly,
                    typeof(IBusinessRulesService<>).GetTypeInfo().Assembly
                )
                .Strict(false)
#if DEBUG
                .DebugMode(true)
#else
                .DebugMode(s_debugMode)
#endif

                )
                .SetValue("SanteDBBre", new BusinessRulesBridge())
                .SetValue("console", new JsConsoleProvider())
                .SetValue("reflector", new JsObjectProvider());
            s_engine.SetValue("Promise", TypeReference.CreateTypeReference(s_engine, typeof(JsPromiseProvider)));
        }

        /// <summary>
        /// Tests that a promise should fulfill
        /// </summary>
        [TestMethod]
        public void PromimseShouldFulfill()
        {
            s_engine.Execute(
@"var foo; 
new Promise(function(fulfill, reject) { 
    for (var i = 0; i < 5000; i++)
        console.debug(i);
    fulfill('hi!');
})
    .then(function(d) { console.debug('FULFILL '); foo = d;  })
    .catch(function(e) { console.debug('REJECTED'); });
for(var i = 0; i < 1000; i++) 
    console.debug('I AM HERE!' + i);");
           Thread.Sleep(4000); // Give the async process 4 sec to finish
           var result = s_engine.GetValue("foo");
           Assert.AreEqual("hi!", result.AsString());
        }

        /// <summary>
        /// Tests that a promise should fulfill
        /// </summary>
        [TestMethod]
        public void PromimseShouldWaitAll()
        {
            s_engine.Execute(
@"
var promises = [
    (new Promise(function (fulfill, reject) {
        for (var i = 0; i < 500; i++)
            console.debug('P1-' + i);
        fulfill('P1');
    })
        .then(function (d) { console.debug('P1 FULFILL '  + d); })
        .catch(function (e) { console.debug('P1 REJECTED'); })),
    (new Promise(function (fulfill, reject) {
        for (var i = 0; i < 500; i++)
            console.debug('P2-' + i);
        fulfill('P2');
    })
        .then(function (d) { console.debug('P2 FULFILL ' + d); })
        .catch(function (e) { console.debug('P2 REJECTED'); }))
];
Promise.all(promises);
console.debug('AFTER ALL');");
            Thread.Sleep(4000); // Give the async process 4 sec to finish
            var result = s_engine.GetValue("foo");
        }
    }
}
