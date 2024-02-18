using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Threading;
using Celestite.Pages;
using Celestite.Pages.Default;
using Celestite.Utils;
using FluentAvalonia.UI.Controls;

namespace Celestite.ViewModels
{
    public class NavigationFactory : INavigationPageFactory
    {
        private static NavigationFactory? _outerInstance;
        private static NavigationFactory? _instance;

        public static NavigationFactory OuterInstance => LazyInitializer.EnsureInitialized(ref _outerInstance, () => new NavigationFactory());
        public static NavigationFactory Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new NavigationFactory());

        public const uint Login = 1808393236;
        public const uint MainNavigation = 2787752072;
        public const uint DmmProfileReg = 45416349;

        public const uint HomePage = 2455771270;
        public const uint SettingsPageId = 1770793989;
        public const uint NotificationPage = 1870810798;
        public const uint DownloadManagerPageId = 3168097895;
        public const uint MissionPage = 3836352586;
        public const uint AccountPage = 1060427373;

        public const uint GamesPageId = 3656201620;

        private readonly Dictionary<uint, ContentControl> _cachedControls = [];

        public override Control GetPage(Type srcType)
        {
            return null!;
        }

        public override Control GetPageFromObject(uint target)
        {
            if (_cachedControls.TryGetValue(target, out var control))
            {
                GCUtils.CollectGeneration2();
                return control;
            }
            if (target is MainNavigation or Login) ClearCache();
            control = target switch
            {
                // 全页面
                Login => new LoginPage(),
                MainNavigation => new MainNavigationView(),
                DmmProfileReg => new DmmProfileRegistrationPage(),
                // 附属
                HomePage => new HomePage(),
                SettingsPageId => new SettingsPage(),
                NotificationPage => new NotificationPage(),
                GamesPageId => new GamesPage(),
                DownloadManagerPageId => new DownloadManagerPage(),
                MissionPage => new MissionPage(),
                AccountPage => new AccountPage(),
                // 未知页
                _ => throw new InvalidOperationException()
            };
            _cachedControls[target] = control;
            GCUtils.CollectGeneration2();
            return control;
        }

        public void ClearCache()
        {
            _cachedControls.Clear();
        }

        public override Control GetPageFromObject(object target)
        {
            throw new NotImplementedException();
        }

        public void Navigate(uint target)
        {
            if (NavFrame == null)
                throw new InvalidOperationException();
            Dispatcher.UIThread.Invoke(() =>
            {
                NavFrame.NavigateFromObject(target);
            });
        }
    }
}
