namespace TB_Browser.Core.Services
{
    public interface IBrowserService
    {
        ITabService TabService { get; set; }
        void Navigate(string url);
        void GoBack();
        void GoForward();
        void Reload();
        event EventHandler<string> UrlChanged;
    }
}
