using NetMQ;
using NetMQ.Sockets;
using System;
using System.Text;

namespace StockTV.Classes.NetMQUtil
{
    /// <summary>
    /// Response Server
    /// </summary>
    internal static class RespServer
    {
        #region DataReceived EventHandler

        internal static event MqServerDataReceivedEventHandler RespServerDataReceived;

        private static void RaiseMqServerDataReceived(NetMQMessage message)
        {
            var handler = RespServerDataReceived;
            handler?.Invoke(new MqServerDataReceivedEventArgs(message));
        }

        #endregion

        internal static void Start()
        {
            toSenderQueue = new NetMQQueue<NetMQMessage>();
            fromSenderQueue = new NetMQQueue<NetMQMessage>();

            server = new RouterSocket("@tcp://*:4747");
            server.Options.Identity = Encoding.UTF8.GetBytes(Environment.MachineName + "-" + Guid.NewGuid().ToString());
            server.ReceiveReady += Server_ReceiveReady;

            toSenderQueue.ReceiveReady += ToSenderQueue_ReceiveReady;
            fromSenderQueue.ReceiveReady += FromSenderQueue_ReceiveReady;


            poller = new NetMQPoller() { server, toSenderQueue, fromSenderQueue };
            poller.RunAsync();
        }

        private static void FromSenderQueue_ReceiveReady(object sender, NetMQQueueEventArgs<NetMQMessage> e)
        {
            RaiseMqServerDataReceived(e.Queue.Dequeue());
        }

        private static void ToSenderQueue_ReceiveReady(object sender, NetMQQueueEventArgs<NetMQMessage> e)
        {
            server.SendMultipartMessage(e.Queue.Dequeue());
        }

        internal static void Cancel()
        {
            poller.Stop();
            server.Dispose();
            poller.Dispose();
        }

        static RouterSocket server;
        static NetMQPoller poller;
        static NetMQQueue<NetMQMessage> toSenderQueue;
        static NetMQQueue<NetMQMessage> fromSenderQueue;

        internal static void AddOutbound(NetMQMessage data)
        {
            toSenderQueue.Enqueue(data);
        }

        private static void Server_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var message = e.Socket.ReceiveMultipartMessage();

            if (message.FrameCount == 3)
            {
                System.Diagnostics.Debug.WriteLine($"Received...{message[0].ConvertToString()} Frames: {message.FrameCount} - {message[1].ConvertToString()} - {message[2].ConvertToString()} ");

                if (message.Last.ConvertToString().Equals("Hello"))
                {
                    NetMQMessage welcomeMessage = new NetMQMessage();
                    welcomeMessage.Append(message[0]);
                    welcomeMessage.AppendEmptyFrame();
                    welcomeMessage.Append("Welcome");
                    AddOutbound(welcomeMessage);
                }
                else
                    fromSenderQueue.Enqueue(message);
            }

        }



    }
}
