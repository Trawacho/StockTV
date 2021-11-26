using Makaretu.Dns;
using System;
using Windows.ApplicationModel;

namespace StockTV.Classes
{
    public static class MdnsService
    {
        static ServiceDiscovery service;
        static ServiceProfile stocktvProfile;
        internal static void Advertise()
        {
            if(stocktvProfile == null)
            {
                stocktvProfile = new ServiceProfile(Environment.MachineName, "_stockTV._tcp.", 4747);
                stocktvProfile.AddProperty("pubSvc", "4748");
                stocktvProfile.AddProperty("ctrSvc", "4747");
                stocktvProfile.AddProperty("pkgVer", GetAppVersion());
            }

            if (service == null)
                service = new ServiceDiscovery();

            service.Advertise(stocktvProfile);
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
