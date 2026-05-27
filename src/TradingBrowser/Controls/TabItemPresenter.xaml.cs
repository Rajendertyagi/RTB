using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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

    public event EventHandler<PointerRoutedEventArgs>? MiddleClicked;
    public event EventHandler<RoutedEventArgs>? CloseClicked;
    public event EventHandler<RightTappedRoutedEventArgs>? TabRightTapped;

    public TabItemPresenter() => this.InitializeComponent();

    // ✅ FIX: Bulletproof active state styling via direct property assignment
    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TabItemPresenter p)
        {
            if (p.IsActive)
            {
                p.TabBackground.Fill = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"];
                p.TabBackground.Stroke = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
                p.BottomCover.Visibility = Visibility.Visible;
                p.CloseButton.Visibility = Visibility.Visible;
            }
            else
            {
                p.TabBackground.Fill = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                p.TabBackground.Stroke = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                p.BottomCover.Visibility = Visibility.Collapsed;
                p.CloseButton.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => CloseClicked?.Invoke(this, e);

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
        {
            MiddleClicked?.Invoke(this, e);
            e.Handled = true;
        }
    }

    // ✅ FIX: Override at UserControl level to bypass ListView event swallowing
    protected override void OnRightTapped(RightTappedRoutedEventArgs e)
    {
        base.OnRightTapped(e);
        TabRightTapped?.Invoke(this, e);
        e.Handled = true;
    }
}
