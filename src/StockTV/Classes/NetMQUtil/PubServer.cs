using NetMQ;
using NetMQ.Sockets;
using System;

namespace StockTV.Classes.NetMQUtil
{
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
            public void Initalise(object state)
            {

            }
            public void Run(PairSocket shim)
            {
                using (publisher = new PublisherSocket())
                {
                    aliveTimer = new NetMQTimer(TimeSpan.FromSeconds(1));
                    aliveTimer.Elapsed += (sender, eventArgs) => { publisher.SendMoreFrame("Alive").SendFrameEmpty(); };

                    publisher.Bind("tcp://*:4748");
                    publisher.Options.SendHighWatermark = 100;

                    this.shim = shim;
                    shim.ReceiveReady += OnShimReady;
                    shim.SignalOK();
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
                    case "SendingResultInfo":
                        byte[] result = e.Socket.ReceiveFrameBytes();
                        publisher.SendMoreFrame("ResultInfo").SendFrame(result);
                        break;
                    default:
                        string stringMessage = e.Socket.ReceiveFrameString();
                        publisher.SendMoreFrame("unknown").SendFrame(stringMessage);
                        break;
                }
            }
        }

        private NetMQActor actor;

        public void Start()
        {
            if (actor != null) return;
            actor = NetMQActor.Create(new ShimHandler());
        }

        public void Stop()
        {
            if (actor != null)
            {
                actor.Dispose();
                actor = null;
            }
        }

        public void SendStringMessage(string stringToSend)
        {
            if (actor == null) return;

            var message = new NetMQMessage();
            message.Append("SendingResultInfo");
            message.Append(stringToSend);
            actor.SendMultipartMessage(message);
        }

        public void SendDataMessage(string command, byte[] dataToSend)
        {
            if (actor == null) return;

            var message = new NetMQMessage();
            message.Append(command); //SendingResultInfo as Command for the Shim-EventHandler
            message.Append(dataToSend);
            actor.SendMultipartMessage(message);
        }


    }
}
