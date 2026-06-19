namespace PSOTLP.Http
{
    public interface IOTLPHttpClient
    {
        OTLPHttpResponse Send(OTLPHttpRequest request);
    }
}
