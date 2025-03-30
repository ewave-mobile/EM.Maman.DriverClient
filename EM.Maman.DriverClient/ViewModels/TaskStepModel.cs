using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.DriverClient.ViewModels
{
    public class TaskStepModel : INotifyPropertyChanged
    {
        private int _stepNumber;
        private string _stepName;
        private bool _isCurrentStep;
        private bool _isCompleted;
        private bool _isLastStep;

        public int StepNumber
        {
            get => _stepNumber;
            set
            {
                if (_stepNumber != value)
                {
                    _stepNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StepName
        {
            get => _stepName;
            set
            {
                if (_stepName != value)
                {
                    _stepName = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCurrentStep
        {
            get => _isCurrentStep;
            set
            {
                if (_isCurrentStep != value)
                {
                    _isCurrentStep = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLastStep
        {
            get => _isLastStep;
            set
            {
                if (_isLastStep != value)
                {
                    _isLastStep = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
