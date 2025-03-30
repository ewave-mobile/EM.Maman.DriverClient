using EM.Maman.Models.Enums;
using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.CustomModels
{
    public class TaskDetails : INotifyPropertyChanged
    {
        private int _id;
        private string _code;
        private string _name;
        private string _description;
        private Enums.TaskType _taskType;
        private Enums.TaskStatus _status;
        private DateTime _createdDateTime;
        private DateTime? _executedDateTime;
        private int? _sourceFingerPosition;
        private int? _destinationFingerPosition;
        private Finger _sourceFinger;
        private Finger _destinationFinger;
        private Pallet _pallet;
        private Cell _destinationCell;
        private int _userId;
        private string _operatorNotes;
        private bool _isPriority;
        private bool _isUploaded;
        private Cell _sourceCell;

        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Code
        {
            get => _code;
            set
            {
                if (_code != value)
                {
                    _code = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        public Enums.TaskType TaskType
        {
            get => _taskType;
            set
            {
                if (_taskType != value)
                {
                    _taskType = value;
                    OnPropertyChanged();
                }
            }
        }

        public Enums.TaskStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime CreatedDateTime
        {
            get => _createdDateTime;
            set
            {
                if (_createdDateTime != value)
                {
                    _createdDateTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? ExecutedDateTime
        {
            get => _executedDateTime;
            set
            {
                if (_executedDateTime != value)
                {
                    _executedDateTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public int? SourceFingerPosition
        {
            get => _sourceFingerPosition;
            set
            {
                if (_sourceFingerPosition != value)
                {
                    _sourceFingerPosition = value;
                    OnPropertyChanged();
                }
            }
        }

        public int? DestinationFingerPosition
        {
            get => _destinationFingerPosition;
            set
            {
                if (_destinationFingerPosition != value)
                {
                    _destinationFingerPosition = value;
                    OnPropertyChanged();
                }
            }
        }

        public Finger SourceFinger
        {
            get => _sourceFinger;
            set
            {
                if (_sourceFinger != value)
                {
                    _sourceFinger = value;
                    if (value != null)
                    {
                        SourceFingerPosition = value.Position;
                    }
                    OnPropertyChanged();
                }
            }
        }

        public Finger DestinationFinger
        {
            get => _destinationFinger;
            set
            {
                if (_destinationFinger != value)
                {
                    _destinationFinger = value;
                    if (value != null)
                    {
                        DestinationFingerPosition = value.Position;
                    }
                    OnPropertyChanged();
                }
            }
        }

        public Pallet Pallet
        {
            get => _pallet;
            set
            {
                if (_pallet != value)
                {
                    _pallet = value;
                    OnPropertyChanged();
                }
            }
        }

        public Cell DestinationCell
        {
            get => _destinationCell;
            set
            {
                if (_destinationCell != value)
                {
                    _destinationCell = value;
                    OnPropertyChanged();
                }
            }
        }

        public int UserId
        {
            get => _userId;
            set
            {
                if (_userId != value)
                {
                    _userId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string OperatorNotes
        {
            get => _operatorNotes;
            set
            {
                if (_operatorNotes != value)
                {
                    _operatorNotes = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsPriority
        {
            get => _isPriority;
            set
            {
                if (_isPriority != value)
                {
                    _isPriority = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsUploaded
        {
            get => _isUploaded;
            set
            {
                if (_isUploaded != value)
                {
                    _isUploaded = value;
                    OnPropertyChanged();
                }
            }
        }

        // Helper properties
        public bool IsImportTask => TaskType == Enums.TaskType.Import;
        public bool IsExportTask => TaskType == Enums.TaskType.Export;
        public bool IsInProgress => Status == Enums.TaskStatus.InProgress;
        public bool IsCompleted => Status == Enums.TaskStatus.Completed;
        public bool CanStart => Status == Enums.TaskStatus.Created;
        //public bool NeedsSourceNavigation => IsImportTask && SourceFinger != null;
        public bool NeedsSourceNavigation =>true;
        //public bool NeedsDestinationNavigation => (IsImportTask && DestinationCell != null) ||
        //                                        (IsExportTask && DestinationFinger != null);
        public bool NeedsDestinationNavigation => true;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public Cell SourceCell
        {
            get => _sourceCell;
            set
            {
                if (_sourceCell != value)
                {
                    _sourceCell = value;
                    OnPropertyChanged();
                }
            }
        }
        // Conversion methods to/from database model
        public static TaskDetails FromDbModel(EM.Maman.Models.LocalDbModels.Task task, Pallet pallet = null,
                                            Finger sourceFinger = null, Finger destFinger = null, Cell destCell = null)
        {
            if (task == null) return null;

            var details = new TaskDetails
            {
                Id = task.Id,
                Code = task.Code,
                Name = task.Name,
                Description = task.Description,
                TaskType = task.TaskTypeId == 1 ? Enums.TaskType.Import : Enums.TaskType.Export,
                Status = task.IsExecuted == true ? Enums.TaskStatus.Completed : Enums.TaskStatus.Created,
                CreatedDateTime = task.DownloadDate ?? DateTime.Now,
                ExecutedDateTime = task.ExecutedDate,
                SourceFinger = sourceFinger,
                DestinationFinger = destFinger,
                Pallet = pallet,
                DestinationCell = destCell,
                IsUploaded = task.IsUploaded ?? false
            };

            return details;
        }

        public EM.Maman.Models.LocalDbModels.Task ToDbModel()
        {
            var task = new EM.Maman.Models.LocalDbModels.Task
            {
                Id = Id > 0 ? Id : 0,
                Code = Code,
                Name = Name,
                Description = Description,
                TaskTypeId = (int)TaskType,
                IsExecuted = Status == Enums.TaskStatus.Completed,
                DownloadDate = CreatedDateTime,
                ExecutedDate = ExecutedDateTime,
                FingerLocationId = SourceFinger?.Id,
                CellEndLocationId = DestinationCell?.Id,
                PalletId = Pallet?.Id.ToString(),
                IsUploaded = IsUploaded
            };

            return task;
        }
    }
}
