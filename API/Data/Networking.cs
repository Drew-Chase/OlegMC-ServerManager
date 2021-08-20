using Open.Nat;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OlegMC.REST_API.Data
{
    public class Networking
    {
        public static object Thead { get; private set; }

        public static async Task OpenPort(int port, string description = "OlegMC Server Manager")
        {
            try
            {
                await ClosePort(port);
                NatDiscoverer discoverer = new NatDiscoverer();
                CancellationTokenSource cts = new CancellationTokenSource(10000);
                NatDevice device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);
                Mapping map = new(Protocol.Tcp, port, port, description);
                await device.CreatePortMapAsync(map);

                map = new(Protocol.Udp, port, port, description);
                await device.CreatePortMapAsync(map);
                Console.WriteLine($"Created {map}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
        public static async Task ClosePort(int port)
        {
            NatDiscoverer nat = new();
            CancellationTokenSource cts = new(5000);
            NatDevice device = await nat.DiscoverDeviceAsync(PortMapper.Upnp, cts);

            foreach (Mapping mapping in await device.GetAllMappingsAsync())
            {
                if (mapping.PrivatePort == port)
                {
                    Console.WriteLine($"Deleting {mapping}");
                    await device.DeletePortMapAsync(mapping);
                }
            }
        }
        public static async Task<bool> IsPortOpen(int port)
        {
            NatDiscoverer nat = new();
            CancellationTokenSource cts = new(5000);
            NatDevice device = await nat.DiscoverDeviceAsync(PortMapper.Upnp, cts);

            foreach (Mapping mapping in await device.GetAllMappingsAsync())
            {
                if (mapping.PrivatePort == port)
                {
                    return true;
                }
            }
            return false;
        }

        public static async Task ListPorts()
        {
            NatDiscoverer nat = new();
            CancellationTokenSource cts = new(5000);
            NatDevice device = await nat.DiscoverDeviceAsync(PortMapper.Upnp, cts);

            foreach (Mapping mapping in await device.GetAllMappingsAsync())
            {
                Console.WriteLine($"OPENED => {mapping}");
            }
        }
    }
}
