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
            public UdpClient udpclient;
            public IPEndPoint endPoint;
        }

        /// <summary>
        /// Send data to Broadcast address
        /// </summary>
        /// <param name="data"></param>
        public static void SendData(byte[] data)
        {
            UdpState state = new UdpState()
            {
                udpclient = new UdpClient(),
                endPoint = new IPEndPoint(Settings.Instance.BroadcastAddress, Settings.Instance.BroadcastPort)
            };

            state.udpclient.BeginSend(data, data.Length, state.endPoint, new AsyncCallback(SendCallback), state);
        }

        /// <summary>
        /// callBack to do EndSend
        /// </summary>
        /// <param name="ar"></param>
        static void SendCallback(IAsyncResult ar)
        {
            var state = (UdpState)ar.AsyncState;
            try
            {
                state.udpclient.EndSend(ar);
            }
            catch (SocketException ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(ex.Message);
#endif
            }
        }

        /// <summary>
        /// Get Broadcast-Address from System. NULL value if an error occours
        /// </summary>
        /// <returns></returns>
        public static IPAddress GetBroadcastAddress()
        {
            (IPAddress address, IPAddress mask) = GetIPAddressAndSubnetMask();
            return GetBroadcastAddress(address, mask);
        }

        /// <summary>
        /// Get IPAddress, SubnetMask and BroadCastAddress from System. NULL-Values if an error occours
        /// </summary>
        /// <returns></returns>
        public static (IPAddress address, IPAddress mask, IPAddress broadcast) GetIPAddresses()
        {
            (IPAddress address, IPAddress mask) = GetIPAddressAndSubnetMask();
            IPAddress broadcast = default;
            broadcast = GetBroadcastAddress(address, mask);
            return (address, mask, broadcast);
        }

        /// <summary>
        /// Get the Broadcast-Address from given IP and Subnetmask. NULL-Values if an error occours
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="subnetMask"></param>
        /// <returns></returns>
        static IPAddress GetBroadcastAddress(IPAddress ipAddress, IPAddress subnetMask)
        {
            if (ipAddress == null ||
                subnetMask == null)
                return null;

            IPAddress broadcast = default;
            try
            {
                uint address = BitConverter.ToUInt32(ipAddress.GetAddressBytes(), 0);
                uint mask = BitConverter.ToUInt32(subnetMask.GetAddressBytes(), 0);
                uint broadCastIpAddress = address | ~mask;
                broadcast = new IPAddress(BitConverter.GetBytes(broadCastIpAddress));
            }
            catch { }


            return broadcast;
        }

        /// <summary>
        /// Get IPAddress and subnetmask from System. NULL-Values if an error occours
        /// </summary>
        /// <returns></returns>
        static (IPAddress address, IPAddress mask) GetIPAddressAndSubnetMask()
        {
            IPAddress _address = default;
            IPAddress _mask = default;
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("10.0.0.1", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    _address = endPoint.Address;
                }
                foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
                {
                    foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            if (_address.Equals(unicastIPAddressInformation.Address))
                            {
                                _mask = unicastIPAddressInformation.IPv4Mask;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            return (_address, _mask);
        }

    }


}
