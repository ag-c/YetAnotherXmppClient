namespace YetAnotherXmppClient.Infrastructure.Commands
{
    // for internal use only: protocol handlers can register implemented discoverable features through this command
    internal class RegisterFeatureCommand : ICommand
    {
        public string ProtocolNamespace { get; }

        public RegisterFeatureCommand(string protocolNamespace)
        {
            this.ProtocolNamespace = protocolNamespace;
        }
    }
}
