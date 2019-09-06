using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace YetAnotherXmppClient.Infrastructure
{
    public interface IEvent { }

    public interface IQuery<TResult> { }


    public interface IEventHandler { }
    public interface IEventHandler<TEvent> : IEventHandler where TEvent : IEvent
    {
        Task HandleEventAsync(TEvent evt);
    }

    public interface IQueryHandler { }
    public interface IQueryHandler<TQuery, TResult> : IQueryHandler
    {
        Task<TResult> HandleQueryAsync(TQuery query);
    }

    public interface IMediator
    {
        void RegisterHandler<TEvent>(IEventHandler<TEvent> handler, bool publishLatestEventToNewHandler = false) where TEvent : IEvent;
        void RegisterHandler<TQuery, TResult>(IQueryHandler<TQuery, TResult> handler) where TQuery : IQuery<TResult>;
        void RegisterHandler<TQuery, TResult>(Expression<Func<TQuery,Task<TResult>>> handler) where TQuery : IQuery<TResult>;
        void RegisterHandler<TEvent>(Expression<Func<TEvent, Task>> handler) where TEvent : IEvent;

        Task PublishAsync<TEvent>(TEvent evt) where TEvent : IEvent;
        Task<TResult> QueryAsync<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>;
    }

    public class Mediator : IMediator
    {
        private readonly Dictionary<Type, object> latestEvents = new Dictionary<Type, object>();
        private readonly Dictionary<Type, List<IEventHandler>> eventHandlers = new Dictionary<Type, List<IEventHandler>>();
        private readonly Dictionary<Type, IQueryHandler> queryHandlers = new Dictionary<Type, IQueryHandler>();
        private readonly Dictionary<Type, Delegate> delQueryHandlers = new Dictionary<Type, Delegate>();
        private readonly Dictionary<Type, Delegate> delEventHandlers = new Dictionary<Type, Delegate>();

        public void RegisterHandler<TEvent>(IEventHandler<TEvent> handler, bool publishLatestEventToNewHandler = false) where TEvent : IEvent
        {
            if(!this.eventHandlers.ContainsKey(typeof(TEvent)))
                this.eventHandlers.Add(typeof(TEvent), new List<IEventHandler>());

            this.eventHandlers[typeof(TEvent)].Add(handler);

            if (this.latestEvents.ContainsKey(typeof(TEvent)))
                handler.HandleEventAsync((TEvent)this.latestEvents[typeof(TEvent)]).Wait(); //UNDONE!
        }

        public void RegisterHandler<TQuery, TResult>(IQueryHandler<TQuery, TResult> handler) where TQuery : IQuery<TResult>
        {
            this.queryHandlers.Add(typeof(TQuery), handler);
        }

        public void RegisterHandler<TQuery, TResult>(Expression<Func<TQuery, Task<TResult>>> handler) where TQuery : IQuery<TResult>
        {
            this.delQueryHandlers.Add(typeof(TQuery), ((LambdaExpression)handler).Compile(false));
        }

        public void RegisterHandler<TEvent>(Expression<Func<TEvent, Task>> handler) where TEvent : IEvent
        {
            this.delEventHandlers.Add(typeof(TEvent), ((LambdaExpression)handler).Compile(false));
        }

        public Task<TResult> QueryAsync<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>
        {
            if (this.delQueryHandlers.ContainsKey(typeof(TQuery)))
            {
                return (Task<TResult>)this.delQueryHandlers[typeof(TQuery)].DynamicInvoke(query);
            }

            var handler = this.queryHandlers[typeof(TQuery)];
            return ((IQueryHandler<TQuery, TResult>)handler).HandleQueryAsync(query);
        }

        public async Task PublishAsync<TEvent>(TEvent evt) where TEvent : IEvent
        {
            this.latestEvents.Add(typeof(TEvent), evt);

            if (this.delEventHandlers.ContainsKey(typeof(TEvent)))
            {
                await (Task)this.delEventHandlers[typeof(TEvent)].DynamicInvoke(evt);
            }

            if (!this.eventHandlers.ContainsKey(evt.GetType()))
                return;

            var handlerList = this.eventHandlers[evt.GetType()];
            await Task.WhenAll(handlerList.Select(handler => ((IEventHandler<TEvent>)handler).HandleEventAsync(evt)));
        }
    }
}
