using System;
using System.Threading.Tasks;
using TCP_chat.Server;

namespace TCP_chat.Server
{
    public class NetworkServer
    {
        public static event EventHandler<string> eventMessage;

        ServerObject server;

        public NetworkServer()
        {
            server = new ServerObject();
            ServerObject.eventMessage += ServerObject_eventMessage;
        }

        public async Task StartServer()
        {
            await server.ListenAsync();
        }

        private void ServerObject_eventMessage(object sender, string e)
        {
            eventMessage?.Invoke(null, e);
        }
    }
}
