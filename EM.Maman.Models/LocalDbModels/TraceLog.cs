﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace EM.Maman.Models.LocalDbModels;

public partial class TraceLog
{
    public long Id { get; set; }

    public string Url { get; set; }

    public string Request { get; set; }

    public string RequestBody { get; set; }

    public string ResponseCode { get; set; }

    public string ResponseBody { get; set; }
}