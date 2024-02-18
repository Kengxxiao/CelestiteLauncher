using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;

namespace Celestite.Controls;

[PseudoClasses(":allowClick", s_pcEmpty)]
[TemplatePart(s_tpExpander, typeof(Expander))]
public partial class DmmGameExpander : HeaderedContentControl
{
    /// <summary>
    /// Defines the <see cref="Description"/> property
    /// </summary>
    public static readonly StyledProperty<string> DescriptionProperty =
        AvaloniaProperty.Register<DmmGameExpander, string>(nameof(Description));
    public static readonly StyledProperty<string> Description2Property =
        AvaloniaProperty.Register<DmmGameExpander, string>(nameof(Description2));

    /// <summary>
    /// Defines the <see cref="IImage"/> property
    /// </summary>
    public static readonly StyledProperty<IImage> GameIconProperty =
        AvaloniaProperty.Register<DmmGameExpander, IImage>(nameof(GameIcon));

    public static readonly StyledProperty<IconSource?> Action1Property =
        AvaloniaProperty.Register<DmmGameExpander, IconSource?>(nameof(Action1));
    public static readonly StyledProperty<IconSource?> Action2Property =
        AvaloniaProperty.Register<DmmGameExpander, IconSource?>(nameof(Action2));
    public static readonly StyledProperty<IconSource?> Action3Property =
        AvaloniaProperty.Register<DmmGameExpander, IconSource?>(nameof(Action3));

    public static readonly StyledProperty<ICommand?> Action1CommandProperty =
        AvaloniaProperty.Register<DmmGameExpander, ICommand?>(nameof(Action1Command));
    public static readonly StyledProperty<ICommand?> Action2CommandProperty =
        AvaloniaProperty.Register<DmmGameExpander, ICommand?>(nameof(Action2Command));
    public static readonly StyledProperty<ICommand?> Action3CommandProperty =
        AvaloniaProperty.Register<DmmGameExpander, ICommand?>(nameof(Action3Command));

    public static readonly StyledProperty<string> Action1LabelProperty =
        AvaloniaProperty.Register<DmmGameExpander, string>(nameof(Action1Label));
    public static readonly StyledProperty<string> Action2LabelProperty =
        AvaloniaProperty.Register<DmmGameExpander, string>(nameof(Action2Label));
    public static readonly StyledProperty<string> Action3LabelProperty =
        AvaloniaProperty.Register<DmmGameExpander, string>(nameof(Action3Label));

    public static readonly StyledProperty<bool> UseProgressProperty =
        AvaloniaProperty.Register<DmmGameExpander, bool>(nameof(UseProgress));
    public static readonly StyledProperty<double> ProgressValueProperty =
        AvaloniaProperty.Register<DmmGameExpander, double>(nameof(ProgressValue));

    /// <summary>
    /// Gets or sets the description text
    /// </summary>
    public string Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string Description2
    {
        get => GetValue(Description2Property);
        set => SetValue(Description2Property, value);
    }

    /// <summary>
    /// Gets or sets the IconSource for the DmmGameExpander
    /// </summary>
    public IImage GameIcon
    {
        get => GetValue(GameIconProperty);
        set => SetValue(GameIconProperty, value);
    }

    public IconSource? Action1
    {
        get => GetValue(Action1Property);
        set => SetValue(Action1Property, value);
    }

    public IconSource? Action2
    {
        get => GetValue(Action2Property);
        set => SetValue(Action2Property, value);
    }
    public IconSource? Action3
    {
        get => GetValue(Action3Property);
        set => SetValue(Action3Property, value);
    }

    public string Action1Label
    {
        get => GetValue(Action1LabelProperty);
        set => SetValue(Action1LabelProperty, value);
    }
    public string Action2Label
    {
        get => GetValue(Action2LabelProperty);
        set => SetValue(Action2LabelProperty, value);
    }
    public string Action3Label
    {
        get => GetValue(Action3LabelProperty);
        set => SetValue(Action3LabelProperty, value);
    }

    public ICommand? Action1Command
    {
        get => GetValue(Action1CommandProperty);
        set => SetValue(Action1CommandProperty, value);
    }

    public ICommand? Action2Command
    {
        get => GetValue(Action2CommandProperty);
        set => SetValue(Action2CommandProperty, value);
    }
    public ICommand? Action3Command
    {
        get => GetValue(Action3CommandProperty);
        set => SetValue(Action3CommandProperty, value);
    }
    public bool UseProgress
    {
        get => GetValue(UseProgressProperty);
        set => SetValue(UseProgressProperty, value);
    }

    public double ProgressValue
    {
        get => GetValue(ProgressValueProperty);
        set => SetValue(ProgressValueProperty, value);
    }

    private const string s_tpExpander = "Expander";
    private const string s_pcEmpty = ":empty";
}

