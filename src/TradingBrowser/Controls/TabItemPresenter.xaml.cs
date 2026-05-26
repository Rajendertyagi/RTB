using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;

namespace TradingBrowser.Controls
{
    public sealed partial class TabItemPresenter : UserControl
    {
        public TabItemPresenter()
        {
            this.InitializeComponent();
            this.PointerEntered += (s, e) => VisualStateManager.GoToState(this, "PointerOver", true);
            this.PointerExited += (s, e) => VisualStateManager.GoToState(this, "Normal", true);
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(TabItemPresenter), new PropertyMetadata("New Tab"));

        public event EventHandler<PointerRoutedEventArgs>? MiddleClicked;
        public new event EventHandler<ContextRequestedEventArgs>? ContextRequested;
        public event EventHandler<RoutedEventArgs>? CloseClicked;

        private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
            {
                MiddleClicked?.Invoke(this, e);
                e.Handled = true;
            }
        }

        private void RootGrid_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            ContextRequested?.Invoke(this, args);
            args.Handled = true; // FIX 1: Prevents default context menu from stealing the click
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseClicked?.Invoke(this, e);
        }
    }
}
