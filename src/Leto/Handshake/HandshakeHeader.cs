﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Leto.Handshake
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HandshakeHeader
    {
        public HandshakeType MessageType;
        private byte _firstByte;
        private ushort _secondBytes;

        public uint Length
        {
            get
            {
                uint returnValue =(uint)( ((_secondBytes & 0xFF00) >> 8)
                    | ((_secondBytes & 0x00FF) << 8)
                    | (_firstByte << 16));
                return returnValue;
            }
            set
            {
                _firstByte = (byte)(value >> 16);
                _secondBytes = (ushort)(((value & 0x0000FF00) >> 8) | ((value & 0x000000FF) << 8));
            }
        }
    }
}
