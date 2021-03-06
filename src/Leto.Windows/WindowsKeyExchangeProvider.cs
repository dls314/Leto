﻿using Leto.KeyExchanges;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using static Leto.Windows.Interop.BCrypt;
using Leto.Internal;

namespace Leto.Windows
{
    public class WindowsKeyExchangeProvider : IKeyExchangeProvider
    {
        private SafeBCryptAlgorithmHandle _secp256r1;
        private SafeBCryptAlgorithmHandle _secp384r1;
        private SafeBCryptAlgorithmHandle _secp521r1;

        public WindowsKeyExchangeProvider()
        {
            _secp256r1 = BCryptOpenECCurveAlgorithmProvider("secP256r1");
            _secp384r1 = BCryptOpenECCurveAlgorithmProvider("secP384r1");
            _secp521r1 = BCryptOpenECCurveAlgorithmProvider("secP521r1");
        }

        public IKeyExchange GetKeyExchange(NamedGroup namedGroup)
        {
            switch (namedGroup)
            {
                case NamedGroup.secp256r1:
                    return new WindowsECCurveKeyExchange(_secp256r1, namedGroup);
                case NamedGroup.secp384r1:
                    return new WindowsECCurveKeyExchange(_secp384r1, namedGroup);
                case NamedGroup.secp521r1:
                    return new WindowsECCurveKeyExchange(_secp521r1, namedGroup);
                case NamedGroup.x25519:
                case NamedGroup.x448:
                case NamedGroup.ffdhe2048:
                case NamedGroup.ffdhe3072:
                case NamedGroup.ffdhe4096:
                case NamedGroup.ffdhe6144:
                case NamedGroup.ffdhe8192:
                default:
                    return null;
            }
        }

        public IKeyExchange GetKeyExchange(BigEndianAdvancingSpan keyshare)
        {
            while (keyshare.Length > 0)
            {
                var key = keyshare.ReadVector<ushort>();
                var namedGroup = key.Read<NamedGroup>();
                var instance = GetKeyExchange(namedGroup);
                if (instance != null)
                {
                    instance.SetPeerKey(key);
                    return instance;
                }
            }
            return null;
        }
        
        public IKeyExchange GetKeyExchange(KeyExchangeType keyExchange, BigEndianAdvancingSpan supportedGroups)
        {
            switch (keyExchange)
            {
                case KeyExchangeType.Rsa:
                    return new RsaKeyExchange();
                case KeyExchangeType.Ecdhe:
                    return EcdheKeyExchange(supportedGroups);
                default:
                    Alerts.AlertException.ThrowAlert(Alerts.AlertLevel.Fatal, Alerts.AlertDescription.handshake_failure, "Unable to match key exchange");
                    return null;
            }
        }

        private IKeyExchange EcdheKeyExchange(BigEndianAdvancingSpan supportedGroups)
        {
            supportedGroups = supportedGroups.ReadVector<ushort>();
            while (supportedGroups.Length > 0)
            {
                var namedGroup = supportedGroups.Read<NamedGroup>();
                var keyExchange = GetKeyExchange(namedGroup);
                if (keyExchange != null) return keyExchange;
            }
            Alerts.AlertException.ThrowAlert(Alerts.AlertLevel.Fatal, Alerts.AlertDescription.handshake_failure, "Unable to match key exchange");
            return null;
        }

        public IKeyExchange GetKeyExchangeFromSupportedGroups(BigEndianAdvancingSpan supportedGroups)
        {
            supportedGroups = supportedGroups.ReadVector<ushort>();
            while (supportedGroups.Length > 0)
            {
                var namedGroup = supportedGroups.Read<NamedGroup>();
                var keyExchange = GetKeyExchange(namedGroup);
                if (keyExchange != null) return keyExchange;
            }
            return null;
        }

        public void Dispose()
        {
            _secp256r1?.Dispose();
            _secp256r1 = null;
            _secp384r1?.Dispose();
            _secp384r1 = null;
            _secp521r1?.Dispose();
            _secp521r1 = null;
            GC.SuppressFinalize(this);
        }
        
        ~WindowsKeyExchangeProvider() => Dispose();
    }
}
