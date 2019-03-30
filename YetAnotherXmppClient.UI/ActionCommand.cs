using System;
using System.Windows.Input;

namespace YetAnotherXmppClient.UI
{
    internal class ActionCommand : ICommand
    {
        private readonly Func<object, bool> canExecuteHandler;
        private readonly Action<object> executeHandler;

        public ActionCommand(Action<object> execute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            this.executeHandler = execute;
        }

        public ActionCommand(Action<object> execute, Func<object, bool> canExecute)
            : this(execute)
        {
            this.canExecuteHandler = canExecute;
        }

        public event EventHandler CanExecuteChanged;
        //{
        //    add { CommandManager.RequerySuggested += value; }
        //    remove { CommandManager.RequerySuggested -= value; }
        //}

        public void Execute(object parameter)
        {
            this.executeHandler(parameter);
        }

        public bool CanExecute(object parameter)
        {
            if (this.canExecuteHandler == null)
            {
                return true;
            }

            return this.canExecuteHandler(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            //CommandManager.InvalidateRequerySuggested();
        }
    }
}
