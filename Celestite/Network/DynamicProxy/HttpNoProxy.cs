using System;
using System.Net;

namespace Celestite.Network.DynamicProxy
{
    internal sealed class HttpNoProxy : IWebProxy
    {
        public static HttpNoProxy Instance = new();
        public ICredentials? Credentials { get; set; }
        public Uri? GetProxy(Uri destination) => null;
        public bool IsBypassed(Uri host) => true;
    }
}
