using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace TradingBrowser.Controls;

public sealed partial class TabItemPresenter : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(TabItemPresenter), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(TabItemPresenter), new PropertyMetadata(false, OnIsActiveChanged));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public event TypedEventHandler<object, PointerRoutedEventArgs>? MiddleClicked;
    public event TypedEventHandler<object, RoutedEventArgs>? CloseClicked;
    public event TypedEventHandler<object, RightTappedRoutedEventArgs>? TabRightTapped;

    public TabItemPresenter()
    {
        this.InitializeComponent();
    }

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TabItemPresenter presenter)
        {
            VisualStateManager.GoToState(presenter, presenter.IsActive ? "Active" : "Inactive", true);
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

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseClicked?.Invoke(this, e);
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
        {
            MiddleClicked?.Invoke(this, e);
            e.Handled = true;
        }
    }

    private void RootGrid_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        TabRightTapped?.Invoke(this, e);
        e.Handled = true;
    }
}
