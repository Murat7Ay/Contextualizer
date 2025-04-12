namespace Contextualizer.Core
{
    public class TextCapturedEventArgs : EventArgs
    {
        public string CapturedText { get; }

        public TextCapturedEventArgs(string capturedText)
        {
            CapturedText = capturedText;
        }
    }
}
