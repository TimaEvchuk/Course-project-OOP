using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Messages;

namespace Plantify.ViewModels
{
    public partial class PlantDetailViewModel : BaseViewModel
    {
        public PlantViewModel Plant { get; }

        private readonly IMessenger _messenger;

        public PlantDetailViewModel(PlantViewModel plant, IMessenger messenger)
        {
            Plant = plant;
            _messenger = messenger;
        }

        [RelayCommand]
        private void Close()
        {
            _messenger.Send(new CloseOverlayMessage());
        }

        [RelayCommand]
        private void AddPlantToGarden()
        {
            _messenger.Send(new ShowAddUserPlantOverlayMessage(Plant.PlantModel));
        }
    }
}
