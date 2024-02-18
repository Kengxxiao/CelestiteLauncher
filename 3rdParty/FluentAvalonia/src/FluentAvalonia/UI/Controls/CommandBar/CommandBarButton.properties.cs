﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a templated button control to be displayed in an <see cref="CommandBar"/>
/// </summary>
[PseudoClasses(SharedPseudoclasses.s_pcIcon, SharedPseudoclasses.s_pcLabel, SharedPseudoclasses.s_pcCompact)]
[PseudoClasses(SharedPseudoclasses.s_pcFlyout, s_pcSubmenuOpen, SharedPseudoclasses.s_pcOverflow)]
[PseudoClasses(SharedPseudoclasses.s_pcHotkey)]
public partial class CommandBarButton : Button, ICommandBarElement
{
    /// <summary>
    /// Defines the <see cref="IsInOverflow"/> property
    /// </summary>
    public static readonly DirectProperty<CommandBarButton, bool> IsInOverflowProperty =
        AvaloniaProperty.RegisterDirect<CommandBarButton, bool>(nameof(IsInOverflow),
            x => x.IsInOverflow);

    /// <summary>
    /// Defines the <see cref="IconSource"/> property
    /// </summary>
    public static readonly StyledProperty<IconSource> IconSourceProperty =
        SettingsExpander.IconSourceProperty.AddOwner<CommandBarButton>();

    /// <summary>
    /// Defines the <see cref="Label"/> property
    /// </summary>
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<CommandBarButton, string>(nameof(Label));

    /// <summary>
    /// Defines the <see cref="DynamicOverflowOrder"/> property
    /// </summary>
    public static readonly DirectProperty<CommandBarButton, int> DynamicOverflowOrderProperty =
        AvaloniaProperty.RegisterDirect<CommandBarButton, int>(nameof(DynamicOverflowOrder),
            x => x.DynamicOverflowOrder, (x, v) => x.DynamicOverflowOrder = v);

    /// <summary>
    /// Defines the <see cref="IsCompact"/> property
    /// </summary>
    public static readonly StyledProperty<bool> IsCompactProperty =
        AvaloniaProperty.Register<CommandBarButton, bool>(nameof(IsCompact));

    /// <summary>
    /// Defines the <see cref="TemplateSettings"/> property
    /// </summary>
    public static readonly StyledProperty<CommandBarButtonTemplateSettings> TemplateSettingsProperty =
        AvaloniaProperty.Register<CommandBarButton, CommandBarButtonTemplateSettings>(nameof(TemplateSettings));

    /// <summary>
    /// Gets or sets a value that indicates whether the button is shown with no label and reduced padding.
    /// </summary>
    public bool IsCompact
    {
        get => GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    /// <summary>
    /// Gets a value that indicates whether this item is in the overflow menu.
    /// </summary>
    public bool IsInOverflow
    {
        get => _isInOverflow;
        internal set
        {
            if (SetAndRaise(IsInOverflowProperty, ref _isInOverflow, value))
            {
                PseudoClasses.Set(SharedPseudoclasses.s_pcOverflow, value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the graphic content of the app bar toggle button.
    /// </summary>
    public IconSource IconSource
    {
        get => GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the text description displayed on the app bar toggle button.
    /// </summary>
    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <inheritdoc/>
    public int DynamicOverflowOrder
    {
        get => _dynamicOverflowOrder;
        set => SetAndRaise(DynamicOverflowOrderProperty, ref _dynamicOverflowOrder, value);
    }

    /// <summary>
    /// Gets the template settings for this CommandBarButton
    /// </summary>
    public CommandBarButtonTemplateSettings TemplateSettings
    {
        get => GetValue(TemplateSettingsProperty);
        private set => SetValue(TemplateSettingsProperty, value);
    }

    private bool _isInOverflow;
    private int _dynamicOverflowOrder;

    private const string s_pcSubmenuOpen = ":submenuopen";
}
