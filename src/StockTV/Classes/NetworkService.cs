using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

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

    class NetworkSimulationService
    {
        static Random rand = new Random();
        public static void Simulate()
        {
            const int numberOfMatches = 9;
            const int numberOfCourts = 4;
            const int numberOfTurns = 6;
            Settings.Instance.IsBroadcasting = true;

            var matches = new List<Match>();

            while (true)
            {
                matches.Clear();
                for (int b = 0; b < numberOfCourts; b++)
                {
                    matches.Add(new Match());
                }

                Parallel.ForEach(matches, (m) =>
                {
                    for (int i = 0; i < numberOfMatches; i++) // 9 Spiele
                    {
                        for (int k = 0; k < numberOfTurns; k++) // 6 Kehren pro Spiel
                        {
                            Turn t = new Turn();
                            if (rand.Next(2) == 1)
                            {
                                t.PointsLeft = Convert.ToByte(rand.Next(2, 9));
                            }
                            else
                            {
                                t.PointsRight = Convert.ToByte(rand.Next(2, 9));
                            }

                            m.AddTurn(t);
                            NetworkService.SendData(m.Serialize(true,
                                                            Convert.ToByte(matches.IndexOf(m) + 1)));
                            Thread.Sleep(2000);
                        }
                        m.Reset();
                    }
                });


                Thread.Sleep(10000);
                byte[] cancelValue = new byte[1] { byte.MaxValue };
                NetworkService.SendData(cancelValue);
            }

        }

    }
}
