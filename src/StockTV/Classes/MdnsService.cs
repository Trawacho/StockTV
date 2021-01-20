using Makaretu.Dns;
using System;
using Windows.ApplicationModel;

namespace StockTV.Classes
{
    public static class MdnsService
    {
        static ServiceProfile profile;
        static ServiceDiscovery service;
        internal static void Advertise()
        {
            if (profile == null)
            {
                profile = new ServiceProfile(Environment.MachineName, "_stockapp._tcp.", 4747);
                profile.AddProperty("pkgVer", Package.Current.Id.Version.ToString());
                profile.AddProperty("ResultInfo", "4748");
            }
            if (service == null)
                service = new ServiceDiscovery();

            service.Advertise(profile);
        }

        internal static void Unadvertise()
        {
            service?.Unadvertise();
        }

       
    }
}
