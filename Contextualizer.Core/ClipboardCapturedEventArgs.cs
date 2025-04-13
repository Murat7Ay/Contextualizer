namespace Contextualizer.Core
{
    public class ClipboardCapturedEventArgs : EventArgs
    {
        public ClipboardContent ClipboardContent { get; }

        public ClipboardCapturedEventArgs(ClipboardContent clipboardContent)
        {
            ClipboardContent = clipboardContent;
        }
    }
}
