using System.Security.Authentication;
using Yarp.ReverseProxy.Configuration;

namespace BffAuth0.Server;

public static class YarpConfigurations
{
    public static RouteConfig[] GetProductionRoutes()
    {
        return
        [
            new RouteConfig()
            {
                RouteId = "routedownstreamapi",
                ClusterId = "clusterdownstreamapi",
                AuthorizationPolicy = "Anonymous", // TODO fix
                Match = new RouteMatch
                {
                    Path = "/api/DownstreamYarpData/{**catch-all}"
                }
            }
        ]; 
    }

    public static ClusterConfig[] GetProductionClusters(string downstreamApiUrl)
    {
        return
        [
            new ClusterConfig()
            {
                ClusterId = "clusterdownstreamapi",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "destination1", new DestinationConfig() { Address = $"{downstreamApiUrl}/" } }
                },
                HttpClient = new HttpClientConfig { MaxConnectionsPerServer = 10, SslProtocols =  SslProtocols.Tls12 }
            }
        ];
    }
}
