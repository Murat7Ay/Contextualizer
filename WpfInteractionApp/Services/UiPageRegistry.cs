using WpfInteractionApp.Pages;

namespace WpfInteractionApp.Services
{
    public sealed class UiPageRegistry
    {
        private UiPageRegistry() { }

        public static UiPageRegistry Instance { get; } = new UiPageRegistry();

        public HandlersPage? HandlersPage { get; internal set; }
    }
}


