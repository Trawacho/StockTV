using Makaretu.Dns;
using System;

namespace StockTV.Classes
{
    public static class MdnsService
    {
        static ServiceProfile profile;
        static ServiceDiscovery service;
        internal static void Advertise()
        {
            if (profile == null)
                profile = new ServiceProfile(Environment.MachineName, "_stockapp._tcp.local.", 4747);
            if (service == null)
                service = new ServiceDiscovery();

            service.Advertise(profile);
        }

        internal static void Unadvertise()
        {
            service?.Unadvertise();
            MdnsService.Dispose();
        }

        internal static void Dispose()
        {
            profile = null;
            service?.Dispose();
        }
    }
}
