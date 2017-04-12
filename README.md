# Leto
A interest learning exercise in producing a TLS 1.3 Library based off the experimental "Pipelines" from CoreFXLabs.

Currently supports TLS 1.2 and TLS 1.3 Draft 18, Draft 19 is a WIP.

Thanks to Mint/Tris who I have used to help understand from.

Currently uses OpenSsl 1.1, and Windows CNG for the crypto part and supports

Hello Retry, and standard Handshake. 

Now supports/downgrades to TLS 1.3 Correctly

AESxxx-GCM
ChaCha20/Poly
ECDSA certificates
RSA certificates

Key exchanges (All now supported)

Some support for Windows CNG is working, most extensions. Multiple server certificates, secure renegotiation

https://tls13.cetus.io was hosting a site running on it (but the free hosting ran out). It ran for 4 weeks serving the TLS 1.3 spec without any downtime.

1. x25519
2. x448
3. ffdhe8192
4. ffdhe6144
5. ffdhe4096
6. ffdhe3072
7. ffdhe2048
8. secp521r1
9. secp384r1
10. secp256r1

This library aims to prove out the use case for Pipelines having a native TLS library and for how TLS 1.3 can be implemented in that library.

Help and submissions are welcome!
