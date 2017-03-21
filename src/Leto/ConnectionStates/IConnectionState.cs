﻿using Leto.BulkCiphers;
using Leto.Certificates;
using Leto.CipherSuites;
using Leto.Handshake;
using Leto.Hashes;
using Leto.Keyshares;
using System;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Leto.ConnectionStates
{
    public interface IConnectionState : IDisposable
    {
        CipherSuite CipherSuite { get; }
        void ChangeCipherSpec();
        Task HandleClientHello(ClientHelloParser clientHelloParser);
        IHash HandshakeHash { get; }
        ushort RecordVersion { get; }
        AeadBulkCipher ReadKey { get; }
        AeadBulkCipher WriteKey { get; }
        bool HandshakeDone { get; }
    }
}