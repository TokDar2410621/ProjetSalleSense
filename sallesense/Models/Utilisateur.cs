using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SallseSense.Models;

[Table("Utilisateur")]
public partial class Utilisateur
{
    [Key]
    [Column("idUtilisateur_PK")]
    public int IdUtilisateurPk { get; set; }

    [Required]
    [Column("pseudo")]
    [StringLength(80)]
    public string Pseudo { get; set; }

    [Required]
    [Column("courriel")]
    [StringLength(120)]
    public string Courriel { get; set; }

    
    [Column("motDePasse")]
    [StringLength(255)]
    public string? MotDePasse { get; set; }

    [Column("mdp_salt")]
    [MaxLength(16)]
    public byte[] MdpSalt { get; set; }

    [Column("mdp_hash")]
    [MaxLength(32)]
    public byte[] MdpHash { get; set; }

    [Column("role")]
    [StringLength(20)]
    public string? Role { get; set; }
}
