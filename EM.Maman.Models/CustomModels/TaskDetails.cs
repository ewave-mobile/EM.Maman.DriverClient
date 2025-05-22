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
        private Enums.ActiveTaskStatus _activeTaskStatus;
        private DateTime _createdDateTime;
        private DateTime? _executedDateTime;
        private int? _sourceFingerPosition;
        private int? _destinationFingerPosition;
        private Finger _sourceFinger;
        private Finger _destinationFinger;
        private Pallet _pallet;
        private Cell _destinationCell;
        // public int? SourceFingerId { get; set; } // Potentially redundant with new DB structure
        // public long? DestinationCellId { get; set; } // Potentially redundant with new DB structure
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
                    // OnPropertyChanged(nameof(IsImportTask)); // No longer dependent on TaskType
                    // OnPropertyChanged(nameof(IsExportTask)); // No longer dependent on TaskType
                    OnPropertyChanged(nameof(DisplayDetail1));
                    OnPropertyChanged(nameof(DisplayDetail2));
                    OnPropertyChanged(nameof(DisplayDetail3));
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
        public Enums.ActiveTaskStatus ActiveTaskStatus
        {
            get => _activeTaskStatus;
            set
            {
                if (_activeTaskStatus != value)
                {
                    _activeTaskStatus = value;
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
                    OnPropertyChanged(nameof(IsImportTask)); // Now dependent on Pallet.UpdateType
                    OnPropertyChanged(nameof(IsExportTask)); // Now dependent on Pallet.UpdateType
                    OnPropertyChanged(nameof(DisplayDetail1));
                    OnPropertyChanged(nameof(DisplayDetail2));
                    OnPropertyChanged(nameof(DisplayDetail3));
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
        public bool IsImportTask => Pallet != null && Pallet.UpdateType == Enums.UpdateType.Import;
        public bool IsExportTask => Pallet != null && Pallet.UpdateType == Enums.UpdateType.Export;
        public bool IsInProgress => Status == Enums.TaskStatus.InProgress;
        public bool IsCompleted => Status == Enums.TaskStatus.Completed;
        public bool CanStart => Status == Enums.TaskStatus.Created;
        //public bool NeedsSourceNavigation => IsImportTask && SourceFinger != null;
        public bool NeedsSourceNavigation =>true;
        //public bool NeedsDestinationNavigation => (IsImportTask && DestinationCell != null) ||
        //                                        (IsExportTask && DestinationFinger != null);
        public bool NeedsDestinationNavigation => true;

        // New display properties
        public string DisplayDetail1
        {
            get
            {
                if (Pallet == null) return "-";
                // Assuming IsExportTask uses Export fields, else Import fields
                return IsExportTask ? Pallet.ExportBarcode : Pallet.ImportUnit;
            }
        }

        public string DisplayDetail2
        {
            get
            {
                if (Pallet == null) return "-";
                return IsExportTask ? Pallet.ExportAwbAppearance : Pallet.ImportAppearance;
            }
        }

        public string DisplayDetail3
        {
            get
            {
                if (Pallet == null) return "-";
                return IsExportTask ? Pallet.ExportSwbPrefix : Pallet.ImportManifest;
            }
        }

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
        public static TaskDetails FromDbModel(EM.Maman.Models.LocalDbModels.Task dbTask)
        {
            if (dbTask == null) return null;

            var details = new TaskDetails
            {
                Id = dbTask.Id,
                Code = dbTask.Code,
                Name = dbTask.Name,
                Description = dbTask.Description,
                TaskType = (Enums.TaskType)dbTask.TaskTypeId, // Assumes TaskTypeId directly maps to enum int values
                Status = dbTask.Status.HasValue
                    ? (Enums.TaskStatus)dbTask.Status.Value
                    : (dbTask.IsExecuted == true ? Enums.TaskStatus.Completed : Enums.TaskStatus.Created),
                CreatedDateTime = dbTask.DownloadDate ?? DateTime.Now,
                ExecutedDateTime = dbTask.ExecutedDate,
                Pallet = null, // Pallet must be loaded separately and passed to an overloaded FromDbModel method or set afterwards
                IsUploaded = dbTask.IsUploaded ?? false,
                ActiveTaskStatus = dbTask.ActiveTaskStatus.HasValue
                    ? (Enums.ActiveTaskStatus)dbTask.ActiveTaskStatus.Value
                    : ( (Enums.TaskType)dbTask.TaskTypeId == Enums.TaskType.Retrieval ? Enums.ActiveTaskStatus.retrieval : Enums.ActiveTaskStatus.authentication ), // Default ActiveStatus
                UserId = 0, // TODO: Map UserId if available in dbTask
                OperatorNotes = "", // TODO: Map OperatorNotes if available in dbTask
                IsPriority = false // TODO: Map IsPriority if available in dbTask
            };

            if (details.TaskType == Enums.TaskType.Storage)
            {
                details.SourceFinger = dbTask.StorageSourceFinger;
                details.DestinationCell = dbTask.StorageDestinationCell;
            }
            else if (details.TaskType == Enums.TaskType.Retrieval)
            {
                details.SourceCell = dbTask.RetrievalSourceCell;
                details.DestinationFinger = dbTask.RetrievalDestinationFinger;
                // Also handle HND retrieval to cell if applicable
                if (dbTask.RetrievalDestinationCell != null)
                {
                    details.DestinationCell = dbTask.RetrievalDestinationCell;
                }
            }
            
            // Populate position properties from the main objects if they are set
            if (details.SourceFinger != null) details.SourceFingerPosition = details.SourceFinger.Position;
            if (details.DestinationFinger != null) details.DestinationFingerPosition = details.DestinationFinger.Position;
            // Note: DestinationCell does not have a 'Position' property in the same way Finger does.
            // SourceCell also does not have a 'Position' property in the same way Finger does.

            return details;
        }

        public static TaskDetails FromDbModel(EM.Maman.Models.LocalDbModels.Task dbTask, Pallet pallet, Finger sourceFinger, Cell sourceCell, Cell destinationCell, Finger destinationFinger = null)
        {
            if (dbTask == null) return null;

            var details = new TaskDetails
            {
                Id = dbTask.Id,
                Code = dbTask.Code,
                Name = dbTask.Name,
                Description = dbTask.Description,
                TaskType = (Enums.TaskType)dbTask.TaskTypeId,
                Status = dbTask.Status.HasValue
                    ? (Enums.TaskStatus)dbTask.Status.Value
                    : (dbTask.IsExecuted == true ? Enums.TaskStatus.Completed : Enums.TaskStatus.Created),
                CreatedDateTime = dbTask.DownloadDate ?? DateTime.Now,
                ExecutedDateTime = dbTask.ExecutedDate,
                Pallet = pallet, // Use passed pallet
                IsUploaded = dbTask.IsUploaded ?? false,
                ActiveTaskStatus = dbTask.ActiveTaskStatus.HasValue
                    ? (Enums.ActiveTaskStatus)dbTask.ActiveTaskStatus.Value
                    : ((Enums.TaskType)dbTask.TaskTypeId == Enums.TaskType.Retrieval ? Enums.ActiveTaskStatus.retrieval : Enums.ActiveTaskStatus.authentication),
                UserId = 0, // TODO: Map UserId
                OperatorNotes = "", // TODO: Map OperatorNotes
                IsPriority = false, // TODO: Map IsPriority

                SourceFinger = sourceFinger,
                SourceCell = sourceCell,
                DestinationFinger = destinationFinger,
                DestinationCell = destinationCell
            };
            
            // Populate position properties from the main objects if they are set
            if (details.SourceFinger != null) details.SourceFingerPosition = details.SourceFinger.Position;
            if (details.DestinationFinger != null) details.DestinationFingerPosition = details.DestinationFinger.Position;

            // If specific DB navigation properties are loaded and parameters were null, use them as fallback
            // This makes the method more robust if some callers don't have all related entities pre-loaded.
            if (details.TaskType == Enums.TaskType.Storage)
            {
                if (details.SourceFinger == null) details.SourceFinger = dbTask.StorageSourceFinger;
                if (details.DestinationCell == null) details.DestinationCell = dbTask.StorageDestinationCell;
            }
            else if (details.TaskType == Enums.TaskType.Retrieval)
            {
                if (details.SourceCell == null) details.SourceCell = dbTask.RetrievalSourceCell;
                if (details.DestinationFinger == null) details.DestinationFinger = dbTask.RetrievalDestinationFinger;
                if (details.DestinationCell == null && dbTask.RetrievalDestinationCell != null) // HND to cell
                {
                    details.DestinationCell = dbTask.RetrievalDestinationCell;
                }
            }
            return details;
        }

        public EM.Maman.Models.LocalDbModels.Task ToDbModel()
        {
            var dbTask = new EM.Maman.Models.LocalDbModels.Task
            {
                Id = Id > 0 ? Id : 0, // EF handles Id generation for new entities if Id is 0
                Code = Code,
                Name = Name,
                Description = Description,
                TaskTypeId = (long)TaskType,
                IsExecuted = Status == Enums.TaskStatus.Completed,
                DownloadDate = CreatedDateTime,
                ExecutedDate = ExecutedDateTime,
                PalletId = Pallet?.Id,
                IsUploaded = IsUploaded,
                Status = (int)Status,
                ActiveTaskStatus = (int)ActiveTaskStatus
                // UserId, OperatorNotes, IsPriority would be set here if they were properties of TaskDetails
            };

            if (TaskType == Enums.TaskType.Storage)
            {
                dbTask.StorageSourceFingerId = SourceFinger?.Id;
                dbTask.StorageDestinationCellId = DestinationCell?.Id;
            }
            else if (TaskType == Enums.TaskType.Retrieval)
            {
                dbTask.RetrievalSourceCellId = SourceCell?.Id;
                dbTask.RetrievalDestinationFingerId = DestinationFinger?.Id;
                // For HND retrieval to cell
                if (DestinationCell != null && DestinationFinger == null) // Heuristic: if DestCell is set and DestFinger is not, it's a cell-to-cell retrieval
                {
                     dbTask.RetrievalDestinationCellId = DestinationCell?.Id;
                }
                else if (DestinationCell != null && DestinationFinger != null) // If both are somehow set, prioritize finger for API tasks
                {
                    // This case might need more business logic if a retrieval task can have BOTH a dest finger AND a dest cell.
                    // For now, if dest finger is set, assume it's the primary destination for retrieval.
                    // If only dest cell is set (and it's a retrieval task), then it's HND to cell.
                     dbTask.RetrievalDestinationCellId = (DestinationFinger == null) ? DestinationCell?.Id : null;
                }
            }

            return dbTask;
        }
    }
}
