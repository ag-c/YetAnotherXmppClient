using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.Infrastructure.Commands
{
    public class SetMoodCommand : ICommand
    {
        /// <summary>
        /// if null, then mood publishing will be disabled
        /// </summary>
        public Mood? Mood { get; set; } 

        public string Text { get; set; }
    }
}
