using Makaretu.Dns;
using System;
using Windows.ApplicationModel;

namespace StockTV.Classes
{
    public static class MdnsService
    {
        static ServiceDiscovery _service;
        static ServiceProfile _stocktvProfile;
        internal static void Advertise()
        {
            if(_stocktvProfile == null)
            {
                _stocktvProfile = new ServiceProfile(Environment.MachineName, "_stockTV._tcp.", 4747);
                _stocktvProfile.AddProperty("pubSvc", "4748");
                _stocktvProfile.AddProperty("ctrSvc", "4747");
                _stocktvProfile.AddProperty("pkgVer", GetAppVersion());
            }

            if (_service == null)
                _service = new ServiceDiscovery();

            _service.Advertise(_stocktvProfile);
        }

        internal static void Unadvertise()
        {
            _service?.Unadvertise();
        }

        internal static string GetAppVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;
            return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }
       
    }
}
