using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;

namespace TradingBrowser.Controls;

public sealed partial class TabItemPresenter : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(TabItemPresenter), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(TabItemPresenter), new PropertyMetadata(false, OnIsActiveChanged));

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public bool IsActive { get => (bool)GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }

    public event EventHandler<RoutedEventArgs>? CloseClicked;
    public event EventHandler<RoutedEventArgs>? CloseTabRequested;
    public event EventHandler<RoutedEventArgs>? CloseOtherTabsRequested;

    public TabItemPresenter() => this.InitializeComponent();

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TabItemPresenter p) p.ApplyActiveState();
    }

    private void ApplyActiveState()
    {
        if (IsActive)
        {
            ActiveBackground.Visibility = Visibility.Visible;
            InactiveBackground.Visibility = Visibility.Collapsed;
            BottomCover.Visibility = Visibility.Visible;
            CloseButton.Visibility = Visibility.Visible;
        }
        else
        {
            ActiveBackground.Visibility = Visibility.Collapsed;
            InactiveBackground.Visibility = Visibility.Visible;
            BottomCover.Visibility = Visibility.Collapsed;
            CloseButton.Visibility = Visibility.Collapsed;
        }
    }

    private void RootGrid_PointerEntered(object sender, PointerRoutedEventArgs e) 
    { 
        CloseButton.Visibility = Visibility.Visible; 
    }

    private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e) 
    { 
        if (!IsActive) CloseButton.Visibility = Visibility.Collapsed; 
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => CloseClicked?.Invoke(this, e);
    
    // ✅ FIX: ContextFlyout click handlers
    private void CloseTab_Click(object sender, RoutedEventArgs e) => CloseTabRequested?.Invoke(this, e);
    private void CloseOtherTabs_Click(object sender, RoutedEventArgs e) => CloseOtherTabsRequested?.Invoke(this, e);

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
        {
            CloseClicked?.Invoke(this, e);
            e.Handled = true;
        }
    }
}
