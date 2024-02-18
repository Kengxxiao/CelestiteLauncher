using System;
using System.Net;

namespace Celestite.Network.DynamicProxy
{
    internal interface IMultiWebProxy : IWebProxy
    {
        /// <summary>
        /// Gets the proxy URIs.
        /// </summary>
        public MultiProxy GetMultiProxy(Uri uri);
    }
}
