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
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(TabItemPresenter), new PropertyMetadata("New Tab"));

        // EDGE UI: Active State Property
        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(TabItemPresenter), new PropertyMetadata(false, OnIsActiveChanged));

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TabItemPresenter presenter)
            {
                bool isActive = (bool)e.NewValue;
                VisualStateManager.GoToState(presenter, isActive ? "Active" : "Normal", true);
            }
        }

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
            args.Handled = true; 
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseClicked?.Invoke(this, e);
        }
    }
}
