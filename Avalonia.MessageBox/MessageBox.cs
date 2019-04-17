using System;
using Avalonia;
using System.Threading;

namespace StarDebris.Avalonia.MessageBox
{
    public class MessageBox
    {
        private string messageBoxText {get;set;}
        private MessageBoxStyle style {get;set;}
        private MessageBoxButtons buttons{get;set;}

        /// <summary>
        ///     Displays a modal with a specified message.
        /// </summary>
        /// <param name="messageBoxText">Message to be displayed</param>
        public MessageBox(string messageBoxText)
        {
            this.messageBoxText = messageBoxText;
        }

        /// <summary>
        ///     Displays a modal with a specified message.
        /// </summary>
        /// <param name="messageBoxText">Message to be displayed</param>
        /// <param name="closed">Closed event, used to get the messagebox result.</param>
        public MessageBox(string messageBoxText, MessageClosed closed)
        {
            this.messageBoxText = messageBoxText;
            OnMessageClosed = closed;
        }

        /// <summary>
        ///     Displays a modal with a specified message.
        /// </summary>
        /// <param name="messageBoxText">Message to be displayed</param>
        /// <param name="style">The style of the message (WIP)</param>
        /// <param name="buttons">The buttons to be displayed (Multiple buttons can be used with the use of | )</param>
        public MessageBox(string messageBoxText, MessageBoxStyle style = MessageBoxStyle.Info, MessageBoxButtons buttons = MessageBoxButtons.Ok)
        {
            this.messageBoxText = messageBoxText;
            this.style = style;
            this.buttons = buttons;
        }

        /// <summary>
        ///     Displays a modal with a specified message.
        /// </summary>
        /// <param name="messageBoxText">Message to be displayed</param>
        /// <param name="closed">Closed event, used to get the messagebox result.</param>
        /// <param name="style">The style of the message (WIP)</param>
        /// <param name="buttons">The buttons to be displayed (Multiple buttons can be used with the use of | )</param>
        public MessageBox(string messageBoxText, MessageClosed closed, MessageBoxStyle style = MessageBoxStyle.Info, MessageBoxButtons buttons = MessageBoxButtons.Ok)
        {
            this.messageBoxText = messageBoxText;
            this.style = style;
            this.buttons = buttons;
            OnMessageClosed = closed;
        }

        /// <summary>
        ///     Displays the messagebox modal.
        /// </summary>
        public void Show()
        {
            MessageBoxWindow msg = new MessageBoxWindow(messageBoxText, style, buttons);
            msg.Show();

            new Thread(delegate() {
                while (true)
                {
                    if (msg.result != null)
                    {
                        OnMessageClosed(msg.result, e);
                        break;
                    }
                }
            }).Start();
        }

        public event MessageClosed OnMessageClosed;
        public EventArgs e = null;
        /// <summary>
        ///     Fired once a button is pressed on the messagebox modal. (Excludes window close button)
        /// </summary>
        /// <param name="result">Contains the buttons pressed that caused the modal to close.</param>
        /// <param name="e">null by default</param>
        public delegate void MessageClosed(DialogResult result, EventArgs e);
    }
}