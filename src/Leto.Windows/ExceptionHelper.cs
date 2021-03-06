﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Leto.Windows
{
    internal static class ExceptionHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [DebuggerStepThrough]
        internal static void ThrowException(Exception ex) => throw ex;
    }
}
