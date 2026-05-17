using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Plantify.Data;
using Plantify.Dialogs;
using Plantify.Messages;
using Plantify.Models;
using Plantify.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Plantify.ViewModels
{
    public partial class PlantManagementViewModel : BaseViewModel, IRecipient<AdminUserListChangedMessage>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessenger _messenger;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private ObservableCollection<Plant> _plants;

        [ObservableProperty]
        private ObservableCollection<PlantSubmission> _pendingSubmissions;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UpdatePlantCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeletePlantCommand))]
        private Plant? _selectedPlant;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ApproveSubmissionCommand))]
        [NotifyCanExecuteChangedFor(nameof(RejectSubmissionCommand))]
        private PlantSubmission? _selectedSubmission;

        public PlantManagementViewModel(IUnitOfWork unitOfWork, IMessenger messenger, IDialogService dialogService)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
            _dialogService = dialogService;
            _plants = new ObservableCollection<Plant>();
            _pendingSubmissions = new ObservableCollection<PlantSubmission>();
            
            _messenger.Register<AdminUserListChangedMessage>(this);
        }

        [RelayCommand]
        private async Task LoadData()
        {
            await LoadPlants();
            await LoadSubmissions();
        }

        [RelayCommand]
        private async Task LoadPlants()
        {
            var plantList = await _unitOfWork.Plants.GetAllAsync(include: q => q.Include(p => p.Sections));
            Plants.Clear();
            foreach (var plant in plantList)
            {
                Plants.Add(plant);
            }
        }

        [RelayCommand]
        private async Task LoadSubmissions()
        {
            var submissionList = await _unitOfWork.PlantSubmissions.GetAllAsync(include: q => q.Include(s => s.SubmittedByUser));
            PendingSubmissions.Clear();
            foreach (var submission in submissionList)
            {
                PendingSubmissions.Add(submission);
            }
        }
        
        [RelayCommand]
        private void AddPlant()
        {
            _messenger.Send(new ShowAddEditPlantOverlayMessage(null));
        }

        private bool CanUpdateOrDelete() => SelectedPlant != null;

        [RelayCommand(CanExecute = nameof(CanUpdateOrDelete))]
        private void UpdatePlant()
        {
            if (SelectedPlant == null) return;
            _messenger.Send(new ShowAddEditPlantOverlayMessage(SelectedPlant));
        }

        [RelayCommand(CanExecute = nameof(CanUpdateOrDelete))]
        private async Task DeletePlant()
        {
            if (SelectedPlant == null) return;

            var result = _dialogService.ShowConfirmationDialog($"Вы уверены, что хотите удалить '{SelectedPlant.Name}' из каталога? Это действие необратимо.");

            if (result.Confirmed)
            {
                _unitOfWork.Plants.Delete(SelectedPlant);
                await _unitOfWork.CompleteAsync();
                SelectedPlant = null; // Hide details panel
                await LoadPlants();
            }
        }

        private bool CanManipulateSubmission() => SelectedSubmission != null;

        [RelayCommand(CanExecute = nameof(CanManipulateSubmission))]
        private async Task ApproveSubmission()
        {
            if (SelectedSubmission == null) return;

            var newPlant = new Plant
            {
                Name = SelectedSubmission.Name,
                Variety = SelectedSubmission.Variety,
                LightRequirement = SelectedSubmission.LightRequirement,
                WateringInterval = SelectedSubmission.WateringInterval,
                FertilizingInterval = SelectedSubmission.FertilizingInterval,
                ImagePath = SelectedSubmission.ImagePath,
            };

            if (!string.IsNullOrWhiteSpace(SelectedSubmission.Description))
            {
                newPlant.Sections.Add(new PlantSection { Title = "Описание", Content = SelectedSubmission.Description });
            }

            await _unitOfWork.Plants.AddAsync(newPlant);
            _unitOfWork.PlantSubmissions.Delete(SelectedSubmission);
            await _unitOfWork.CompleteAsync();

            await LoadData();
        }

        [RelayCommand(CanExecute = nameof(CanManipulateSubmission))]
        private async Task RejectSubmission()
        {
            if (SelectedSubmission == null) return;
            
            _unitOfWork.PlantSubmissions.Delete(SelectedSubmission);
            await _unitOfWork.CompleteAsync();

            await LoadSubmissions();
        }
        
        public void Receive(AdminUserListChangedMessage message)
        {
            // This message is now used as a generic "refresh plant data" signal
            _ = LoadData();
        }
    }
}
