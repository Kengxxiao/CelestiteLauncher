using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Celestite.Pages;

public partial class HomePage : UserControl
{
    private long _elapsedMillisecondsAfterLastChange;
    private DispatcherTimer? _timer;
    public HomePage()
    {
        InitializeComponent();
    }

    private void SliderPrevious(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Slider.SelectedIndex == 0)
            Slider.SelectedIndex = Slider.Items.Count - 1;
        else
            Slider.Previous();
        _elapsedMillisecondsAfterLastChange = Environment.TickCount64;
    }
    private void SliderNext(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => MakeSliderNext();
    private void MakeSliderNext()
    {
        if (Slider.SelectedIndex == Slider.Items.Count - 1)
            Slider.SelectedIndex = 0;
        else
            Slider.Next();
        _elapsedMillisecondsAfterLastChange = Environment.TickCount64;
    }

    private void TimerTick(object? sender, EventArgs args)
    {
        if (Environment.TickCount64 - _elapsedMillisecondsAfterLastChange > 5000)
            MakeSliderNext();
    }

    private void Carousel_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _timer ??= new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _elapsedMillisecondsAfterLastChange = Environment.TickCount64;
        _timer.Tick += TimerTick;
        _timer.IsEnabled = true;
    }

    private void Carousel_OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (_timer == null) return;
        _timer.Tick -= TimerTick;
        _timer.IsEnabled = false;
    }
}