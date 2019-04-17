namespace StarDebris.Avalonia.MessageBox
{
    public class DialogResult
    {
        public DialogResult(MessageBoxButtons buttonClicked)
        {
            result = buttonClicked;
        }

        public MessageBoxButtons result { get; private set; }
    }
}