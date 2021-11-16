using NetMQ;
using System;

namespace StockTV.Classes.NetMQUtil
{
    internal delegate void MqServerDataReceivedEventHandler(MqServerDataReceivedEventArgs args);

    internal enum MessageTopic
    {
        Hello = 01,
        Welcome = 02,
        SetResult = 10,
        GetResult = 11,
        SetSettings = 20,
        GetSettings = 21,
        ResetResult = 30,
        SetTeamNames = 40,
        SetImage = 50,
        GoToImage = 51,
        ClearImage = 52
    }

    internal class MqServerDataReceivedEventArgs : EventArgs
    {
        private NetMQMessage Message { get; set; }

        public MessageTopic Topic => (MessageTopic)Enum.Parse(typeof(MessageTopic), Message[2].ConvertToString());
        
        public byte[] Value => Message[3].ToByteArray(true);

        public byte[] GetAdditionals()
        {
            if(Message.FrameCount > 4)
            {
                return Message[4].ToByteArray(true);
            }
            else
            {
                return null;
            }
        }

        public NetMQFrame SenderID => Message[0];

        public MqServerDataReceivedEventArgs(NetMQMessage message)
        {
            this.Message = message;
        }
      
    }
}
