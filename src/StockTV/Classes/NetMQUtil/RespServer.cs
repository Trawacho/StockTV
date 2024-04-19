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
            _toSenderQueue = new NetMQQueue<NetMQMessage>();
            _fromSenderQueue = new NetMQQueue<NetMQMessage>();

            _server = new RouterSocket("@tcp://*:4747");
            _server.Options.Identity = Encoding.UTF8.GetBytes(Environment.MachineName + "-" + Guid.NewGuid().ToString());
            _server.ReceiveReady += Server_ReceiveReady;

            _toSenderQueue.ReceiveReady += ToSenderQueue_ReceiveReady;
            _fromSenderQueue.ReceiveReady += FromSenderQueue_ReceiveReady;


            _poller = new NetMQPoller() { _server, _toSenderQueue, _fromSenderQueue };
            _poller.RunAsync();
        }

        private static void FromSenderQueue_ReceiveReady(object sender, NetMQQueueEventArgs<NetMQMessage> e)
        {
            RaiseMqServerDataReceived(e.Queue.Dequeue());
        }

        private static void ToSenderQueue_ReceiveReady(object sender, NetMQQueueEventArgs<NetMQMessage> e)
        {
            _server.SendMultipartMessage(e.Queue.Dequeue());
        }

        internal static void Cancel()
        {
            _poller.Stop();
            _server.Dispose();
            _poller.Dispose();
        }

        static RouterSocket _server;
        static NetMQPoller _poller;
        static NetMQQueue<NetMQMessage> _toSenderQueue;
        static NetMQQueue<NetMQMessage> _fromSenderQueue;

        internal static void AddOutbound(NetMQMessage data)
        {
            _toSenderQueue.Enqueue(data);
        }

        private static void Server_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var message = e.Socket.ReceiveMultipartMessage();

            if (message.FrameCount >= 4)
            {
                System.Diagnostics.Debug.WriteLine($"Received...{message[0].ConvertToString()} Frames: {message.FrameCount} - {message[1].ConvertToString()} - {message[2].ConvertToString()} ");

                if (message[2].ConvertToString().Equals(MessageTopic.Hello.ToString()))
                {
                    NetMQMessage welcomeMessage = new NetMQMessage();
                    welcomeMessage.Append(message[0]);
                    welcomeMessage.AppendEmptyFrame();
                    welcomeMessage.Append(MessageTopic.Welcome.ToString());
                    AddOutbound(welcomeMessage);
                }


                else
                    _fromSenderQueue.Enqueue(message);
            }

        }



    }
}
