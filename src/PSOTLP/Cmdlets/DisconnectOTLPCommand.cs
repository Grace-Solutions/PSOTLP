using System;
using System.Management.Automation;
using PSOTLP.Connections;

namespace PSOTLP.Cmdlets
{
    [Cmdlet(VerbsCommunications.Disconnect, "OTLP")]
    [OutputType(typeof(OTLPConnectionView))]
    public sealed class DisconnectOTLPCommand : OTLPCmdletBase
    {
        [Parameter] public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var current = OTLPSessionManager.CurrentConnection;
                OTLPConnectionView view = null;
                if (current != null && PassThru.IsPresent) { view = OTLPConnectionView.From(current); }

                WriteVerboseLine("Disconnecting OTLP connection. Please Wait...");
                OTLPSessionManager.Disconnect();
                WriteVerboseLine("OTLP connection cleared.");

                if (view != null) { view.IsConnected = false; WriteObject(view); }
            }
            catch (Exception ex)
            {
                HandleException("Disconnect", ex);
            }
        }
    }
}
