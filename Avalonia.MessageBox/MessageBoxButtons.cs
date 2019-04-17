using System;

namespace StarDebris.Avalonia.MessageBox
{
    [Flags]
    public enum MessageBoxButtons
    {
        Ok = 1,
        Cancel = 2,
        Yes = 4,
        No = 8,
        Retry = 16,

        None = 0,
        All = Ok | Cancel | Yes | No | Retry
    }
}