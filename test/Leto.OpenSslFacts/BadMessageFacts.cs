﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Leto.OpenSslFacts
{
    public class BadMessageFacts
    {
        [Fact]
        public async Task ClientHelloWithExtraBytes()
        {
            using (var listener = new OpenSsl11.OpenSslSecurePipeListener(Data.Certificates.RSACertificate))
            {
                await CommonFacts.BadHelloFacts.SendHelloWithExtraTrailingBytes(listener);
            }
        }

        [Fact]
        public async Task WrongInitialHandshakeMessage()
        {
            using (var listener = new OpenSsl11.OpenSslSecurePipeListener(Data.Certificates.RSACertificate))
            {
                await CommonFacts.BadHelloFacts.WrongInitialHandshakeMessage(listener);
            }
        }

        [Fact]
        public async Task InvalidVectorSizeForExtensions()
        {
            using (var listener = new OpenSsl11.OpenSslSecurePipeListener(Data.Certificates.RSACertificate))
            {
                await CommonFacts.BadHelloFacts.InvalidVectorSizeForExtensions(listener);
            }
        }

        [Fact]
        public async Task StartWithApplicationRecord()
        {
            using (var listener = new OpenSsl11.OpenSslSecurePipeListener(Data.Certificates.RSACertificate))
            {
                await CommonFacts.BadHelloFacts.StartWithApplicationRecord(listener);
            }
        }

        [Fact]
        public async Task UnknownAlpn()
        {
            using (var listener = new OpenSsl11.OpenSslSecurePipeListener(Data.Certificates.RSACertificate))
            {
                await CommonFacts.BadHelloFacts.UnknownALPN(listener);
            }
        }
    }
}
