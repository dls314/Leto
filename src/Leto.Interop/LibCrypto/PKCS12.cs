using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Leto.Interop
{
    public partial class LibCrypto
    {
        public struct PKCS12
        {
            private IntPtr _ptr;

            public bool IsValid => _ptr != IntPtr.Zero;

            public void Free()
            {
                if (!IsValid) return;
                PKCS12_free(this);
                _ptr = IntPtr.Zero;
            }
        }
    }
}
