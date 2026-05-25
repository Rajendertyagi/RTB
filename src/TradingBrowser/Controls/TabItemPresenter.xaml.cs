using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using System;

namespace TradingBrowser.Controls;

public sealed partial class TabItemPresenter : UserControl
{
    public static readonly DependencyProperty TitleProperty = 
        DependencyProperty.Register("Title", typeof(string), typeof(TabItemPresenter), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty IsSelectedProperty = 
        DependencyProperty.Register("IsSelected", typeof(bool), typeof(TabItemPresenter), new PropertyMetadata(false, OnIsSelectedChanged));
    public static readonly DependencyProperty IsPinnedProperty = 
        DependencyProperty.Register("IsPinned", typeof(bool), typeof(TabItemPresenter), new PropertyMetadata(false));

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public bool IsPinned { get => (bool)GetValue(IsPinnedProperty); set => SetValue(IsPinnedProperty, value); }

    private SolidColorBrush BackgroundBrush => IsSelected 
        ? new SolidColorBrush(Color.FromArgb(255, 32, 33, 36)) 
        : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        
    private SolidColorBrush ForegroundBrush => IsSelected 
        ? new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) 
        : new SolidColorBrush(Color.FromArgb(255, 154, 160, 166));

    // FIX: Use RoutedEventHandler instead of Action to match XAML event handler signatures
    public event RoutedEventHandler? MiddleClicked;
    public event RoutedEventHandler? CloseClicked;

    public TabItemPresenter()
    {
        this.InitializeComponent();
        CloseButton.Visibility = IsSelected ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TabItemPresenter control)
        {
            bool isSelected = (bool)e.NewValue;
            VisualStateManager.GoToState(control, isSelected ? "Selected" : "Normal", true);
            control.CloseButton.Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void UserControl_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint(this).Properties;
        if (props.IsMiddleButtonPressed)
        {
            e.Handled = true;
            MiddleClicked?.Invoke(this, new RoutedEventArgs());
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseClicked?.Invoke(this, new RoutedEventArgs());
    }
}
