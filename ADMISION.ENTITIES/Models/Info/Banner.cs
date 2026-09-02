using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ADMISION.ENTITIES.Models.Info
{
    [Table("Banner", Schema = "Info")]
    public class Banner
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string ImageUrlVertical { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;
    }
}
