using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using ReactiveUI;

namespace YetAnotherXmppClient.UI
{
    public class HandlerAwaitingInteraction<TInput, TOutput> : Interaction<TInput, TOutput>
    {
        private readonly TaskCompletionSource<TOutput> tcs = new TaskCompletionSource<TOutput>();

        public IDisposable RegisterHandlerAndNotify(Func<InteractionContext<TInput, TOutput>, Task> handler)
        {
            var disposable = this.RegisterHandler(handler);
            this.tcs.TrySetResult(default);
            return disposable;
        }

        public override IObservable<TOutput> Handle(TInput input)
        {
            return this.tcs.Task.ToObservable().Concat(Observable.Defer(() => base.Handle(input))).LastAsync();
        }
    }
}