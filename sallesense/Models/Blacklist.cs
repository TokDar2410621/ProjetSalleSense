using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SallseSense.Models;

[Table("Blacklist")]
public partial class Blacklist
{
    [Key]
    [Column("idBlacklist_PK")]
    public int IdBlacklistPk { get; set; }

    [Column("idUtilisateur")]
    public int IdUtilisateur { get; set; }

    [Column("duree")]
    public TimeOnly? Duree { get; set; }
}
