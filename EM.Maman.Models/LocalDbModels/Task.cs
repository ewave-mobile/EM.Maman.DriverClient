﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EM.Maman.Models.LocalDbModels;

public partial class Task
{
    public int Id { get; set; }

    [StringLength(255)]
    public string Name { get; set; }

    public string Description { get; set; }

    public DateTime? DownloadDate { get; set; }

    public bool? IsExecuted { get; set; }

    public DateTime? ExecutedDate { get; set; }

    public bool? IsUploaded { get; set; }

    public DateTime? UploadDate { get; set; }

    public long? TaskTypeId { get; set; }

    // Existing generic location fields (to be phased out or used for backward compatibility during migration)
    public long? CellEndLocationId { get; set; }
    public long? FingerLocationId { get; set; }

    public long? CurrentTrolleyLocationId { get; set; } // Unrelated to source/destination logic

    public int? PalletId { get; set; }

    public string Code { get; set; }

    public int? Status { get; set; }

    public int? ActiveTaskStatus { get; set; }

    // New specific location fields for clarity
    public long? StorageSourceFingerId { get; set; }
    public long? StorageDestinationCellId { get; set; }

    public long? RetrievalSourceCellId { get; set; }
    public long? RetrievalDestinationFingerId { get; set; }
    public long? RetrievalDestinationCellId { get; set; } // For HND retrieval to another cell

    // Existing generic navigation properties (to be phased out or re-evaluated)
    public virtual Cell CellEndLocation { get; set; }
    public virtual Finger FingerLocation { get; set; }

    // New specific navigation properties
    public virtual Finger StorageSourceFinger { get; set; }
    public virtual Cell StorageDestinationCell { get; set; }

    public virtual Cell RetrievalSourceCell { get; set; }
    public virtual Finger RetrievalDestinationFinger { get; set; }
    public virtual Cell RetrievalDestinationCell { get; set; } // For HND retrieval to another cell
}
