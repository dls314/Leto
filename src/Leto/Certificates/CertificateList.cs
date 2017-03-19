﻿using Leto.Keyshares;
using System;
using System.Collections.Generic;
using System.Text;

namespace Leto.Certificates
{
    public class CertificateList
    {
        private List<ICertificate> _certificates = new List<ICertificate>();

        public void AddCertificate(ICertificate certificate)
        {
            _certificates.Add(certificate);
        }

        public ICertificate GetCertificate(string host, CertificateType certificateType)
        {
            for(var i = 0; i < _certificates.Count;i++)
            {
                if(host != null)
                {
                    //Need to implement certificates for a specific host
                    throw new NotImplementedException();
                }
                else
                {
                    if(_certificates[i].CertificateType == certificateType)
                    {
                        return _certificates[i];
                    }
                }
            }
            Alerts.AlertException.ThrowAlert(Alerts.AlertLevel.Fatal, Alerts.AlertDescription.certificate_unobtainable, $"Could not find a certficate for {host} and type {certificateType}");
            return null;
        }

        public ICertificate GetCertificate(string host, SignatureScheme type)
        {
            for (int i = 0; i < _certificates.Count; i++)
            {
                //if (_certificates[i].HostName != host && host != null)
                //{
                //    continue;
                //}
                var cert = _certificates[i];
                //if (!cert.SupportsSignatureScheme(type))
                //{
                //    continue;
                //}
                return _certificates[i];
            }
            return null;
        }
    }
}
