using System;
using System.IO.Pipelines;
using Leto.Handshake;
using Leto.RecordLayer;
using Leto.CipherSuites;
using Leto.BulkCiphers;
using Leto.ConnectionStates.SecretSchedules;
using System.Binary;
using Leto.Internal;

namespace Leto.ConnectionStates
{
    public sealed partial class Server12ConnectionState : ConnectionState, IConnectionState
    {
        private SecretSchedule12 _secretSchedule;
        private AeadBulkCipher _storedKey;
        private bool _requiresTicket;
        private bool _abbreviatedHandshake;

        public Server12ConnectionState(SecurePipeConnection secureConnection) : base(secureConnection) =>
            _secretSchedule = new SecretSchedule12(this);

        public TlsVersion RecordVersion => TlsVersion.Tls12;

        public void ChangeCipherSpec()
        {
            if (_state == HandshakeState.WaitingForChangeCipherSpec)
            {
                (_readKey, _storedKey) = _secretSchedule.GenerateKeys();
                _state = HandshakeState.WaitingForClientFinished;
                return;
            }
            if (_state == HandshakeState.WaitingForClientFinishedAbbreviated)
            {
                _readKey = _storedKey;
                return;
            }
            Alerts.AlertException.ThrowUnexpectedMessage(RecordType.ChangeCipherSpec);
        }

        public bool HandleClientHello(ref ClientHelloParser clientHello)
        {
            CipherSuite = _cryptoProvider.CipherSuites.GetCipherSuite(TlsVersion.Tls12, clientHello.CipherSuites);
            HandshakeHash = _cryptoProvider.HashProvider.GetHash(CipherSuite.HashType);
            HandshakeHash.HashData(clientHello.OriginalMessage);
            _certificate = SecureConnection.Listener.CertificateList.GetCertificate(null, CipherSuite.CertificateType.Value);
            _secretSchedule.SetClientRandom(clientHello.ClientRandom);
            _negotiatedAlpn = clientHello.NegotiatedAlpn;
            _hostName = clientHello.HostName;
            KeyExchange = _cryptoProvider.KeyExchangeProvider.GetKeyExchange(CipherSuite.KeyExchange, clientHello.SupportedGroups);
            if (_certificate == null)
            {
                (_certificate, _signatureScheme) = SecureConnection.Listener.CertificateList.GetCertificate(clientHello.SignatureAlgos);
            }
            else
            {
                _signatureScheme = _certificate.SelectAlgorithm(clientHello.SignatureAlgos);
            }
            if(clientHello.SessionTicket.Length > 0)
            {
                ProcessSessionTicket(clientHello.SessionTicket);
            }
            
            if (_abbreviatedHandshake)
            {
                SendFirstFlightAbbreviated(clientHello);
            }
            else
            {
                SendFirstFlightFull();
            }
            return true;
        }


        private void WriteChangeCipherSpec()
        {
            var writer = SecureConnection.HandshakeOutput.Writer.Alloc();
            writer.WriteBigEndian<byte>(1);
            writer.Commit();
            RecordHandler.WriteRecords(SecureConnection.HandshakeOutput.Reader, RecordType.ChangeCipherSpec);
        }

        private void WriteCertificates() => this.WriteHandshakeFrame((ref WritableBuffer buffer) =>
                CertificateWriter.WriteCertificates(buffer, _certificate), HandshakeType.certificate);

        private void ProcessSessionTicket(BigEndianAdvancingSpan buffer)
        {
            if (SecureConnection.Listener.SessionProvider == null)
            {
                return;
            }
            _requiresTicket = true;
            if (buffer.Length == 0 || !_secretSchedule.ReadSessionTicket(buffer))
            {
                return;
            }
            _abbreviatedHandshake = true;
        }

        protected override void Dispose(bool disposing)
        {
            lock (this)
            {
                base.Dispose(disposing);
                KeyExchange?.Dispose();
                KeyExchange = null;
                _secretSchedule?.Dispose();
                _secretSchedule = null;
            }
        }

        public bool ProcessHandshake()
        {
            var hasWritten = false;
            var hasReader = SecureConnection.HandshakeInput.Reader.TryRead(out ReadResult reader);
            if (!hasReader) return hasWritten;
            var buffer = reader.Buffer;
            try
            {
                while (HandshakeFraming.ReadHandshakeFrame(ref buffer, out ReadableBuffer messageBuffer, out HandshakeType messageType))
                {
                    Span<byte> span;
                    switch (messageType)
                    {
                        case HandshakeType.client_hello when _state == HandshakeState.WaitingForClientHello:
                            var helloParser = new ClientHelloParser(messageBuffer, SecureConnection);
                            var version = GetVersion(ref helloParser);
                            if(version != TlsVersion.Tls12)
                            {
                                Alerts.AlertException.ThrowAlert(Alerts.AlertLevel.Fatal, Alerts.AlertDescription.protocol_version, "Invalid protocol version");
                            }
                            return HandleClientHello(ref helloParser);
                        case HandshakeType.client_key_exchange when _state == HandshakeState.WaitingForClientKeyExchange:
                            span = messageBuffer.ToSpan();
                            HandshakeHash.HashData(span);
                            span = span.Slice(HandshakeFraming.HeaderSize);
                            KeyExchange.SetPeerKey(new BigEndianAdvancingSpan(span), _certificate, _signatureScheme);
                            _secretSchedule.GenerateMasterSecret();
                            _state = HandshakeState.WaitingForChangeCipherSpec;
                            break;
                        case HandshakeType.finished when _state == HandshakeState.WaitingForClientFinished:
                            span = messageBuffer.ToSpan();
                            if (_secretSchedule.GenerateAndCompareClientVerify(span))
                            {
                                _state = HandshakeState.HandshakeCompleted;
                            }
                            if (_requiresTicket)
                            {
                                _secretSchedule.WriteSessionTicket();
                                hasWritten = true;
                                RecordHandler.WriteRecords(SecureConnection.HandshakeOutput.Reader, RecordType.Handshake);
                            }
                            WriteChangeCipherSpec();
                            _writeKey = _storedKey;
                            _secretSchedule.GenerateAndWriteServerVerify();
                            RecordHandler.WriteRecords(SecureConnection.HandshakeOutput.Reader, RecordType.Handshake);
                            hasWritten = true;
                            _secretSchedule.DisposeStore();
                            break;
                        case HandshakeType.finished when _state == HandshakeState.WaitingForClientFinishedAbbreviated:
                            span = messageBuffer.ToSpan();
                            if (_secretSchedule.GenerateAndCompareClientVerify(span))
                            {
                            }
                            _state = HandshakeState.HandshakeCompleted;
                            _secretSchedule.DisposeStore();
                            break;
                        default:
                            Alerts.AlertException.ThrowUnexpectedMessage(messageType);
                            return false;
                    }
                }
            }
            finally
            {
                SecureConnection.HandshakeInput.Reader.Advance(buffer.Start, buffer.End);
            }
            return hasWritten;
        }
    }
}
