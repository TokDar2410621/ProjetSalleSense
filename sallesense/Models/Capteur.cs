using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SallseSense.Models;

[Table("Capteur")]
public partial class Capteur
{
    [Key]
    [Column("idCapteur_PK")]
    public int IdCapteurPk { get; set; }

    [Required]
    [Column("nom")]
    [StringLength(80)]
    public string Nom { get; set; }

    [Required]
    [Column("type")]
    [StringLength(40)]
    public string Type { get; set; }
}
