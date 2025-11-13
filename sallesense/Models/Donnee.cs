using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SallseSense.Models;

public partial class Donnee
{
    [Key]
    [Column("idDonnee_PK")]
    public int IdDonneePk { get; set; }

    [Column("dateHeure")]
    public DateTime DateHeure { get; set; }

    [Column("idCapteur")]
    public int IdCapteur { get; set; }

    [Column("mesure")]
    public double? Mesure { get; set; }

    [Column("photo")]
    [StringLength(255)]
    public string Photo { get; set; }

    [Column("photoBlob")]
    public byte[] PhotoBlob { get; set; }

    [Column("noSalle")]
    public int NoSalle { get; set; }
}
