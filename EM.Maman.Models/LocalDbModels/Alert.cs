﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace EM.Maman.Models.LocalDbModels;

public partial class Alert
{
    public long Id { get; set; }

    public string Title { get; set; }

    public string Content { get; set; }

    public string Color { get; set; }

    public bool? WasDisplayed { get; set; }

    public bool? ConfirmedToApi { get; set; }

    public DateTime? ConfirmedDate { get; set; }

    public DateTime? DownloadDate { get; set; }
}