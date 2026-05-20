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
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Plantify.ViewModels
{
    public partial class PlantManagementViewModel : BaseViewModel, IRecipient<AdminUserListChangedMessage>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessenger _messenger;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private ObservableCollection<PlantViewModel> _plants;

        [ObservableProperty]
        private ObservableCollection<PlantSubmission> _pendingSubmissions;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UpdatePlantCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeletePlantCommand))]
        private PlantViewModel? _selectedPlant;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ApproveSubmissionCommand))]
        [NotifyCanExecuteChangedFor(nameof(RejectSubmissionCommand))]
        private PlantSubmission? _selectedSubmission;
        
        [ObservableProperty]
        private ObservableCollection<Variety> _varieties;

        [ObservableProperty]
        private ObservableCollection<LightRequirement> _lightRequirements;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UpdateVarietyCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteVarietyCommand))]
        private Variety? _selectedVariety;
        
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UpdateLightRequirementCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteLightRequirementCommand))]
        private LightRequirement? _selectedLightRequirement;

        public PlantManagementViewModel(IUnitOfWork unitOfWork, IMessenger messenger, IDialogService dialogService)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
            _dialogService = dialogService;
            _plants = new ObservableCollection<PlantViewModel>();
            _pendingSubmissions = new ObservableCollection<PlantSubmission>();
            _varieties = new ObservableCollection<Variety>();
            _lightRequirements = new ObservableCollection<LightRequirement>();
            
            _messenger.Register<AdminUserListChangedMessage>(this);
        }

        [RelayCommand]
        private async Task LoadData()
        {
            await LoadPlants();
            await LoadSubmissions();
            await LoadCategories();
        }

        [RelayCommand]
        private async Task LoadPlants()
        {
            var plantList = await _unitOfWork.Plants.GetAllAsync(
                include: q => q.Include(p => p.Sections).Include(p => p.Variety).Include(p => p.LightRequirement),
                withTracking: false);
            Plants.Clear();
            foreach (var plant in plantList)
            {
                Plants.Add(new PlantViewModel(plant));
            }
        }

        [RelayCommand]
        private async Task LoadSubmissions()
        {
            var submissionList = await _unitOfWork.PlantSubmissions.GetAllAsync(include: q => q.Include(s => s.SubmittedByUser).Include(s => s.Variety).Include(s => s.LightRequirement));
            PendingSubmissions.Clear();
            foreach (var submission in submissionList)
            {
                PendingSubmissions.Add(submission);
            }
        }

        [RelayCommand]
        private async Task LoadCategories()
        {
            var varietyList = await _unitOfWork.Varieties.GetAllAsync(withTracking: false);
            Varieties.Clear();
            foreach (var item in varietyList.OrderBy(v => v.Name))
            {
                Varieties.Add(item);
            }

            var lightList = await _unitOfWork.LightRequirements.GetAllAsync(withTracking: false);
            LightRequirements.Clear();
            foreach (var item in lightList.OrderBy(l => l.Name))
            {
                LightRequirements.Add(item);
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
            _messenger.Send(new ShowAddEditPlantOverlayMessage(SelectedPlant.PlantModel));
        }

        [RelayCommand(CanExecute = nameof(CanUpdateOrDelete))]
        private async Task DeletePlant()
        {
            if (SelectedPlant == null) return;

            var result = _dialogService.ShowConfirmationDialog($"Вы уверены, что хотите удалить '{SelectedPlant.Name}' из каталога? Это действие необратимо.");

            if (result.Confirmed)
            {
                // Сначала находим "свежую" версию из БД, потом удаляем
                var plantToDelete = await _unitOfWork.Plants.GetByIdAsync(SelectedPlant.PlantModel.Id);
                if (plantToDelete != null)
                {
                    _unitOfWork.Plants.Delete(plantToDelete);
                    await _unitOfWork.CompleteAsync();
                }

                SelectedPlant = null; // Скрываем панель деталей
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
                VarietyId = SelectedSubmission.VarietyId,
                LightRequirementId = SelectedSubmission.LightRequirementId,
                WateringInterval = SelectedSubmission.WateringInterval,
                FertilizingInterval = SelectedSubmission.FertilizingInterval,
                ImagePath = SelectedSubmission.ImagePath,
            };

            if (!string.IsNullOrWhiteSpace(SelectedSubmission.Description))
            {
                try
                {
                    var sections = JsonSerializer.Deserialize<List<PlantSectionViewModel>>(SelectedSubmission.Description);
                    if (sections != null)
                    {
                        foreach (var section in sections)
                        {
                            newPlant.Sections.Add(new PlantSection { Title = section.Title, Content = section.Content });
                        }
                    }
                }
                catch 
                { 
                    newPlant.Sections.Add(new PlantSection { Title = "Описание", Content = SelectedSubmission.Description });
                }
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
        
        private bool CanUpdateOrDeleteVariety() => SelectedVariety != null;
        private bool CanUpdateOrDeleteLightRequirement() => SelectedLightRequirement != null;

        [RelayCommand]
        private async Task AddVariety()
        {
            var result = _dialogService.ShowInputDialog("Введите название нового вида:", "Добавить вид");
            if (result.Confirmed && !string.IsNullOrWhiteSpace(result.Text) && result.Text.Trim().Length >= 2)
            {
                var newVariety = new Variety { Name = result.Text.Trim() };
                await _unitOfWork.Varieties.AddAsync(newVariety);
                await _unitOfWork.CompleteAsync();
                await LoadCategories();
            }
        }

        [RelayCommand(CanExecute = nameof(CanUpdateOrDeleteVariety))]
        private async Task UpdateVariety()
        {
            if (SelectedVariety == null) return;
            var result = _dialogService.ShowInputDialog("Введите новое название для вида:", "Редактировать вид", SelectedVariety.Name);
            if (result.Confirmed && !string.IsNullOrWhiteSpace(result.Text))
            {
                SelectedVariety.Name = result.Text.Trim();
                _unitOfWork.Varieties.Update(SelectedVariety);
                await _unitOfWork.CompleteAsync();
                await LoadCategories();
            }
        }

        [RelayCommand(CanExecute = nameof(CanUpdateOrDeleteVariety))]
        private async Task DeleteVariety()
        {
            if (SelectedVariety == null) return;
            var result = _dialogService.ShowConfirmationDialog($"Вы уверены, что хотите удалить вид '{SelectedVariety.Name}'?");
            if (result.Confirmed)
            {
                _unitOfWork.Varieties.Delete(SelectedVariety);
                await _unitOfWork.CompleteAsync();
                await LoadCategories();
            }
        }

        [RelayCommand]
        private async Task AddLightRequirement()
        {
            var result = _dialogService.ShowInputDialog("Введите новое требование к свету:", "Добавить требование");
            if (result.Confirmed && !string.IsNullOrWhiteSpace(result.Text) && result.Text.Trim().Length >= 2)
            {
                var newReq = new LightRequirement { Name = result.Text.Trim() };
                await _unitOfWork.LightRequirements.AddAsync(newReq);
                await _unitOfWork.CompleteAsync();
                await LoadCategories();
            }
        }

        [RelayCommand(CanExecute = nameof(CanUpdateOrDeleteLightRequirement))]
        private async Task UpdateLightRequirement()
        {
            if (SelectedLightRequirement == null) return;
            var result = _dialogService.ShowInputDialog("Введите новое название для требования:", "Редактировать требование", SelectedLightRequirement.Name);
            if (result.Confirmed && !string.IsNullOrWhiteSpace(result.Text))
            {
                SelectedLightRequirement.Name = result.Text.Trim();
                _unitOfWork.LightRequirements.Update(SelectedLightRequirement);
                await _unitOfWork.CompleteAsync();
                await LoadCategories();
            }
        }
        
        [RelayCommand(CanExecute = nameof(CanUpdateOrDeleteLightRequirement))]
        private async Task DeleteLightRequirement()
        {
            if (SelectedLightRequirement == null) return;
            var result = _dialogService.ShowConfirmationDialog($"Вы уверены, что хотите удалить требование '{SelectedLightRequirement.Name}'?");
            if (result.Confirmed)
            {
                _unitOfWork.LightRequirements.Delete(SelectedLightRequirement);
                await _unitOfWork.CompleteAsync();
                await LoadCategories();
            }
        }

        public void Receive(AdminUserListChangedMessage message)
        {
            _ = LoadData();
        }
    }
}
