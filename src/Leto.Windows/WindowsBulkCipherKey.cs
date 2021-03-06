﻿using Leto.BulkCiphers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Buffers;
using Microsoft.Win32.SafeHandles;
using static Leto.Windows.Interop.BCrypt;
using System.Binary;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Leto.Windows
{
    public unsafe class WindowsBulkCipherKey : IBulkCipherKey
    {
        private Buffer<byte> _iv;
        private int _tagSize;
        private SafeBCryptKeyHandle _keyHandle;
        private BufferHandle _ivHandle;
        private KeyMode _keyMode;
        private OwnedBuffer<byte> _scratchSpace;
        private OwnedBuffer<byte> _keyStore;
        private BufferHandle _scratchPin;
        private byte* _pointerAuthData;
        private byte* _pointerTag;
        private byte* _pointerMac;
        private byte* _pointerIv;
        private byte* _pointerModeInfo;
        private byte[] _tempBlock = new byte[16];
        private int _tempBlockBytes = 0;

        internal WindowsBulkCipherKey(SafeBCryptAlgorithmHandle type, OwnedBuffer<byte> keyStore, int keySize, int ivSize, int tagSize, string chainingMode, OwnedBuffer<byte> scratchSpace)
        {
            _scratchSpace = scratchSpace;
            _scratchPin = _scratchSpace.Buffer.Pin();
            _tagSize = tagSize;
            _keyStore = keyStore;
            _iv = _keyStore.Buffer.Slice(keySize, ivSize);
            _ivHandle = _iv.Pin();
            _keyHandle = BCryptImportKey(type, keyStore.Span.Slice(0, keySize));
            _pointerAuthData = (byte*)_scratchPin.PinnedPointer;
            _pointerTag = _pointerAuthData + sizeof(AdditionalInfo);
            _pointerMac = _pointerTag + _tagSize;
            _pointerIv = _pointerMac + _tagSize;
            _pointerModeInfo = _pointerIv + _tagSize;
        }

        public Buffer<byte> IV => _iv;
        public int TagSize => _tagSize;

        public void AddAdditionalInfo(ref AdditionalInfo addInfo)
        {
            ref var context = ref Unsafe.AsRef<BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO>(_pointerModeInfo);
            context.pbAuthData = _pointerAuthData;
            context.cbAuthData = sizeof(AdditionalInfo);
            Unsafe.Write(context.pbAuthData, addInfo);
        }

        public void Init(KeyMode mode)
        {
            _keyMode = mode;
            Unsafe.InitBlock(_scratchPin.PinnedPointer, 0, (uint)_scratchSpace.Length);
            var context = new BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO()
            {
                dwFlags = AuthenticatedCipherModeInfoFlags.ChainCalls,
                cbMacContext = _tagSize,
                pbMacContext = _pointerMac,
                cbNonce = _iv.Length,
                pbNonce = _ivHandle.PinnedPointer,
                cbAuthData = 0,
                pbAuthData = null,
                cbTag = _tagSize,
                cbSize = Marshal.SizeOf<BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO>(),
                pbTag = _pointerTag,
                dwInfoVersion = 1
            };
            Unsafe.Write(_pointerModeInfo, context);
        }

        public void GetTag(Span<byte> span)
        {
            if (_keyMode == KeyMode.Encryption)
            {
                var tagSpan = new Span<byte>(_pointerTag, _tagSize);
                tagSpan.CopyTo(span);
                return;
            }
            throw new NotSupportedException();
        }

        public int Update(Span<byte> inputOutput)
        {
            if (_keyMode == KeyMode.Encryption)
            {

                throw new NotSupportedException();
            }
            else
            {
                return BCryptDecrypt(_keyHandle, inputOutput, (BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO*) _pointerModeInfo, _pointerIv);
            }
        }

        public int Update(Span<byte> input, Span<byte> output)
        {
            if (_keyMode == KeyMode.Encryption)
            {
                return BCryptEncrypt(_keyHandle, input, output, (BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO*)_pointerModeInfo, _pointerIv);
            }
            throw new NotSupportedException();
        }

        public void SetTag(ReadOnlySpan<byte> tagSpan) => tagSpan.CopyTo(new Span<byte>(_pointerTag, _tagSize));

        public void Dispose()
        {
            _scratchPin.Free();
            _scratchSpace?.Dispose();
            _scratchSpace = null;
            _ivHandle.Free();
            _keyHandle?.Dispose();
            _keyStore.Dispose();
            _keyHandle = null;
            GC.SuppressFinalize(this);
        }

        public int Finish(Span<byte> inputAndOutput)
        {
            ref var context = ref Unsafe.AsRef<BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO>(_pointerModeInfo);
            context.dwFlags ^= AuthenticatedCipherModeInfoFlags.ChainCalls;
            return Update(inputAndOutput);
        }

        public int Finish(Span<byte> input, Span<byte> output)
        {
            ref var context = ref Unsafe.AsRef<BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO>(_pointerModeInfo);
            context.dwFlags ^= AuthenticatedCipherModeInfoFlags.ChainCalls;
            return Update(input, output);
        }

        ~WindowsBulkCipherKey()
        {
            Dispose();
        }
    }
}
