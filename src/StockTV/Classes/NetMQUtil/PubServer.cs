using NetMQ;
using NetMQ.Sockets;
using System;
using System.Text.Json;

namespace StockTV.Classes.NetMQUtil
{
    internal class AliveInfo
    {
        public string IpAddress { get; set; }
        public string HostName { get; set; }
        public string AppVersion { get; set; }

        internal static AliveInfo Create()
        {
            return new AliveInfo()
            {
                IpAddress = BroadcastService.GetIPAddresses().address.ToString(),
                HostName = Environment.MachineName,
                AppVersion = MdnsService.GetAppVersion()
            };
        }

        private AliveInfo()
        {

        }
    }
    
    /// <summary>
    /// Server to Publish Messages
    /// </summary>
    internal class PubServer
    {
        public class ShimHandler : IShimHandler
        {
            private PairSocket shim;
            private NetMQPoller poller;
            private PublisherSocket publisher;
            private NetMQTimer aliveTimer;
            public string aliveInfo;
            public void Initalise(object state)
            {
                _ = state;
            }

            public void Run(PairSocket shim)
            {
                using (publisher = new PublisherSocket())
                {
                    aliveTimer = new NetMQTimer(TimeSpan.FromSeconds(5));
                    aliveTimer.Elapsed += (sender, eventArgs) =>
                    {
                        publisher.SendMoreFrame(MessageTopic.Alive.ToString())
                                 .SendFrame(aliveInfo);
                    };

                    publisher.Bind("tcp://*:4748");
                    publisher.Options.SendHighWatermark = 100;

                    this.shim = shim;
                    this.shim.ReceiveReady += OnShimReady;
                    this.shim.SignalOK();
                    poller = new NetMQPoller { shim, publisher, aliveTimer };
                    poller.Run();
                }
            }

            private void OnShimReady(object sender, NetMQSocketEventArgs e)
            {
                string command = e.Socket.ReceiveFrameString();

                switch (command)
                {
                    case NetMQActor.EndShimMessage:
                        poller.Stop();
                        break;
                    default:
                        string stringMessage = e.Socket.ReceiveFrameString();
                        publisher.SendMoreFrame(command).SendFrame(stringMessage);
                        break;
                }
            }
        }

        private NetMQActor actor;
        
        public void Start()
        {
            if (actor != null) return;
            actor = NetMQActor.Create(
                new ShimHandler() 
                { 
                    aliveInfo = JsonSerializer.Serialize(AliveInfo.Create()) 
                });
        }

        public void Stop()
        {
            if (actor != null)
            {
                actor.Dispose();
                actor = null;
            }
        }

        public void SendDataMessage(MessageTopic topic, byte[] dataToSend)
        {
            if (actor == null) return;

            var message = new NetMQMessage();
            message.Append(topic.ToString());
            message.Append(dataToSend);
            actor.SendMultipartMessage(message);
        }

        public void SendDataMessage(MessageTopic topic, string dataToSend)
        {
            if (actor == null) return;

            var message = new NetMQMessage();
            message.Append(topic.ToString());
            message.Append(dataToSend);
            actor.SendMultipartMessage(message);
        }


    }
}
