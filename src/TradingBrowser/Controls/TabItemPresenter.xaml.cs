using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI; // Provides the Color struct
using static Windows.UI.Colors; // FIX: Import static Colors class members (White, Transparent, etc.)
using System;

namespace TradingBrowser.Controls;

/// <summary>
/// Custom tab item control with trapezoidal shape, middle-click close, and visual state management.
/// Handles pointer events and exposes clean actions for the MainWindow to consume.
/// </summary>
public sealed partial class TabItemPresenter : UserControl
{
    // Dependency properties for data binding from XAML
    public static readonly DependencyProperty TitleProperty = 
        DependencyProperty.Register("Title", typeof(string), typeof(TabItemPresenter), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty IsSelectedProperty = 
        DependencyProperty.Register("IsSelected", typeof(bool), typeof(TabItemPresenter), new PropertyMetadata(false, OnIsSelectedChanged));
    public static readonly DependencyProperty IsPinnedProperty = 
        DependencyProperty.Register("IsPinned", typeof(bool), typeof(TabItemPresenter), new PropertyMetadata(false));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsPinned
    {
        get => (bool)GetValue(IsPinnedProperty);
        set => SetValue(IsPinnedProperty, value);
    }

    /// <summary>
    /// Internal brushes bound to XAML for state-driven theming.
    /// Uses Windows.UI.Colors which is the correct namespace for WinUI 3.
    /// </summary>
    private SolidColorBrush BackgroundBrush => IsSelected 
        ? new SolidColorBrush(Color.FromArgb(255, 32, 33, 36)) 
        : new SolidColorBrush(Transparent); // FIX: Now resolves via 'using static Windows.UI.Colors'
        
    private SolidColorBrush ForegroundBrush => IsSelected 
        ? new SolidColorBrush(White) // FIX: Now resolves via 'using static Windows.UI.Colors'
        : new SolidColorBrush(Color.FromArgb(255, 154, 160, 166));

    /// <summary>
    /// Event fired when the middle mouse button is pressed on this tab.
    /// </summary>
    public event Action? MiddleClicked;

    /// <summary>
    /// Event fired when the close button is clicked.
    /// </summary>
    public event Action? CloseClicked;

    public TabItemPresenter()
    {
        this.InitializeComponent();
        // Default close button visibility based on selection state
        CloseButton.Visibility = IsSelected ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Callback triggered by the dependency property system when IsSelected changes.
    /// Updates the VisualStateManager and close button visibility.
    /// </summary>
    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TabItemPresenter control)
        {
            bool isSelected = (bool)e.NewValue;
            VisualStateManager.GoToState(control, isSelected ? "Selected" : "Normal", true);
            control.CloseButton.Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Handles pointer input to detect middle-click (mouse wheel button) for quick tab closing.
    /// Uses PointerRoutedEventArgs which supports the Handled property.
    /// </summary>
    private void UserControl_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint(this).Properties;
        if (props.IsMiddleButtonPressed)
        {
            // Mark as handled to prevent the click from bubbling up to the ListView selection logic
            e.Handled = true;
            MiddleClicked?.Invoke();
        }
    }

    /// <summary>
    /// Handles the X button click event.
    /// RoutedEventArgs does NOT have a Handled property in WinUI 3, so we simply invoke the action.
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseClicked?.Invoke();
    }
}
