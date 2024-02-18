using System;
using System.Net;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Celestite.Utils;

namespace Celestite.Network.DynamicProxy.Windows
{
    [SupportedOSPlatform("windows")]
    public class DynamicWindowsHttpProxy : IWebProxy, IDisposable
    {
        private static DynamicWindowsHttpProxy? _proxyInstance;
        public static DynamicWindowsHttpProxy ProxyInstance => LazyInitializer.EnsureInitialized(ref _proxyInstance, () =>
        {
            var dynamicProxy = new DynamicWindowsHttpProxy();
            dynamicProxy.Start();
            return dynamicProxy;
        });
        public DynamicWindowsHttpProxy()
        {
            if (HttpWindowsProxy.TryCreate(out var proxy))
            {
                _innerProxy = proxy;
            }
            else
            {
                _innerProxy = new HttpNoProxy();
            }
        }

        /// <summary>
        /// 开始根据注册表变更动态修改代理，需要开启一个线程监听注册表
        /// </summary>
        public void Start()
        {
            _registryMonitor = new RegistryMonitor(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Connections");
            _registryMonitor.RegChanged += RegistryMonitor_RegChanged;
            _registryMonitor.Start();
        }

        private void RegistryMonitor_RegChanged(object? sender, EventArgs e)
        {
            UpdateProxy();
        }

        private void UpdateProxy()
        {
            if (HttpWindowsProxy.TryCreate(out var proxy))
            {
                InnerProxy = proxy;
            }
            else
            {
                InnerProxy = new HttpNoProxy();
            }
        }

        private RegistryMonitor? _registryMonitor;

        private IWebProxy InnerProxy
        {
            set
            {
                if (ReferenceEquals(_innerProxy, value))
                {
                    return;
                }

                if (_innerProxy is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _innerProxy = value;
            }
            get => _innerProxy;
        }

        /// <inheritdoc />
        public ICredentials? Credentials
        {
            get => InnerProxy.Credentials;
            set => InnerProxy.Credentials = value;
        }

        /// <summary>
        /// 超过多少次超时之后，就再也不拿代理了
        /// </summary>
        public int MaxTimeoutCount { set; get; } = 5;

        /// <summary>
        /// 获取代理的超时时间，默认 15 秒
        /// </summary>
        public TimeSpan TimeoutForGetProxy { set; get; } = TimeSpan.FromSeconds(15);

        /// <inheritdoc />
        public Uri? GetProxy(Uri destination)
        {
            if (InnerProxy is HttpNoProxy)
            {
                // 如果是啥都没的代理，那返回即可
                return InnerProxy.GetProxy(destination);
            }
            else
            {
                if (_timeoutCount > MaxTimeoutCount)
                {
                    // 超过 5 次都超时，那就别拿代理了
                    return null;
                }

                var task = Task.Run(() => InnerProxy.GetProxy(destination));
                var delayTask = Task.Delay(TimeoutForGetProxy);

                Task.WaitAny(task, delayTask);

                if (task.IsCompleted)
                {
                    return task.Result;
                }
                else
                {
                    // 超时
                    _timeoutCount++;
                    return null;
                }
            }
        }
        private int _timeoutCount;

        /// <inheritdoc />
        public bool IsBypassed(Uri host)
        {
            return ConfigUtils.GetBypassSystemProxy() || InnerProxy.IsBypassed(host);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_innerProxy is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _registryMonitor?.Dispose();
            GC.SuppressFinalize(this);
        }

        private IWebProxy _innerProxy;
    }
}
