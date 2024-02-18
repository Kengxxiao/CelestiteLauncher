using System;
using Avalonia.Controls;
using Celestite.Dialogs.Games;
using Celestite.Network.Models;
using FluentAvalonia.UI.Controls;

namespace Celestite.ViewModels.Navigation
{
    public class GameNavigationFactory : INavigationPageFactory
    {
        public static GameNavigationFactory Instance { get; private set; } = new();
        public override Control GetPage(Type srcType)
        {
            throw new NotImplementedException();
        }

        public override Control GetPageFromObject(uint target)
        {
            throw new NotImplementedException();
        }

        public override Control GetPageFromObject(object target)
        {
            if (target is not MyGame gameData)
                throw new NotImplementedException();
            return new GameSettingsDialog(gameData);
        }
    }
}
