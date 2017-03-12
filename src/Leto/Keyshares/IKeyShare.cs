﻿using Leto.Hashes;
using System;

namespace Leto.Keyshares
{
    public interface IKeyshareInstance : IDisposable
    {
        bool HasPeerKey { get; }
        void SetPeerKey(ReadOnlySpan<byte> peerKey);
        int KeyExchangeSize { get; }
        void WritePublicKey(Span<byte> keyBuffer);
        NamedGroup NamedGroup { get; }
        void DeriveSecret(IHashProvider hashProvider, HashType hashType, ReadOnlySpan<byte> salt, Span<byte> output);
        void DeriveMasterSecret(IHashProvider hashProvider, HashType hashType, ReadOnlySpan<byte> seed, Span<byte> output);
    }
}
