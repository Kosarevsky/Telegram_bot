using System.Net;


namespace Services.Interfaces
{
    public interface IProxyProvider
    {
        WebProxy GetRandomProxy();
    }
}
