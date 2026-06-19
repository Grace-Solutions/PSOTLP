using System;
using System.Management.Automation;
using PSOTLP.Connections;

namespace PSOTLP.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "OTLPConnection")]
    [OutputType(typeof(OTLPConnectionView))]
    public sealed class GetOTLPConnectionCommand : OTLPCmdletBase
    {
        protected override void ProcessRecord()
        {
            try
            {
                var current = OTLPSessionManager.CurrentConnection;
                if (current == null)
                {
                    WriteVerboseLine("No active OTLP connection.");
                    return;
                }
                WriteObject(OTLPConnectionView.From(current));
            }
            catch (Exception ex)
            {
                HandleException("Get", ex);
            }
        }
    }
}
