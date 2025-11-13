using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SallseSense.Models;

[Table("Avertissement")]
public partial class Avertissement
{
    [Key]
    [Column("idAvertissement")]
    public int IdAvertissement { get; set; }

    [Column("idUtilisateur")]
    public int IdUtilisateur { get; set; }

    [Column("idEvenement")]
    public int IdEvenement { get; set; }
}
