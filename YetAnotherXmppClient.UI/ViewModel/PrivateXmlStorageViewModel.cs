using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using ReactiveUI;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Queries;

namespace YetAnotherXmppClient.UI.ViewModel
{
    public class PrivateXmlStorageViewModel : ReactiveObject
    {
        private readonly IMediator mediator;

        public ReactiveCommand<Unit, Unit> RetrieveCommand { get; }
        public ReactiveCommand<Unit, Unit> StoreCommand { get; }

        private string expandedXName;
        public string ExpandedXName
        {
            get => this.expandedXName;
            set
            {
                this.expandedXName = value;
                this.ValidateExpandedXName();
            }
        }

        private string xml;
        public string Xml
        {
            get => this.xml;
            set
            {
                this.RaiseAndSetIfChanged(ref this.xml, value);
                this.ValidateXml();
            }
        }

        private bool isValidXName;
        public bool IsValidXName
        {
            get => this.isValidXName;
            set => this.RaiseAndSetIfChanged(ref this.isValidXName, value);
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

        private void ValidateXml()
        {
            try
            {
                XElement.Parse(this.Xml);
            }
            catch (XmlException)
            {
                this.IsValidXml = false;
                return;
            }

            this.IsValidXml = true;
        }

        private void ValidateExpandedXName()
        {
            try
            {
                XName.Get(this.ExpandedXName);
            }
            catch (ArgumentException)
            {
                this.IsValidXName = false;
                return;
            }

            this.IsValidXName = true;
        }

        private async Task OnRetrieveAsync()
        {
            this.Xml = await this.mediator.QueryAsync<RetrievePrivateXmlQuery, string>(new RetrievePrivateXmlQuery(this.ExpandedXName));
        }

        private async Task OnStoreAsync()
        {
            var responseIq = await this.mediator.QueryAsync<StorePrivateXmlQuery, Iq>(new StorePrivateXmlQuery(this.Xml));
            await Interactions.ShowStorePrivateXmlStorageResponse.Handle(responseIq.ToString());
        }

    }
}
