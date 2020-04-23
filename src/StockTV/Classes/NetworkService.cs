using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace StockTV.Classes
{
    public static class NetworkService
    {
        
        public static async Task SendMessage(Match match)
        {
            (IPAddress address, IPAddress mask) ip_mask = GetIpAndMask();

            IPAddress broadcast = GetBroadcastAddress(ip_mask.address, ip_mask.mask);
            var ep = new IPEndPoint(broadcast, 4700);

            var datagramm = match.Serialize(true); 
            using(var sendClient = new UdpClient())
            {
                sendClient.ExclusiveAddressUse = false;
                sendClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                int xx = await sendClient.SendAsync(datagramm, datagramm.Length, ep);
            }
        }

        private static string MatchToMessage(Match match)
        {
            var message = string.Empty;
            message += Settings.Instance.CourtNumber.ToString("X2");
            foreach (var game in match.Games)
            {
                message += game.GameNumber.ToString("X2");
                foreach (var t in game.Turns)
                {
                    message += t.PointsLeft.ToString("X2");
                    message += t.PointsRight.ToString("X2");
                }
            }
            return message;
        }

        public static byte[] Compress(byte[] data)
        {
            var output = new MemoryStream();
            using (var dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }


        public static IPAddress GetBroadcastAddress(IPAddress address, IPAddress mask)
        {
            uint ipAddress = BitConverter.ToUInt32(address.GetAddressBytes(), 0);
            uint ipMaskV4 = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
            uint broadCastIpAddress = ipAddress | ~ipMaskV4;

            return new IPAddress(BitConverter.GetBytes(broadCastIpAddress));
        }


        public static (IPAddress address, IPAddress mask) GetIpAndMask()
        {
            IPAddress a = default;
            IPAddress m = default;
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("10.0.0.1", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    a = endPoint.Address;
                }
                foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
                {
                    foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            if (a.Equals(unicastIPAddressInformation.Address))
                            {
                                m = unicastIPAddressInformation.IPv4Mask;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            return (a, m);
        }



    }
}
