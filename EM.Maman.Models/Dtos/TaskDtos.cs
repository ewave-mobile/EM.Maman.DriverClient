#nullable disable // To match the style of other DTOs/Models if applicable
using System;
using System.Collections.Generic;
// Using System.Text.Json.Serialization for potential JsonPropertyName attributes if needed later
// For now, assuming property names match JSON keys directly or with default case-insensitivity.

namespace EM.Maman.Models.Dtos
{
    public class TaskItemDto
    {
        public int Id { get; set; }
        public int CraneId { get; set; }
        public string FingerCode { get; set; }
        public int RequestTypeId { get; set; }
        public int CargoTypeId { get; set; }
        public string CargoAirLine { get; set; }
        public string Ramp { get; set; }
        public string Transporter { get; set; }
        public string CargoFlightNumber { get; set; }
        public string CargoFlightDate { get; set; }
        public string RequestDate { get; set; }
        public string RequestTime { get; set; }
        public string ExpectedExecutionTime { get; set; }
        public string Remark { get; set; }
        public int StorageTypeId { get; set; }
        public bool IsSecuredStorage { get; set; }
        public int CargoHeightRangeTypeId { get; set; }
        public int CargoHeight { get; set; }
        public string ImpManifest { get; set; }
        public string ImpUnit { get; set; }
        public string ImpAppearance { get; set; }
        public string AwbPrefix { get; set; }
        public string AwbNumber { get; set; }
        public int ExpAwbAppearance { get; set; }
        public int ExpAwbStorage { get; set; }
        public string ExpBarcode { get; set; }
        public string UldType { get; set; }
        public string UldNumber { get; set; }
        public string UldAirline { get; set; }
        public string RequestInitiator { get; set; }
        public DateTime CreateDate { get; set; }
        public int? QtyPallets2Retreive { get; set; }
        public int? RetreivePurposeType { get; set; }
        public bool IsProhibitedForStorage { get; set; }
        public string UldBarcode { get; set; }
        public string Destination { get; set; }
        public string UldFlightAirline { get; set; }
        public string UldFlightNumber { get; set; }
        public string UldDate { get; set; }
        public string UldPldestination { get; set; } // Property name from JSON is "uldPldestination"
        public int ActionTypeId { get; set; }
        public int? RecommendedCellId { get; set; }
        public int? ActualCellId { get; set; }
        public int TaskStatusId { get; set; }
        public DateTime DownloadedDate { get; set; }
    }

    public class TasksApiResponseDto
    {
        public List<TaskItemDto> Result { get; set; }
        public int Id { get; set; }
        public object Exception { get; set; }
        public int Status { get; set; }
        public bool IsCanceled { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsCompletedSuccessfully { get; set; }
        public int CreationOptions { get; set; }
        public object AsyncState { get; set; }
        public bool IsFaulted { get; set; }
    }
}
#nullable enable
