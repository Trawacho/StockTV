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
            outbound = new NetMQQueue<NetMQMessage>();

            server = new ResponseSocket("@tcp://*:4747");
            server.Options.Identity = Encoding.UTF8.GetBytes(Environment.MachineName);
            server.ReceiveReady += Server_ReceiveReady;

            poller = new NetMQPoller() { server };
            poller.RunAsync();
        }

        internal static void Cancel()
        {
            poller.Stop();
            server.Dispose();
            poller.Dispose();
        }

        static ResponseSocket server;
        static NetMQPoller poller;
        static NetMQQueue<NetMQMessage> outbound;
        private static int _errCnt = 0;

        internal static void AddOutbound(NetMQMessage data)
        {
            outbound.Enqueue(data);
        }

        private static void Server_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            e.Socket.ReceiveReady -= Server_ReceiveReady;
            try
            {
                var message = e.Socket.ReceiveMultipartMessage();

                RaiseMqServerDataReceived(message);

                if (outbound.TryDequeue(out NetMQMessage result, TimeSpan.FromMilliseconds(100)))
                {
                    e.Socket.SendMultipartMessage(result);
                }
                else
                {
                    e.Socket.SignalOK();
                }

                _errCnt = 0;
                e.Socket.ReceiveReady += Server_ReceiveReady;
            }
            catch
            {
                _errCnt++;
                Cancel();
                if (_errCnt < 5)
                    Start();
            }

        }



    }
}
