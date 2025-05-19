using System;
using System.Linq;
using System.Windows;
using EM.Maman.Models.Enums;
using Microsoft.Extensions.Logging;

namespace EM.Maman.DriverClient.ViewModels
{
    public partial class MainViewModel
    {
        private async void OnPositionChanged(object sender, int positionValue)
        {
            CheckForArrivalAtDestination(positionValue);

            int level = positionValue / 100;
            int position = positionValue % 100;

            // Update current cell level and position for MainViewModel
            _currentCellLevel = level;
            _currentCellPosition = position;

            if (CurrentTrolley != null)
            {
                CurrentTrolley.Position = position;
                OnPropertyChanged(nameof(CurrentTrolley));
            }

            if (TrolleyVM != null)
            {
                TrolleyVM.UpdateTrolleyPosition(level, position);
                // Ensure selected level always matches current physical level after a position change
                if (TrolleyVM.SelectedLevelNumber != level)
                {
                    _logger.LogInformation($"Physical trolley level ({level}) and selected display level ({TrolleyVM.SelectedLevelNumber}) differ on position change. Syncing display level.");
                    TrolleyVM.SelectedLevelNumber = level;
                }
            }

            if (WarehouseVM != null)
            {
                WarehouseVM.CurrentLevelNumber = level;
            }

            bool isFinger = await IsFingerLocationAsync(positionValue);

            if (isFinger)
            {
                if (_currentFingerPositionValue != positionValue)
                {
                    _logger.LogInformation("Arrived at finger location (PositionValue: {PositionValue}).", positionValue);
                    _currentFingerPositionValue = positionValue;
                    await LoadPalletsForFingerAuthenticationAsync(positionValue);
                }

                // Create a new UnitOfWork instance for this operation
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var finger = (await unitOfWork.Fingers.FindAsync(f => f.Position == positionValue)).FirstOrDefault();
                    if (finger != null)
                    {
                        CurrentFingerDisplayName = finger.DisplayName;
                    }
                }
            }
            else
            {
                if (_currentFingerPositionValue != null)
                {
                    _logger.LogInformation("Left finger location.");
                    _currentFingerPositionValue = null;
                    IsFingerAuthenticationViewActive = false;
                    _dispatcherService.Invoke(() =>
                    {
                        PalletsToAuthenticate.Clear();
                    });
                }
            }
            
            // Notify that task panel visibility properties might have changed
            OnPropertyChanged(nameof(ShouldShowTasksPanel));
            OnPropertyChanged(nameof(ShouldShowDefaultPhoto));
        }

     

      
    }
}
