﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NuGetGallery
{
    public class StatisticsPackagesItemViewModel
    {
        public string PackageId { get; set; }
        public string PackageVersion { get; set; }
        public int Downloads { get; set; }
    }
}