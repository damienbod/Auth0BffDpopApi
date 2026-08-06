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
                RouteId = "routeverifier",
                ClusterId = "clusterverifier",
                AuthorizationPolicy = "Anonymous",
                Match = new RouteMatch
                {
                    Path = "/oid4vp/{**catch-all}"
                }
            }
        ]; 
    }

    public static RouteConfig[] GetDevelopmentRoutes()
    {
        return
        [
            new RouteConfig()
            {
                RouteId = "routeissuer",
                ClusterId = "clusterissuer",
                AuthorizationPolicy = "Anonymous",
                Match = new RouteMatch
                {
                    Path = "/oid4vci/{**catch-all}"
                }
            },
            new RouteConfig()
            {
                RouteId = "routeissuerwellknown",
                ClusterId = "clusterissuer",
                AuthorizationPolicy = "Anonymous",
                Match = new RouteMatch
                {
                    Path = "/.well-known/{**catch-all}"
                }
            },
            new RouteConfig()
            {
                RouteId = "routeverifier",
                ClusterId = "clusterverifier",
                AuthorizationPolicy = "Anonymous",
                Match = new RouteMatch
                {
                    Path = "/oid4vp/{**catch-all}"
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
                ClusterId = "clusterverifier",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "destination1", new DestinationConfig() { Address = $"{downstreamApiUrl}/" } }
                },
                HttpClient = new HttpClientConfig { MaxConnectionsPerServer = 10, SslProtocols =  SslProtocols.Tls12 }
            }
        ];    
    }

    public static ClusterConfig[] GetDevelopmentClusters(string downstreamApiUrl)
    {
        return 
        [
            new ClusterConfig()
            {
                ClusterId = "clusterissuer",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "destination1", new DestinationConfig() { Address = $"{downstreamApiUrl}/" } }
                },
                HttpClient = new HttpClientConfig { MaxConnectionsPerServer = 10, SslProtocols =  SslProtocols.Tls12 }
            },
            new ClusterConfig()
            {
                ClusterId = "clusterverifier",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "destination1", new DestinationConfig() { Address = $"{downstreamApiUrl}/" } }
                },
                HttpClient = new HttpClientConfig { MaxConnectionsPerServer = 10, SslProtocols =  SslProtocols.Tls12 }
            }
        ];
    }
}
