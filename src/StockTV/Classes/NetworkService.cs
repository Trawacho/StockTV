using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace StockTV.Classes
{
    public static class NetworkService
    {

        class UdpState
        {
            public UdpClient u;
            public IPEndPoint e;
        }


        public static void SendData(byte[] data)
        {
            UdpState state = new UdpState()
            {
                u = new UdpClient(),
                e = new IPEndPoint(Settings.Instance.BroadcastAddress, Settings.Instance.BroadcastPort)
            };

            state.u.BeginSend(data, data.Length, state.e, new AsyncCallback(SendCallback), state);

        }

        static void SendCallback(IAsyncResult ar)
        {
            var state = (UdpState)ar.AsyncState;
            state.u.EndSend(ar);
        }

        public static IPAddress GetBroadcastAddress()
        {
            (IPAddress a, IPAddress m) = GetIPAddressAndSubnetMask();
            return GetBroadcastAddress(a, m);
        }
       
        public static (IPAddress address, IPAddress mask, IPAddress broadcast) GetIPAddresses()
        {
            (IPAddress address, IPAddress mask) = GetIPAddressAndSubnetMask();
            var broadcast = GetBroadcastAddress(address, mask);
            return (address, mask, broadcast);
        }

        static IPAddress GetBroadcastAddress(IPAddress ipAddress, IPAddress subnetMask)
        {
            uint address = BitConverter.ToUInt32(ipAddress.GetAddressBytes(), 0);
            uint mask = BitConverter.ToUInt32(subnetMask.GetAddressBytes(), 0);
            uint broadCastIpAddress = address | ~mask;

            return new IPAddress(BitConverter.GetBytes(broadCastIpAddress));
        }


        static (IPAddress address, IPAddress mask) GetIPAddressAndSubnetMask()
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
