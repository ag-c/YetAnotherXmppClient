using System;
using System.Threading.Tasks;

namespace YetAnotherXmppClient.Infrastructure
{
    internal class ActionEventHandler<TEvent> : IEventHandler<TEvent> where TEvent : IEvent
    {
        private readonly Func<TEvent, Task> taskFunc;
        private readonly Action<TEvent> action;

        public ActionEventHandler(Action<TEvent> action)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public ActionEventHandler(Func<TEvent, Task> taskFunc)
        {
            this.taskFunc = taskFunc ?? throw new ArgumentNullException(nameof(taskFunc));
        }

        Task IEventHandler<TEvent>.HandleEventAsync(TEvent evt)
        {
            return this.action != null ? Task.Run(() => this.action(evt)) : this.taskFunc(evt);
        }
    }
}
