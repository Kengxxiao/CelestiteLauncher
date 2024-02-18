using System;
using System.Security.Authentication;
using Celestite.I18N;
using Celestite.Utils;
using ZeroLog;

namespace Celestite.Network
{

    public class NetworkOperationResult
    {
        public bool Success { get; }
        public bool Failed => !Success;
        private readonly Exception? _exception;
        public Exception Exception => _exception!;

        private static readonly Log Logger = LogManager.GetLogger("Network");

        protected NetworkOperationResult(bool success, Exception? error = null)
        {
            switch (success)
            {
                case true when error != null:
                    throw new InvalidOperationException();
                case false when error == null:
                    throw new InvalidOperationException();
            }

            Success = success;
            _exception = error;

            if (!success)
            {
                if (error!.Message.Contains("302"))
                {
                    NotificationHelper.Error(Localization.LooksLikeRegionBlockError);
                    return;
                }
                if (error is not OperationCanceledException)
                    NotificationHelper.Error(error!.Message);
            }
        }

        public static NetworkOperationResult Ok() => new(true, null);
        public static NetworkOperationResult<T> Ok<T>(T value) where T : class
        {
            return new NetworkOperationResult<T>(value, true, null);
        }

        public static NetworkOperationResult<T> Fail<T>(Exception error) where T : class
        {
            if (error.InnerException is AuthenticationException ex)
                error = ex;
            Logger.Error(string.Empty, error);
            return new NetworkOperationResult<T>(null, false, error);
        }
        public static NetworkOperationResult Fail(Exception error)
        {
            if (error.InnerException is AuthenticationException ex)
                error = ex;
            Logger.Error(string.Empty, error);
            return new NetworkOperationResult(false, error);
        }
    }

    public class NetworkOperationResult<T> : NetworkOperationResult, IDisposable where T : class
    {
        private readonly T? _value;
        public T Value => _value!;

        protected internal NetworkOperationResult(T? value, bool success, Exception? error = null) : base(success, error)
        {
            _value = value;
        }

        public void Dispose()
        {
            if (_value is IDisposable disposable)
                disposable.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
