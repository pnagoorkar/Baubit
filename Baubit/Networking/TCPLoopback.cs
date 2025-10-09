using Baubit.Bootstrapping;
using System.Net;
using System.Net.Sockets;

namespace Baubit.Networking
{
    public class TCPLoopback : IBootstrap
    {
        private bool disposedValue;

        public Stream ClientSideStream { get; init; }
        public Stream ServerSideStream { get; init; }

        private TCPLoopback(Stream clientSideStream, 
                            Stream serverSideStream)
        {
            ClientSideStream = clientSideStream;
            ServerSideStream = serverSideStream;
        }

        public static async Task<TCPLoopback> CreateNewAsync(CancellationToken cancellationToken = default)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var client = new TcpClient();
            var connectTask = client.ConnectAsync(IPAddress.Loopback, port, cancellationToken);
            var clientProxy = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            await connectTask.ConfigureAwait(false);
            listener.Stop();

            return new TCPLoopback(client.GetStream(), clientProxy.GetStream());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ClientSideStream.Dispose();
                    ServerSideStream.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
