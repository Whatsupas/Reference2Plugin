using System;
using Microsoft.Xrm.Sdk;
using System.Linq;

namespace RTFirstPlugin
{
    // Important:

    // [0] Microsoft.CrmSdk.CoreAssemblies.
    // [1] Microsoft.PowerPlatform.Dataverse.Client
    // [2] Manage plug-ins in single solution.
    // [3] Consolidate plug-ins into a single assembly.
    // [4] When working with Web Access is important to set timeout.
    // [5] Shared variables in the context of a plugin refer to variables that are shared between different steps of the plugin execution.
    //     These variables can be used to store and pass data between different steps, allowing for communication and coordination between
    //     the various parts of the plugin. This can be useful for maintaining state or passing data between different stages of the plugin's execution.
    // [6] Plugins are designed to be stateless and not dependent on previous executions.
    // [7] Plugin-in should be very focused operations that execute quicly, minimizing blocking to improve user experience and avoid timeout.

    public class PreOperationFormatPhoneCreateUpdate : IPlugin
    {
        private const string DEFAULT_PRE_IMAGE = "PreImageDemo"; 
        private const string DEFAULT_SHARED_VARIABLE_KEY = "SharedVariableKey";
        private const string DEFAULT_SHARED_VARIABLE_VALUE = "Demo value";

        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

                if (!context.InputParameters.ContainsKey("Target") || !(context.InputParameters["Target"] is Entity))
                    throw new InvalidPluginExecutionException("No target found");

                var entity = context.InputParameters["Target"] as Entity;

                // Logs Key/value pair per each input parameter
                TraceKeyValuePairCollection(context.InputParameters, tracingService);

                if (!entity.Attributes.Contains("firstname"))
                    return;

                entity["telephone1"] = entity["telephone1"].ToString().ToUpper();

                // Logs Key/value pair per each attribute
                TraceKeyValuePairCollection(entity.Attributes, tracingService);

                // Testing server environment
                tracingService.Trace(Environment.CurrentManagedThreadId.ToString() + Environment.NewLine);
                tracingService.Trace(DateTime.Now + Environment.NewLine);
                tracingService.Trace(TimeZone.CurrentTimeZone.StandardName + Environment.NewLine);
                tracingService.Trace(Environment.MachineName + Environment.NewLine);
                tracingService.Trace(Environment.ProcessorCount + Environment.NewLine);

               
                // Testing just for fun. Async calls inside Sync plugin.
                // In reality we should avoid parallel processing inside execute method!
                // bcs i believe that not all dataverse collections are thread safe (use concurrent collections in such cases and repopulate dataverse colections but anyway not recommended TAP and TPL here)
                // and bcs of potentials deadlocks while operating with dataverse

                // Task.Run(() => entity["jobtitle"] = "Developer Async");
                // Task.Run(() => entity["jobtitle"] = "Developer Asy");
                // Task.Run(() => entity["jobtitle"] = "Developer A");
                // Task.Run(() => entity["fax"] = new Random().Next().ToString());

                // Testing max timeout (2 min)
                // Delay 2.3min (138000 ms) 
                // Thread.Sleep(138000);

                // Testing exception
                // throw new InvalidPluginExecutionException("testing exception");

                // Testing pre-image
                if (context.PreEntityImages.Contains(DEFAULT_PRE_IMAGE))
                {
                    var preImage = context.PreEntityImages[DEFAULT_PRE_IMAGE];

                    if (preImage.Contains("telephone1"))
                    {
                        var msg = preImage["telephone1"].ToString();
                        tracingService.Trace(msg);
                    }
                }
                else
                {
                    tracingService.Trace($"Missing default Pre-Image: {DEFAULT_PRE_IMAGE}");
                }

                // Testing shared variables
                if (context.SharedVariables.Contains(DEFAULT_SHARED_VARIABLE_KEY))
                {
                    tracingService.Trace($"Retrieving existing shared variable. KEY: {DEFAULT_SHARED_VARIABLE_KEY} VALUE: {context.SharedVariables[DEFAULT_SHARED_VARIABLE_KEY]}" + Environment.NewLine);
                }
                else
                {
                    context.SharedVariables.Add(DEFAULT_SHARED_VARIABLE_KEY, DEFAULT_SHARED_VARIABLE_VALUE);
                    tracingService.Trace($"Adding new shared variable. KEY: {DEFAULT_SHARED_VARIABLE_KEY} VALUE: {context.SharedVariables[DEFAULT_SHARED_VARIABLE_KEY]}" + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                 throw new InvalidPluginExecutionException(ex.Message);
            }
        }

        /// <summary>
        /// Logs key-value information from collection of interest
        /// /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pairCollection"></param>
        /// <param name="tracingService"></param>
        private void TraceKeyValuePairCollection<T>(T pairCollection, ITracingService tracingService) where T : DataCollection<string,object>
        {
            tracingService?.Trace(pairCollection?.Aggregate(string.Empty, (currentKeyValuePair, nextKeyValuePair) => currentKeyValuePair + nextKeyValuePair.Key + " | " + GetLogicalName(nextKeyValuePair.Value) + Environment.NewLine));
        }

        /// <summary>
        /// Retrievs logical name from node of interest. 
        /// Expand as needed.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private string GetLogicalName(object node) 
        {
            switch (node)
            {
                case null:
                    return "NULL";
                case Entity entity:
                    return entity.LogicalName;
                case EntityReference entityReference:
                    return entityReference.LogicalName;
                default:
                    return node.ToString();    
            }
        }
    }

}
