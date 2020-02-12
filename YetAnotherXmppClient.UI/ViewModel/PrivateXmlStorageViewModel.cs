using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using YetAnotherXmppClient.Infrastructure;

namespace YetAnotherXmppClient.UI.ViewModel
{
    public class PrivateXmlStorageViewModel : ReactiveObject
    {
        private readonly IMediator mediator;

        public ReactiveCommand<Unit, Unit> RetrieveCommand { get; }
        public ReactiveCommand<Unit, Unit> StoreCommand { get; }

        public string XName { get; set; }

        private string xml;
        public string Xml
        {
            get => this.xml;
            set => this.RaiseAndSetIfChanged(ref this.xml, value);
        }

        private bool isValidXml;
        public bool IsValidXml
        {
            get => this.isValidXml;
            set => this.RaiseAndSetIfChanged(ref this.isValidXml, value);
        }


        public PrivateXmlStorageViewModel(IMediator mediator)
        {
            this.mediator = mediator;
            this.RetrieveCommand = ReactiveCommand.CreateFromTask(this.OnRetrieveAsync);
            this.StoreCommand = ReactiveCommand.CreateFromTask(this.OnStoreAsync);
        }


        private async Task OnRetrieveAsync()
        {
        }

        private async Task OnStoreAsync()
        {
        }

    }
}
