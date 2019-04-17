using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Logging.Serilog;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;

namespace StarDebris.Avalonia.MessageBox
{
    internal class MessageBoxWindow : Window
    {
        public MessageBoxWindow(string messageBoxText, MessageBoxStyle style = MessageBoxStyle.Info, MessageBoxButtons buttons = MessageBoxButtons.Ok)
        {
            this.msgText = messageBoxText;
            this.Title = style.ToString();

            InitializeComponent();

            //Let's hide all buttons first
            okBtn.IsVisible = 
            cancelBtn.IsVisible = 
            retryBtn.IsVisible = 
            yesBtn.IsVisible = 
            noBtn.IsVisible = false;

            Console.WriteLine("Seledcted buttons to display: " + buttons);
            
            //Let's check which buttons are flagged to be shown
            if ((buttons & MessageBoxButtons.Cancel) == MessageBoxButtons.Cancel)
                cancelBtn.IsVisible = true;

            if ((buttons & MessageBoxButtons.Retry) == MessageBoxButtons.Retry)
                retryBtn.IsVisible = true;
                
            if ((buttons & MessageBoxButtons.No) == MessageBoxButtons.No)
                noBtn.IsVisible = true;

            if ((buttons & MessageBoxButtons.Yes) == MessageBoxButtons.Yes)
                yesBtn.IsVisible = true;

            if ((buttons & MessageBoxButtons.Ok) == MessageBoxButtons.Ok)
                okBtn.IsVisible = true;
        }

        public DialogResult result;

        private string msgText { get; set; }

        private Button okBtn;
        private Button cancelBtn;
        private Button yesBtn;
        private Button noBtn;
        private Button retryBtn;

        private TextBlock msgBoxText;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            //Get controls from XAML
            this.okBtn = this.Find<Button>("msgBoxOkBtn");
            this.cancelBtn = this.Find<Button>("msgBoxCancelBtn");
            this.yesBtn = this.Find<Button>("msgBoxYesBtn");
            this.noBtn = this.Find<Button>("msgBoxNoBtn");
            this.retryBtn = this.Find<Button>("msgBoxRetryBtn");

            this.msgBoxText = this.Find<TextBlock>("msgBoxText");

            //Assign events
            okBtn.Click += okBtn_Clicked;
            cancelBtn.Click += closeBtn_Clicked;
            yesBtn.Click += yesBtn_Clicked;
            noBtn.Click += noBtn_Clicked;
            retryBtn.Click += retryBtn_Clicked;

            msgBoxText.Text = msgText;

            //Make sure to fit content
            this.SizeToContent = SizeToContent.Width;
            this.Renderer.Start();
        }

        private void retryBtn_Clicked(object sender, RoutedEventArgs e)
        {
            result = new DialogResult(MessageBoxButtons.Retry);
            Close();
        }

        private void noBtn_Clicked(object sender, RoutedEventArgs e)
        {
            result = new DialogResult(MessageBoxButtons.No);
            Close();
        }

        private void yesBtn_Clicked(object sender, RoutedEventArgs e)
        {
            result = new DialogResult(MessageBoxButtons.Yes);
            Close();
        }

        private void closeBtn_Clicked(object sender, RoutedEventArgs e)
        {
            result = new DialogResult(MessageBoxButtons.Cancel);
            Close();
        }

        private void okBtn_Clicked(object sender, RoutedEventArgs e)
        {
            result = new DialogResult(MessageBoxButtons.Ok);
            Close();
        }
    }
}