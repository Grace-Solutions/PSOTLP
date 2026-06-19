using System;
using System.Management.Automation;
using PSOTLP.Sessions;

namespace PSOTLP.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "OTLPSession")]
    [OutputType(typeof(OTLPSession))]
    public sealed class GetOTLPSessionCommand : OTLPCmdletBase
    {
        [Parameter] public Guid SessionId { get; set; }
        [Parameter] public SwitchParameter IncludeCompleted { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                if (SessionId != Guid.Empty)
                {
                    var service = OTLPSessionRegistry.Get(SessionId);
                    if (service != null) { WriteObject(service.Session); return; }
                    if (IncludeCompleted.IsPresent)
                    {
                        foreach (var completed in OTLPSessionRegistry.ListCompleted())
                        {
                            if (completed.SessionId == SessionId) { WriteObject(completed); return; }
                        }
                    }
                    return;
                }

                foreach (var active in OTLPSessionRegistry.ListActive()) { WriteObject(active); }
                if (IncludeCompleted.IsPresent)
                {
                    foreach (var completed in OTLPSessionRegistry.ListCompleted()) { WriteObject(completed); }
                }
            }
            catch (Exception ex)
            {
                HandleException("GetSession", ex);
            }
        }
    }
}
