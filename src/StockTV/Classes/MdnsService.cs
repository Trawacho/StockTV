using Makaretu.Dns;
using System;
using Windows.ApplicationModel;

namespace StockTV.Classes
{
    public static class MdnsService
    {
        static ServiceProfile appProfile;
        static ServiceDiscovery service;
        static ServiceProfile publishProfile;
        internal static void Advertise()
        {
            if (appProfile == null)
            {
                appProfile = new ServiceProfile(Environment.MachineName, "_stockapp._tcp.local", 4747);
                appProfile.AddProperty("pkgVer", GetAppVersion());
            }

            if (publishProfile == null)
            {
                publishProfile = new ServiceProfile(Environment.MachineName, "_stockpub._tcp.local", 4748);
                publishProfile.AddProperty("pkgVer", GetAppVersion());
            }

            if (service == null)
                service = new ServiceDiscovery();

            service.Advertise(appProfile);
            service.Advertise(publishProfile);
        }

        internal static void Unadvertise()
        {
            service?.Unadvertise();
        }

        private static string GetAppVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;
            return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }
       
    }
}
