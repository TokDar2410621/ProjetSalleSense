using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SallseSense.Models;

[Table("Reservation")]
public partial class Reservation
{
    [Key]
    [Column("idReservation_PK")]
    public int IdReservationPk { get; set; }

    [Column("heureDebut")]
    public DateTime HeureDebut { get; set; }

    [Column("heureFin")]
    public DateTime HeureFin { get; set; }

    [Column("noSalle")]
    public int NoSalle { get; set; }

    [Column("noPersonne")]
    public int NoPersonne { get; set; }

    [Column("nombrePersonne")]
    public int NombrePersonne { get; set; }
}
