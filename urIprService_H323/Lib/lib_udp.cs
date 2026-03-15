using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MyProject.lib {
    #region UDP Server
    public class Received {
        public string Message { get; set; }
        public IPEndPoint Sender { get; set; }
        public byte[] Data { get; set; }
        public int DataLen { get; set; }
        public Received(int dataLen) {
            DataLen = dataLen;
            Data = new byte[DataLen];
        }
    }

    public abstract class UdpBase {
        protected UdpClient Client;
        protected UdpBase() {
            Client = new UdpClient();
        }

        public async Task<Received> Receive() {
            var result = await Client.ReceiveAsync();
            var recv = new Received(result.Buffer.Length);
            recv.Sender = result.RemoteEndPoint;
            try {
                recv.Message = Encoding.UTF8.GetString(result.Buffer, 0, result.Buffer.Length);
            }
            catch (Exception ex) {
                recv.Message = "";
            }
            result.Buffer.CopyTo(recv.Data, 0);
            return recv;
        }

        public async Task<Received> Receive(CancellationToken cancelToken) {
            var result = await Client.ReceiveAsync(cancelToken);
            if (cancelToken.IsCancellationRequested)
                return null;
            var recv = new Received(result.Buffer.Length);
            recv.Sender = result.RemoteEndPoint;
            try {
                recv.Message = Encoding.UTF8.GetString(result.Buffer, 0, result.Buffer.Length);
            }
            catch (Exception ex) {
                recv.Message = "";
            }
            result.Buffer.CopyTo(recv.Data, 0);
            return recv;
        }
    }

    public class UdpListener : UdpBase {
        private IPEndPoint _listenOn;
        public UdpListener(IPEndPoint endpoint, int recvSize) {
            _listenOn = endpoint;
            Client = new UdpClient(_listenOn);
            Client.Client.ReceiveBufferSize = recvSize; // 8MB
        }

        public void Reply(string message, IPEndPoint endpoint) {
            var datagram = Encoding.ASCII.GetBytes(message);
            Client.Send(datagram, datagram.Length, endpoint);
        }

    }

    #endregion

    #region UDP CLient
    public class UdpUser : UdpBase {
        private UdpUser() { }

        public static UdpUser ConnectTo(string hostname, int port) {
            var connection = new UdpUser();
            connection.Client.Connect(hostname, port);
            return connection;
        }

        public void Send(string message) {
            var datagram = Encoding.ASCII.GetBytes(message);
            Client.Send(datagram, datagram.Length);
        }

    }
    #endregion
}
