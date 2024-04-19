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
            private PairSocket _shim;
            private NetMQPoller _poller;
            private PublisherSocket _publisher;
            private NetMQTimer _aliveTimer;
            public string aliveInfo;
            public void Initalise(object state)
            {
                _ = state;
            }

            public void Run(PairSocket shim)
            {
                using (_publisher = new PublisherSocket())
                {
                    _aliveTimer = new NetMQTimer(TimeSpan.FromSeconds(5));
                    _aliveTimer.Elapsed += (sender, eventArgs) =>
                    {
                        _publisher.SendMoreFrame(MessageTopic.Alive.ToString())
                                 .SendFrame(aliveInfo);
                    };

                    _publisher.Bind("tcp://*:4748");
                    _publisher.Options.SendHighWatermark = 100;

                    this._shim = shim;
                    this._shim.ReceiveReady += OnShimReady;
                    this._shim.SignalOK();
                    _poller = new NetMQPoller { shim, _publisher, _aliveTimer };
                    _poller.Run();
                }
            }

            private void OnShimReady(object sender, NetMQSocketEventArgs e)
            {
                string command = e.Socket.ReceiveFrameString();

                switch (command)
                {
                    case NetMQActor.EndShimMessage:
                        _poller.Stop();
                        break;
                    default:
                        string stringMessage = e.Socket.ReceiveFrameString();
                        _publisher.SendMoreFrame(command).SendFrame(stringMessage);
                        break;
                }
            }
        }

        private NetMQActor _actor;
        
        public void Start()
        {
            if (_actor != null) return;
            _actor = NetMQActor.Create(
                new ShimHandler() 
                { 
                    aliveInfo = JsonSerializer.Serialize(AliveInfo.Create()) 
                });
        }

        public void Stop()
        {
            if (_actor != null)
            {
                _actor.Dispose();
                _actor = null;
            }
        }

        public void SendDataMessage(MessageTopic topic, byte[] dataToSend)
        {
            if (_actor == null) return;

            var message = new NetMQMessage();
            message.Append(topic.ToString());
            message.Append(dataToSend);
            _actor.SendMultipartMessage(message);
        }

        public void SendDataMessage(MessageTopic topic, string dataToSend)
        {
            if (_actor == null) return;

            var message = new NetMQMessage();
            message.Append(topic.ToString());
            message.Append(dataToSend);
            _actor.SendMultipartMessage(message);
        }


    }
}
