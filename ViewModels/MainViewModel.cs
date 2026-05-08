using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Messages;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Plantify.ViewModels
{
    public partial class MainViewModel : BaseViewModel, IRecipient<NavigateMessage>
    {
        [ObservableProperty]
        private BaseViewModel _currentViewModel = null!;

        [ObservableProperty]
        private string _currentPageTitle = "";

        private readonly IServiceProvider _serviceProvider;

        public MainViewModel(IServiceProvider serviceProvider, IMessenger messenger)
        {
            _serviceProvider = serviceProvider;
            
            // Register to receive navigation messages
            messenger.RegisterAll(this);

            // Set the initial view model
            Navigate(typeof(EncyclopediaViewModel), "Энциклопедия");
        }

        private void Navigate(Type viewModelType, string pageTitle)
        {
            CurrentViewModel = (BaseViewModel)_serviceProvider.GetRequiredService(viewModelType);
            CurrentPageTitle = pageTitle;
        }

        public void Receive(NavigateMessage message)
        {
            if (message.Value == typeof(EncyclopediaViewModel))
            {
                Navigate(typeof(EncyclopediaViewModel), "Энциклопедия");
            }
            else if (message.Value == typeof(PlantManagementViewModel))
            {
                Navigate(typeof(PlantManagementViewModel), "Управление каталогом");
            }
            // Add other cases as needed
        }

        [RelayCommand]
        private void GoToMyGarden() => Navigate(typeof(MyGardenViewModel), "Мой сад");

        [RelayCommand]
        private void GoToEncyclopedia() => Navigate(typeof(EncyclopediaViewModel), "Энциклопедия");

        [RelayCommand]
        private void GoToSchedule() => Navigate(typeof(ScheduleViewModel), "Расписание");
        
        [RelayCommand]
        private void GoToSettings() => Navigate(typeof(PlantManagementViewModel), "Управление каталогом");
    }
}
