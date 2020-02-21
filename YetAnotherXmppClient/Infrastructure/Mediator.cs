using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Serilog;

namespace YetAnotherXmppClient.Infrastructure
{
    public interface IEvent { }

    public interface ICommand { }

    public interface IQuery<TResult> { }


    public interface IEventHandler { }
    public interface IEventHandler<TEvent> : IEventHandler where TEvent : IEvent
    {
        Task HandleEventAsync(TEvent evt);
    }

    public interface IQueryHandler { }
    public interface IQueryHandler<TQuery, TResult> : IQueryHandler
    {
        TResult HandleQuery(TQuery query);
    }

    public interface IAsyncQueryHandler { }
    public interface IAsyncQueryHandler<TQuery, TResult> : IAsyncQueryHandler
    {
        Task<TResult> HandleQueryAsync(TQuery query);
    }

    public interface IAsyncCommandHandler { }
    public interface IAsyncCommandHandler<TCommand> : IAsyncCommandHandler
    {
        Task HandleCommandAsync(TCommand command);
    }

    public interface ICommandHandler { }
    public interface ICommandHandler<TCommand> : ICommandHandler
    {
        void HandleCommand(TCommand command);
    }

    public interface IMediator
    {
        void RegisterHandler<TEvent>(IEventHandler<TEvent> handler, bool publishLatestEventToNewHandler = false) where TEvent : IEvent;
        void RegisterHandler<TEvent>(Action<TEvent> action, bool publishLatestEventToNewHandler = false) where TEvent : IEvent;
        void RegisterHandler<TEvent>(Func<TEvent, Task> asyncFunc, bool publishLatestEventToNewHandler = false) where TEvent : IEvent;
        void RegisterHandler<TQuery, TResult>(IQueryHandler<TQuery, TResult> handler) where TQuery : IQuery<TResult>;
        void RegisterHandler<TQuery, TResult>(IAsyncQueryHandler<TQuery, TResult> handler) where TQuery : IQuery<TResult>;
        void RegisterHandler<TQuery, TResult>(Expression<Func<TQuery,Task<TResult>>> handler) where TQuery : IQuery<TResult>;
        void RegisterHandler<TCommand>(IAsyncCommandHandler<TCommand> handler) where TCommand : ICommand;
        void RegisterHandler<TCommand>(ICommandHandler<TCommand> handler) where TCommand : ICommand;
        void RegisterHandler<TEvent>(Expression<Func<TEvent, Task>> handler) where TEvent : IEvent;

        Task PublishAsync<TEvent>(TEvent evt) where TEvent : IEvent;
        TResult Query<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>;
        TResult Query<TQuery, TResult>(params object[] parameters) where TQuery : IQuery<TResult>;
        Task<TResult> QueryAsync<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>;
        Task<TResult> QueryAsync<TQuery, TResult>() where TQuery : IQuery<TResult>, new();
        Task<TResult> QueryAsync<TQuery, TResult>(params object[] parameters) where TQuery : IQuery<TResult>;
        Task ExecuteAsync<TCommand>(TCommand command) where TCommand : ICommand;
        void Execute<TCommand>(TCommand command) where TCommand : ICommand;
    }

    public class Mediator : IMediator
    {
        private readonly Dictionary<Type, object> latestEvents = new Dictionary<Type, object>();
        private readonly Dictionary<Type, List<IEventHandler>> eventHandlers = new Dictionary<Type, List<IEventHandler>>();
        private readonly Dictionary<Type, IQueryHandler> queryHandlers = new Dictionary<Type, IQueryHandler>();
        private readonly Dictionary<Type, IAsyncQueryHandler> asyncQueryHandlers = new Dictionary<Type, IAsyncQueryHandler>();
        private readonly Dictionary<Type, IAsyncCommandHandler> asyncCommandHandlers = new Dictionary<Type, IAsyncCommandHandler>();
        private readonly Dictionary<Type, ICommandHandler> commandHandlers = new Dictionary<Type, ICommandHandler>();
        private readonly Dictionary<Type, Delegate> delQueryHandlers = new Dictionary<Type, Delegate>();
        private readonly Dictionary<Type, Delegate> delEventHandlers = new Dictionary<Type, Delegate>();

        private readonly Dictionary<Type, IAsyncQueryContext> waitingAsyncQueries = new Dictionary<Type, IAsyncQueryContext>();


        public void RegisterHandler<TEvent>(Action<TEvent> action, bool publishLatestEventToNewHandler = false) where TEvent : IEvent
        {
            this.RegisterHandler<TEvent>(new ActionEventHandler<TEvent>(action), publishLatestEventToNewHandler);
        }

        public void RegisterHandler<TEvent>(Func<TEvent, Task> asyncFunc, bool publishLatestEventToNewHandler = false) where TEvent : IEvent
        {
            this.RegisterHandler<TEvent>(new ActionEventHandler<TEvent>(asyncFunc), publishLatestEventToNewHandler);
        }

        public void RegisterHandler<TEvent>(IEventHandler<TEvent> handler, bool publishLatestEventToNewHandler = false) where TEvent : IEvent
        {
            if(!this.eventHandlers.ContainsKey(typeof(TEvent)))
                this.eventHandlers.Add(typeof(TEvent), new List<IEventHandler>());

            this.eventHandlers[typeof(TEvent)].Add(handler);

            if (this.latestEvents.ContainsKey(typeof(TEvent)))
                Task.Run(() => handler.HandleEventAsync((TEvent)this.latestEvents[typeof(TEvent)]));
        }

        public void RegisterHandler<TQuery, TResult>(IQueryHandler<TQuery, TResult> handler) where TQuery : IQuery<TResult>
        {
            this.queryHandlers[typeof(TQuery)] = handler;
        }

        public void RegisterHandler<TQuery, TResult>(IAsyncQueryHandler<TQuery, TResult> handler) where TQuery : IQuery<TResult>
        {
            this.asyncQueryHandlers[typeof(TQuery)] = handler;
            if (this.waitingAsyncQueries.TryGetValue(typeof(TQuery), out var queryContext))
            {
                this.waitingAsyncQueries.Remove((typeof(TQuery)));
                Task.Run(async () =>
                    {
                        var result = await handler.HandleQueryAsync((TQuery)queryContext.Query).ConfigureAwait(false);
                        queryContext.Fulfill(result);
                    });
            }
        }

        public void RegisterHandler<TQuery, TResult>(Expression<Func<TQuery, Task<TResult>>> handler) where TQuery : IQuery<TResult>
        {
            this.delQueryHandlers[typeof(TQuery)] = ((LambdaExpression)handler).Compile(false);
        }

        public void RegisterHandler<TCommand>(IAsyncCommandHandler<TCommand> handler) where TCommand : ICommand
        {
            this.asyncCommandHandlers[typeof(TCommand)] = handler;
        }

        public void RegisterHandler<TCommand>(ICommandHandler<TCommand> handler) where TCommand : ICommand
        {
            this.commandHandlers[typeof(TCommand)] = handler;
        }

        public void RegisterHandler<TEvent>(Expression<Func<TEvent, Task>> handler) where TEvent : IEvent
        {
            this.delEventHandlers[typeof(TEvent)] = ((LambdaExpression)handler).Compile(false);
        }

        public TResult Query<TQuery, TResult>(params object[] parameters) where TQuery : IQuery<TResult>
        {
            var ctorInfo = typeof(TQuery).GetConstructor(parameters.Select(obj => obj.GetType()).ToArray());
            return this.Query<TQuery, TResult>((TQuery)ctorInfo.Invoke(parameters));
        }

        public TResult Query<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>
        {
            if (this.delQueryHandlers.ContainsKey(typeof(TQuery)))
            {
                return (TResult)this.delQueryHandlers[typeof(TQuery)].DynamicInvoke(query);
            }

            var handler = this.queryHandlers[typeof(TQuery)];
            return ((IQueryHandler<TQuery, TResult>)handler).HandleQuery(query);
        }

        public Task<TResult> QueryAsync<TQuery, TResult>(params object[] parameters) where TQuery : IQuery<TResult>
        {
            var ctorInfo = typeof(TQuery).GetConstructor(parameters.Select(obj => obj.GetType()).ToArray());
            return this.QueryAsync<TQuery, TResult>((TQuery)ctorInfo.Invoke(parameters));
        }

        public Task<TResult> QueryAsync<TQuery, TResult>() where TQuery : IQuery<TResult>, new()
        {
            return this.QueryAsync<TQuery, TResult>(new TQuery());
        }

        public Task<TResult> QueryAsync<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>
        {
            if (this.delQueryHandlers.ContainsKey(typeof(TQuery)))
            {
                return (Task<TResult>)this.delQueryHandlers[typeof(TQuery)].DynamicInvoke(query);
            }

            if (!this.asyncQueryHandlers.ContainsKey(typeof(TQuery)))
            {
                Log.Information($"Querying query ({typeof(TQuery).Name}) asynchronously without a handler being registered yet..");
                var fq = new AsyncQueryContext<TResult>(query);
                this.waitingAsyncQueries.Add(typeof(TQuery), fq);
                return fq.ResultTask;
            }

            var handler = this.asyncQueryHandlers[typeof(TQuery)];
            return ((IAsyncQueryHandler<TQuery, TResult>)handler).HandleQueryAsync(query);
        }

        public Task ExecuteAsync<TCommand>(TCommand command) where TCommand : ICommand
        {
            var handler = this.asyncCommandHandlers[typeof(TCommand)];
            return ((IAsyncCommandHandler<TCommand>)handler).HandleCommandAsync(command);
        }

        public void Execute<TCommand>(TCommand command) where TCommand : ICommand
        {
            var handler = this.commandHandlers[typeof(TCommand)];
            ((ICommandHandler<TCommand>)handler).HandleCommand(command);
        }

        public async Task PublishAsync<TEvent>(TEvent evt) where TEvent : IEvent
        {
            this.latestEvents[typeof(TEvent)] = evt;

            if (this.delEventHandlers.ContainsKey(typeof(TEvent)))
            {
                await ((Task)this.delEventHandlers[typeof(TEvent)].DynamicInvoke(evt)).ConfigureAwait(false);
            }

            if (!this.eventHandlers.ContainsKey(evt.GetType()))
                return;

            var handlerList = this.eventHandlers[evt.GetType()];
            await Task.WhenAll(handlerList.Select(handler => ((IEventHandler<TEvent>)handler).HandleEventAsync(evt))).ConfigureAwait(false);
        }


        private interface IAsyncQueryContext
        {
            object Query { get; }
            void Fulfill(object result);
        }

        private class AsyncQueryContext<TResult> : IAsyncQueryContext
        {
            private readonly TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();

            public Task<TResult> ResultTask => this.tcs.Task;
            public object Query { get; }

            public AsyncQueryContext(object query)
            {
                this.Query = query;
            }
            public void Fulfill(object result)
            {
                this.tcs.TrySetResult((TResult)result);
            }
        }
    }
}
