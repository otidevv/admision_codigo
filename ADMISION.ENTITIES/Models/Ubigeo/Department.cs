using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ADMISION.ENTITIES.Models.Ubigeo
{
    [Table("Department", Schema = "Ubigeo")]
    public class Department
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;
        public Guid CountryId { get; set; }

        [ForeignKey("CountryId")]
        public virtual Country? Country { get; set; }
        public virtual ICollection<Provincie>? Provincies { get; set; }

    }
}
