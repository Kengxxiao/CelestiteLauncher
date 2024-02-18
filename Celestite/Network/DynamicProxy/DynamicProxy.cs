using System;
using System.Net;
using System.Net.Http;
using Celestite.Network.DynamicProxy.Windows;

namespace Celestite.Network.DynamicProxy
{
    public class DynamicProxyImpl
    {
        public static IWebProxy GetProxy()
        {
            if (OperatingSystem.IsWindows())
                return DynamicWindowsHttpProxy.ProxyInstance;
            return HttpClient.DefaultProxy;
        }
    }
}
