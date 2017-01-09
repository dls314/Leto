﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Leto.Tls13.KeyExchange
{
    public interface IKeyshareProvider
    {
        IKeyshareInstance GetKeyShareInstance(NamedGroup namedGroup);
        void Dispose();
    }
}
