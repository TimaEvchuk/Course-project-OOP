using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace Plantify.ViewModels
{
    public partial class NotificationItemViewModel : BaseViewModel
    {
        private readonly Action<NotificationItemViewModel> _dismissAction;

        public string Text { get; }
        public DateTime Timestamp { get; }

        public NotificationItemViewModel(string text, Action<NotificationItemViewModel> dismissAction)
        {
            Text = text;
            Timestamp = DateTime.Now;
            _dismissAction = dismissAction;
        }

        [RelayCommand]
        private void Dismiss()
        {
            _dismissAction?.Invoke(this);
        }
    }
}
