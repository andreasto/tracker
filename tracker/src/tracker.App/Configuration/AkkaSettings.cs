using System.Net;
using Akka.Cluster.Hosting;
using Akka.Management;
using Akka.Remote.Hosting;

namespace tracker.App.Configuration;

/// <summary>
/// Determines which Akka.Discovery method to use when discovering other nodes to form and join clusters.
/// </summary>
public enum DiscoveryMethod
{
    Config,
    Kubernetes,
    AwsEcsTagBased,
    AwsEc2TagBased,
    AzureTableStorage
}

public enum PersistenceMode
{
    InMemory,
    Azure,
    PostgreSql
}

public class AzureStorageSettings
{
    public string ConnectionStringName { get; set; } = "Azurite";
}

public class AkkaManagementOptions
{
    public bool Enabled { get; set; }
    public string Hostname { get; set; } = "localhost";
    public int Port { get; set; } = 8558;
    public string ServiceName { get; set; } = "TrackerApi";
    public string PortName { get; set; } = "management";
    public int RequiredContactPointsNr { get; set; } = 1;
    public DiscoveryMethod DiscoveryMethod { get; set; } = DiscoveryMethod.Config;
}

public class AkkaSettings
{
    public string ActorSystemName { get; set; } = "TrackerApi";

    public bool UseClustering { get; set; } = true;

    public bool LogConfigOnStart { get; set; }

    public RemoteOptions RemoteOptions { get; set; } = new()
    {
        // can be overridden via config, but is dynamic by default
        PublicHostName = Dns.GetHostName()
    };

    public ClusterOptions ClusterOptions { get; set; } = new ClusterOptions()
    {
        // use our dynamic local host name by default
        SeedNodes = new[] { $"akka.tcp://TrackerApi@{Dns.GetHostName()}:8081" }
    };

    public ShardOptions ShardOptions { get; set; } = new ShardOptions();

    public PersistenceMode PersistenceMode { get; set; } = PersistenceMode.InMemory;

    public AkkaManagementOptions? AkkaManagementOptions { get; set; }
}

