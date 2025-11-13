using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SallseSense.Models;

[Table("Salle")]
public partial class Salle
{
    [Key]
    [Column("idSalle_PK")]
    public int IdSallePk { get; set; }

    [Required]
    [Column("numero")]
    [StringLength(40)]
    public string Numero { get; set; }

    [Column("capaciteMaximale")]
    public int CapaciteMaximale { get; set; }
}
