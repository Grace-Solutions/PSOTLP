using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using PSOTLP.Common;
using PSOTLP.Connections;
using PSOTLP.Exporters;
using PSOTLP.Http;
using PSOTLP.Models;
using PSOTLP.Redaction;
using PSOTLP.Serialization;
using PSOTLP.Sessions;

namespace PSOTLP.Cmdlets
{
    /// <summary>
    /// Executes a script block in a controlled runspace, tapping every PowerShell stream via
    /// PSDataCollection events. Captured records are queued and drained to the OTLP log exporter.
    /// Synchronous: uses PowerShell.Invoke() and Thread-Sleep loops only. Caller-scope functions
    /// and variables can be copied into the child runspace via -ImportFunctions and
    /// -ImportVariables, and -SharedState injects a $SharedState dictionary (typically a
    /// [hashtable]::Synchronized) so the parent and child can exchange data by reference.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "OTLPScript")]
    [OutputType(typeof(PSObject))]
    public sealed class InvokeOTLPScriptCommand : OTLPCmdletBase
    {
        public const string SharedStateVariableName = "SharedState";

        [Parameter(Mandatory = true, Position = 0)]
        public ScriptBlock ScriptBlock { get; set; }

        [Parameter] public object[] ArgumentList { get; set; }
        [Parameter] public string SessionName { get; set; }
        [Parameter] public string ServiceName { get; set; }
        [Parameter] public IDictionary Attribute { get; set; }
        [Parameter] [ValidateRange(1, 10000)] public int BatchSize { get; set; } = 100;
        [Parameter] public SwitchParameter ImportFunctions { get; set; }
        [Parameter] public SwitchParameter ImportVariables { get; set; }
        [Parameter] public IDictionary SharedState { get; set; }
        [Parameter] public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var connection = OTLPSessionManager.RequireCurrentConnection();
                var session = new OTLPSession
                {
                    SessionId = Guid.NewGuid(),
                    SessionName = string.IsNullOrWhiteSpace(SessionName) ? "PSOTLPScript-" + DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssZ") : SessionName,
                    ServiceName = string.IsNullOrWhiteSpace(ServiceName) ? connection.ServiceName : ServiceName,
                    StartedAtUtc = DateTimeOffset.UtcNow,
                    IsActive = true
                };

                var queue = new OTLPSessionQueue(100000, OTLPSessionDropPolicy.DropOldest);
                var redaction = new OTLPRedactionEngine(connection.RedactPatterns);
                var attributes = HashtableToDictionary(Attribute);

                WriteVerboseLine("Invoking OTLP-captured script (sessionId=" + session.SessionId + "). Please Wait...");
                var output = ExecuteScript(session, queue, redaction, attributes);
                FlushQueue(connection, queue, session);

                if (PassThru.IsPresent && output != null)
                {
                    foreach (var item in output) { WriteObject(item); }
                }

                WriteVerboseLine("OTLP script invocation was successful (records captured=" + session.RecordsCaptured + ", exported=" + session.RecordsExported + ").");
            }
            catch (Exception ex)
            {
                HandleException("InvokeScript", ex);
            }
        }

        private System.Collections.ObjectModel.Collection<PSObject> ExecuteScript(OTLPSession session, OTLPSessionQueue queue, OTLPRedactionEngine redaction, IDictionary<string, object> attributes)
        {
            var iss = BuildInitialSessionState();
            using (var runspace = RunspaceFactory.CreateRunspace(iss))
            {
                runspace.Open();
                using (var ps = PowerShell.Create())
                {
                    ps.Runspace = runspace;
                    var streamHook = new OTLPStreamHook(queue, redaction, session, attributes);
                    streamHook.Attach(ps);

                    ps.AddScript(ScriptBlock.ToString());
                    if (ArgumentList != null) { foreach (var arg in ArgumentList) { ps.AddArgument(arg); } }

                    var results = ps.Invoke();

                    streamHook.Detach(ps);
                    streamHook.DrainOutput(results);

                    if (ps.HadErrors)
                    {
                        foreach (var err in ps.Streams.Error) { streamHook.HandleError(err); }
                    }
                    return results;
                }
            }
        }

        private InitialSessionState BuildInitialSessionState()
        {
            var iss = InitialSessionState.CreateDefault();

            if (ImportFunctions.IsPresent) { CopyFunctionsFromCaller(iss); }
            if (ImportVariables.IsPresent) { CopyVariablesFromCaller(iss); }

            if (SharedState != null)
            {
                iss.Variables.Add(new SessionStateVariableEntry(
                    SharedStateVariableName,
                    SharedState,
                    "Shared dictionary visible to both Invoke-OTLPScript caller and child runspace."));
            }

            return iss;
        }

        private void CopyFunctionsFromCaller(InitialSessionState iss)
        {
            var results = InvokeCommand.InvokeScript("Get-ChildItem -Path function: -ErrorAction SilentlyContinue");
            if (results == null) { return; }
            foreach (var psObject in results)
            {
                if (psObject == null) { continue; }
                var fn = psObject.BaseObject as FunctionInfo;
                if (fn == null || string.IsNullOrEmpty(fn.Name) || string.IsNullOrEmpty(fn.Definition)) { continue; }
                try { iss.Commands.Add(new SessionStateFunctionEntry(fn.Name, fn.Definition)); }
                catch (Exception ex) { WriteWarningLine("Skipped function '" + fn.Name + "': " + ex.Message); }
            }
        }

        private void CopyVariablesFromCaller(InitialSessionState iss)
        {
            var results = InvokeCommand.InvokeScript("Get-Variable -ErrorAction SilentlyContinue");
            if (results == null) { return; }
            foreach (var psObject in results)
            {
                if (psObject == null) { continue; }
                var variable = psObject.BaseObject as PSVariable;
                if (variable == null || string.IsNullOrEmpty(variable.Name)) { continue; }
                if (AutomaticVariableNames.Contains(variable.Name)) { continue; }
                if (SharedState != null && string.Equals(variable.Name, SharedStateVariableName, StringComparison.OrdinalIgnoreCase)) { continue; }
                try { iss.Variables.Add(new SessionStateVariableEntry(variable.Name, variable.Value, variable.Description)); }
                catch (Exception ex) { WriteWarningLine("Skipped variable '" + variable.Name + "': " + ex.Message); }
            }
        }

        private static readonly HashSet<string> AutomaticVariableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "?", "^", "$", "_", "args", "ConsoleFileName", "Error", "Event", "EventArgs",
            "EventSubscriber", "ExecutionContext", "false", "foreach", "HOME", "Host",
            "input", "LASTEXITCODE", "Matches", "MyInvocation", "NestedPromptLevel", "null",
            "OutputEncoding", "PID", "PROFILE", "PSBoundParameters", "PSCmdlet", "PSCommandPath",
            "PSCulture", "PSDebugContext", "PSHOME", "PSItem", "PSScriptRoot", "PSSenderInfo",
            "PSUICulture", "PSVersionTable", "PWD", "ShellId", "StackTrace", "switch", "this", "true"
        };

        private void FlushQueue(OTLPConnection connection, OTLPSessionQueue queue, OTLPSession session)
        {
            if (queue.Count == 0) { return; }
            var exporter = BuildExporter(connection);
            while (queue.Count > 0)
            {
                var batch = queue.DrainBatch(BatchSize);
                if (batch.Count == 0) { break; }
                try { exporter.Export(connection, batch); session.RecordsExported += batch.Count; }
                catch (Exception ex) { WriteWarningLine("OTLP script batch export failed: " + ex.Message); break; }
            }
            session.RecordsDropped = queue.Dropped;
            session.StoppedAtUtc = DateTimeOffset.UtcNow;
            session.IsActive = false;
        }

        private IOTLPLogExporter BuildExporter(OTLPConnection connection)
        {
            var http = new OTLPHttpClient(Logger, new OTLPRetryPolicy());
            var serializer = OTLPSerializerFactory.Create(connection != null ? connection.Encoding : OTLPEncoding.Json);
            var redaction = new OTLPRedactionEngine(connection != null ? connection.RedactPatterns : null);
            return new OTLPLogExporter(http, serializer, redaction, Logger);
        }

        private static IDictionary<string, object> HashtableToDictionary(IDictionary source)
        {
            if (source == null) { return null; }
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in source) { if (entry.Key != null) { result[entry.Key.ToString()] = entry.Value; } }
            return result;
        }
    }
}
