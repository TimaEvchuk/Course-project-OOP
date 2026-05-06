using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Messages;

namespace Plantify.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly IMessenger _messenger;
        public string WelcomeMessage { get; } = "Welcome to your Plantify Dashboard!";

        public DashboardViewModel(IMessenger messenger)
        {
            _messenger = messenger;
        }

        [RelayCommand]
        private void GoToEncyclopedia()
        {
            _messenger.Send(new NavigateMessage(typeof(EncyclopediaViewModel)));
        }

        [RelayCommand]
        private void GoToPlantManagement()
        {
            _messenger.Send(new NavigateMessage(typeof(PlantManagementViewModel)));
        }
    }
}
