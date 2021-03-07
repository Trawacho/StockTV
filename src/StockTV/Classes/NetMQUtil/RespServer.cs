using NetMQ;
using NetMQ.Sockets;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace StockTV.Classes.NetMQUtil
{
    /// <summary>
    /// Response Server
    /// </summary>
    internal static class RespServer
    {
        #region DataReceived EventHandler

        internal static event MqServerDataReceivedEventHandler RespServerDataReceived;

        private static void RaiseMqServerDataReceived(NetMQSocket socket, byte[] messageData)
        {
            var handler = RespServerDataReceived;
            handler?.Invoke(socket, new MqServerDataReceivedEventArgs(messageData));
        }

        #endregion


        private static CancellationTokenSource _cts;

        private static Task _task;


        public static bool IsFaulted => _task?.IsFaulted ?? true;

        internal static void Start()
        {
            _cts = new CancellationTokenSource();
            var _token = _cts.Token;

            _task = Task.Factory.StartNew(() =>
            {
                try
                {
                    NetMqWorkerRoutine(_token);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception on WorkerRoutine: {0}", ex.Message);
                }
            }
            , _token
            , TaskCreationOptions.LongRunning
            , TaskScheduler.Default);
        }

        internal static void Cancel()
        {
            _cts?.Cancel();
        }



        private static void NetMqWorkerRoutine(CancellationToken _token)
        {
            using (var server = new ResponseSocket("@tcp://*:4747"))
            {
                server.Options.Identity = Encoding.UTF8.GetBytes($"{Environment.MachineName}");
                server.ReceiveReady += Server_ReceiveReady;

                while (!_token.IsCancellationRequested)
                {
                    server.Poll(TimeSpan.FromMilliseconds(100.0));
                }
            }

        }

        private static void Server_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var socket = e.Socket;

            var message = new Msg();
            message.InitEmpty();
            socket.Receive(ref message);

            if (Encoding.UTF8.GetString(message.Data) == "ALIVE")
            {
                socket.TrySignalOK();
                return;
            }

            RaiseMqServerDataReceived(socket, message.Data);
        }

    }
}
