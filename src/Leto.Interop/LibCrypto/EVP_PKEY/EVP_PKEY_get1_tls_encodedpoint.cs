using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Leto.Interop
{
    public static partial class LibCrypto
    {
        [DllImport(Libraries.LibCrypto, CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe IntPtr EVP_PKEY_get1_tls_encodedpoint(EVP_PKEY pkey, out IntPtr ptr);

        public static unsafe int EVP_PKEY_get1_tls_encodedpoint(EVP_PKEY pkey, Span<byte> output)
        {
            var buffSize = (int)ThrowOnError(EVP_PKEY_get1_tls_encodedpoint(pkey, out IntPtr ptr));
            try
            {
                if(buffSize > output.Length)
                {
                    throw new InvalidOperationException();
                }
                var span = new Span<byte>((byte*)ptr, buffSize);
                span.CopyTo(output);
            }
            finally
            {   
                CRYPTO_clear_free(ptr, (UIntPtr)buffSize, $"OpenSslFunction.cs", 97);
            }
            return buffSize;
        }
    }
}
