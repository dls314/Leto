using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Leto.Interop
{
    public static partial class LibCrypto
    {
        [DllImport(Libraries.LibCrypto, CallingConvention = CallingConvention.Cdecl)]
        private static extern void BIO_set_data(BIO a, IntPtr ptr);

        public static void BIO_set_data(BIO bio, GCHandle handle) => BIO_set_data(bio, GCHandle.ToIntPtr(handle));
        public static void BIO_reset_data(BIO bio) => BIO_set_data(bio, IntPtr.Zero);
    }
}
