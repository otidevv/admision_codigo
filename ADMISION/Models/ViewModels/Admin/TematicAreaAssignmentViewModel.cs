using System;
using System.Collections.Generic;
using ADMISION.ENTITIES.Models.Modality;

namespace ADMISION.Models.ViewModels.Admin
{
    public class TematicAreaAssignmentViewModel
    {
        public Guid TermId { get; set; }
        public Guid CareerId { get; set; }
        public List<TematicAreaSelection> Selections { get; set; } = new();
    }

    public class TematicAreaSelection
    {
        public Guid TematicAreaId { get; set; }
        public string Code { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
