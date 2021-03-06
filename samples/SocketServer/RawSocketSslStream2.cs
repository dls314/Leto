using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.IO.Pipelines.Networking.Sockets;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Leto.OpenSsl11;
using System.Threading.Tasks;
using System.IO;
using SslStream3;

namespace SocketServer
{
    public class RawSocketSslStream2 : RawHttpServerSampleBase
    {
        public RawSocketSslStream2(string filename)
            : base(filename)
        {
        }

        public SocketListener Listener { get; private set; }

        private PipeFactory _factory = new PipeFactory();
        private SslStream3Factory _streamFactory;

        protected override Task Start(IPEndPoint ipEndpoint)
        {
            _streamFactory = new SslStream3Factory("../TLSCerts/server.pfx", "test");
            Listener = new SocketListener();
            Listener.OnConnection(async connection => { await ProcessConnection(await CreateSslStream(connection)); });

            Listener.Start(ipEndpoint);
            return Task.CompletedTask;
        }

        private async Task<IPipeConnection> CreateSslStream(SocketConnection connection)
        {
            var sslStream = _streamFactory.GetStream(connection.GetStream());
            try
            {
                await sslStream.AuthenticateAsServerAsync();
            }
            catch
            {
                sslStream.Dispose();
                return null;
            }
            var returnConnection = new SslStreamConnection()
            {
                Input = _factory.CreateReader(sslStream),
                Output = _factory.CreateWriter(sslStream),
            };
            return returnConnection;
        }

        private class SslStreamConnection : IPipeConnection
        {
            public IPipeReader Input { get; set; }

            public IPipeWriter Output { get; set; }

            public void Dispose() { }
        }

        protected override Task Stop()
        {
            Listener.Dispose();
            return Task.CompletedTask;
        }
    }
}
