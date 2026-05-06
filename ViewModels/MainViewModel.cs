using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Messages;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Plantify.ViewModels
{
    public partial class MainViewModel : BaseViewModel, IRecipient<NavigateMessage>
    {
        [ObservableProperty]
        private BaseViewModel _currentViewModel;

        private readonly IServiceProvider _serviceProvider;

        public MainViewModel(IServiceProvider serviceProvider, IMessenger messenger)
        {
            _serviceProvider = serviceProvider;
            
            // Set the initial view model
            _currentViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();

            // Register to receive navigation messages
            messenger.RegisterAll(this);
        }

        public void Receive(NavigateMessage message)
        {
            // When a navigation message is received, resolve the requested view model 
            // from the DI container and set it as the current view model.
            if (message.Value != null)
            {
                var viewModel = (BaseViewModel)_serviceProvider.GetRequiredService(message.Value);
                CurrentViewModel = viewModel;
            }
        }
    }
}
