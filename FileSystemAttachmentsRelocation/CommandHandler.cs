using System;
using System.Windows.Input;

namespace FileSystemAttachmentsRelocation
{
    public class CommandHandler : ICommand
    {
        private readonly Action _action;
        public CommandHandler(Action action)
        {
            _action = action;
        }

        public void Execute(object parameter)
        {
            _action();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}