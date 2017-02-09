﻿using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;
using Leto.Tls13.BulkCipher;
using Leto.Tls13.Certificates;
using Leto.Tls13.Handshake;
using Leto.Tls13.Hash;
using Leto.Tls13.Internal;
using Leto.Tls13.KeyExchange;
using Microsoft.Extensions.Logging;

namespace Leto.Tls13.State
{
    public class ServerStateTls12 : AbstractServerState, IConnectionStateTls12
    {
        private IBulkCipherInstance _readKey;
        private IBulkCipherInstance _writeKey;
        private KeySchedule12 _schedule;
        
        public ServerStateTls12(SecurePipelineListener listener, ILogger logger)
            : base(listener)
        {
            _logger = logger;
            _schedule = new KeySchedule12(this, listener.KeyScheduleProvider.BufferPool);
        }

        public override IBulkCipherInstance ReadKey => _readKey;
        public override TlsVersion Version => TlsVersion.Tls12;
        public override IBulkCipherInstance WriteKey => _writeKey;
        public override ushort TlsRecordVersion => 0x0303;
        public Span<byte> ClientRandom => _schedule.ClientRandom;
        public Span<byte> ServerRandom => _schedule.ServerRandom;


        public override void HandleAlertMessage(ReadableBuffer messageBuffer)
        {
            var level = messageBuffer.ReadBigEndian<Alerts.AlertLevel>();
            messageBuffer = messageBuffer.Slice(sizeof(Alerts.AlertLevel));
            var description = messageBuffer.ReadBigEndian<Alerts.AlertDescription>();
            Alerts.AlertException.ThrowAlert(level, description, "Alert from the client");
        }

        public override async Task HandleHandshakeMessage(HandshakeType handshakeMessageType, ReadableBuffer buffer, IPipelineWriter pipe)
        {
            WritableBuffer writer;
            switch (State)
            {
                case StateType.None:
                    if (handshakeMessageType != HandshakeType.client_hello)
                    {
                        Alerts.AlertException.ThrowAlert(Alerts.AlertLevel.Fatal, Alerts.AlertDescription.unexpected_message, "Tls 12 didnt get a client hello");
                    }
                    Hello.ReadClientHelloTls12(buffer, this);
                    if (CipherSuite == null)
                    {
                        //Couldn't agree a set of ciphers
                        Alerts.AlertException.ThrowAlert(Alerts.AlertLevel.Fatal, Alerts.AlertDescription.handshake_failure, "Could not agree on a cipher suite during reading client hello");
                    }
                    this.StartHandshakeHash(buffer);
                    //Write server hello
                    ChangeState(StateType.SendServerHello);
                    writer = pipe.Alloc();
                    this.WriteHandshake(ref writer, HandshakeType.server_hello, Hello.SendServerHello12);
                    this.WriteHandshake(ref writer, HandshakeType.certificate, ServerHandshakeTls12.SendCertificates);
                    if (CipherSuite.ExchangeType == KeyExchangeType.Ecdhe || CipherSuite.ExchangeType == KeyExchangeType.Dhe)
                    {
                        if (KeyShare == null)
                        {
                            KeyShare = CryptoProvider.GetDefaultKeyShare(CipherSuite.ExchangeType);
                        }
                        this.WriteHandshake(ref writer, HandshakeType.server_key_exchange, ServerHandshakeTls12.SendKeyExchange);
                    }
                    this.WriteHandshake(ref writer, HandshakeType.server_hello_done, (w, state) => w);
                    await writer.FlushAsync();
                    ChangeState(StateType.WaitClientKeyExchange);
                    break;
                case StateType.WaitClientKeyExchange:
                    if (handshakeMessageType != HandshakeType.client_key_exchange)
                    {
                        Alerts.AlertException.ThrowAlert(Alerts.AlertLevel.Fatal, Alerts.AlertDescription.unexpected_message, $"Received a {handshakeMessageType} when we expected a {HandshakeType.client_key_exchange}");
                    }
                    HandshakeHash.HashData(buffer);
                    KeyShare.SetPeerKey(buffer.Slice(5));
                    _schedule.GenerateMasterSecret();
                    _schedule.CalculateClientFinished();
                    //We can send the server finished because we have the expected client finished
                    _schedule.GenerateKeyMaterial();
                    ChangeState(StateType.ChangeCipherSpec);
                    break;
                case StateType.WaitClientFinished:
                    if(handshakeMessageType != HandshakeType.finished)
                    {
                        Alerts.AlertException.ThrowAlert(Alerts.AlertLevel.Fatal, Alerts.AlertDescription.unexpected_message, $"unexpected message");
                    }
                    _schedule.CompareClientFinishedGenerateServerFinished(buffer);
                    _writeKey = _schedule.GetServerKey();
                    writer = pipe.Alloc();
                    this.WriteHandshake(ref writer, HandshakeType.finished, _schedule.WriteServerFinished);
                    DataForCurrentScheduleSent.Reset();
                    await writer.FlushAsync();
                    await DataForCurrentScheduleSent;
                    ChangeState(StateType.HandshakeComplete);
                    break;
                default:
                    Alerts.AlertException.ThrowAlert(Alerts.AlertLevel.Fatal, Alerts.AlertDescription.unexpected_message, $"Not in any known state {State} that we expected a handshake messge from {handshakeMessageType}");
                    break;
            }
        }
        
        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override void HandleChangeCipherSpec(ReadableBuffer readable)
        {
            if (State != StateType.ChangeCipherSpec)
            {
                Alerts.AlertException.ThrowAlert(Alerts.AlertLevel.Fatal, Alerts.AlertDescription.unexpected_message, "");
            }
            _readKey = _schedule.GetClientKey();
            ChangeState(StateType.WaitClientFinished);
        }

        public override void SetClientRandom(ReadableBuffer readableBuffer)
        {
            readableBuffer.CopyTo(_schedule.ClientRandom);
        }

        public override void SetServerRandom(Memory<byte> memory)
        {
            memory.CopyTo(_schedule.ServerRandom);
        }
    }
}
