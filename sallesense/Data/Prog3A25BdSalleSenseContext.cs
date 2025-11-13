using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SallseSense.Models;

namespace SallseSense.Data;

public partial class Prog3A25BdSalleSenseContext : DbContext
{
    public Prog3A25BdSalleSenseContext()
    {
    }

    public Prog3A25BdSalleSenseContext(DbContextOptions<Prog3A25BdSalleSenseContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Avertissement> Avertissements { get; set; }

    public virtual DbSet<Blacklist> Blacklists { get; set; }

    public virtual DbSet<Capteur> Capteurs { get; set; }

    public virtual DbSet<Donnee> Donnees { get; set; }

    public virtual DbSet<Evenement> Evenements { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<Salle> Salles { get; set; }

    public virtual DbSet<Utilisateur> Utilisateurs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Avertissement>(entity =>
        {
            entity.HasKey(e => e.IdAvertissement).HasName("PK__Avertiss__A60A7EBB6F1A0864");

            entity.ToTable("Avertissement", tb => tb.HasTrigger("verifierNombreAvertissement"));
        });

        modelBuilder.Entity<Blacklist>(entity =>
        {
            entity.HasKey(e => e.IdBlacklistPk).HasName("PK__Blacklis__32DF64FC2F610650");
        });

        modelBuilder.Entity<Capteur>(entity =>
        {
            entity.HasKey(e => e.IdCapteurPk).HasName("PK__Capteur__DA9D4401A85FEDFD");
        });

        modelBuilder.Entity<Donnee>(entity =>
        {
            entity.HasKey(e => e.IdDonneePk).HasName("PK__Donnees__8F493E415BC86D3C");
        });

        modelBuilder.Entity<Evenement>(entity =>
        {
            entity.HasKey(e => e.IdEvenementPk).HasName("PK__Evenemen__6D4AEA8BCDAFECE6");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.IdReservationPk).HasName("PK__Reservat__52DA0BE394F5CDA3");

            entity.ToTable("Reservation", tb => tb.HasTrigger("trg_pasDeChevauchement"));
        });

        modelBuilder.Entity<Salle>(entity =>
        {
            entity.HasKey(e => e.IdSallePk).HasName("PK__Salle__AAAEF125AAA5A986");
        });

        modelBuilder.Entity<Utilisateur>(entity =>
        {
            entity.HasKey(e => e.IdUtilisateurPk).HasName("PK__Utilisat__D4E9F5C89F5B6451");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
