using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SallseSense.Models;

[Table("Evenement")]
public partial class Evenement
{
    [Key]
    [Column("idEvenement_PK")]
    public int IdEvenementPk { get; set; }

    [Required]
    [Column("type")]
    [StringLength(60)]
    public string Type { get; set; }

    [Column("idDonnee")]
    public int IdDonnee { get; set; }

    [Column("description")]
    public string Description { get; set; }
}
