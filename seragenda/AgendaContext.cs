using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using seragenda.Models;

namespace seragenda;

public partial class AgendaContext : DbContext
{
    public AgendaContext()
    {
    }

    public AgendaContext(DbContextOptions<AgendaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Abonnement> Abonnements { get; set; }

    public virtual DbSet<AppartenirViseeAptitude> AppartenirViseeAptitudes { get; set; }

    public virtual DbSet<Aptitude> Aptitudes { get; set; }

    public virtual DbSet<CalendrierScolaire> CalendrierScolaires { get; set; }

    public virtual DbSet<Chapitre> Chapitres { get; set; }

    public virtual DbSet<Competence> Competences { get; set; }

    public virtual DbSet<Cour> Cours { get; set; }

    public virtual DbSet<CoursNiveau> CoursNiveaus { get; set; }

    public virtual DbSet<Domaine> Domaines { get; set; }

    public virtual DbSet<Livre> Livres { get; set; }

    public virtual DbSet<Niveau> Niveaus { get; set; }

    public virtual DbSet<NomVisee> NomVisees { get; set; }

    public virtual DbSet<Planification> Planifications { get; set; }

    public virtual DbSet<SeanceObjectif> SeanceObjectifs { get; set; }

    public virtual DbSet<SeanceRessource> SeanceRessources { get; set; }

    public virtual DbSet<Sousdomaine> Sousdomaines { get; set; }

    public virtual DbSet<Utilisateur> Utilisateurs { get; set; }

    public virtual DbSet<UtilisationChapitre> UtilisationChapitres { get; set; }

    public virtual DbSet<UtilisationLivre> UtilisationLivres { get; set; }

    public virtual DbSet<Visee> Visees { get; set; }

    public virtual DbSet<ViseesMaitriser> ViseesMaitrisers { get; set; }

    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Abonnement>(entity =>
        {
            entity.HasKey(e => e.IdAbonnement).HasName("abonnement_pkey");

            entity.ToTable("abonnement");

            entity.Property(e => e.IdAbonnement).HasColumnName("id_abonnement");
            entity.Property(e => e.DateDebut).HasColumnName("date_debut");
            entity.Property(e => e.DateFin).HasColumnName("date_fin");
            entity.Property(e => e.IdUserFk).HasColumnName("id_user_fk");
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Actif'::character varying")
                .HasColumnName("statut");
            entity.Property(e => e.TypeAbo)
                .HasMaxLength(50)
                .HasColumnName("type_abo");

            entity.HasOne(d => d.IdUserFkNavigation).WithMany(p => p.Abonnements)
                .HasForeignKey(d => d.IdUserFk)
                .HasConstraintName("abonnement_id_user_fk_fkey");
        });

        modelBuilder.Entity<AppartenirViseeAptitude>(entity =>
        {
            entity.HasKey(e => e.IdAppartenirViseeAptitude).HasName("appartenir_visee_aptitude_pkey");

            entity.ToTable("appartenir_visee_aptitude");

            entity.Property(e => e.IdAppartenirViseeAptitude).HasColumnName("id_appartenir_visee_aptitude");
            entity.Property(e => e.IdAptitudeFk).HasColumnName("id_aptitude_fk");
            entity.Property(e => e.IdCompetenceFk).HasColumnName("id_competence_fk");
            entity.Property(e => e.IdViseesMaitriserFk).HasColumnName("id_visees_maitriser_fk");

            entity.HasOne(d => d.IdAptitudeFkNavigation).WithMany(p => p.AppartenirViseeAptitudes)
                .HasForeignKey(d => d.IdAptitudeFk)
                .HasConstraintName("appartenir_visee_aptitude_id_aptitude_fk_fkey");

            entity.HasOne(d => d.IdCompetenceFkNavigation).WithMany(p => p.AppartenirViseeAptitudes)
                .HasForeignKey(d => d.IdCompetenceFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("appartenir_visee_aptitude_id_competence_fk_fkey");

            entity.HasOne(d => d.IdViseesMaitriserFkNavigation).WithMany(p => p.AppartenirViseeAptitudes)
                .HasForeignKey(d => d.IdViseesMaitriserFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("appartenir_visee_aptitude_id_visees_maitriser_fk_fkey");
        });

        modelBuilder.Entity<Aptitude>(entity =>
        {
            entity.HasKey(e => e.IdAptitude).HasName("aptitude_pkey");

            entity.ToTable("aptitude");

            entity.Property(e => e.IdAptitude).HasColumnName("id_aptitude");
            entity.Property(e => e.NomAptitude)
                .HasMaxLength(50)
                .HasColumnName("nom_aptitude");
        });

        modelBuilder.Entity<CalendrierScolaire>(entity =>
        {
            entity.HasKey(e => e.IdCalendrier).HasName("calendrier_scolaire_pkey");

            entity.ToTable("calendrier_scolaire");

            entity.Property(e => e.IdCalendrier).HasColumnName("id_calendrier");
            entity.Property(e => e.AnneeScolaire)
                .HasMaxLength(9)
                .HasComputedColumnSql("\nCASE\n    WHEN (EXTRACT(month FROM date_debut) >= (8)::numeric) THEN (((EXTRACT(year FROM date_debut))::text || '-'::text) || ((EXTRACT(year FROM date_debut) + (1)::numeric))::text)\n    ELSE ((((EXTRACT(year FROM date_debut) - (1)::numeric))::text || '-'::text) || (EXTRACT(year FROM date_debut))::text)\nEND", true)
                .HasColumnName("annee_scolaire");
            entity.Property(e => e.DateDebut).HasColumnName("date_debut");
            entity.Property(e => e.DateFin).HasColumnName("date_fin");
            entity.Property(e => e.NomEvenement)
                .HasMaxLength(100)
                .HasColumnName("nom_evenement");
            entity.Property(e => e.TypeEvenement)
                .HasMaxLength(50)
                .HasColumnName("type_evenement");
        });

        modelBuilder.Entity<Chapitre>(entity =>
        {
            entity.HasKey(e => e.IdChapitre).HasName("chapitre_pkey");

            entity.ToTable("chapitre");

            entity.Property(e => e.IdChapitre).HasColumnName("id_chapitre");
            entity.Property(e => e.IdLivreFk).HasColumnName("id_livre_fk");
            entity.Property(e => e.NumeroChapitre).HasColumnName("numero_chapitre");
            entity.Property(e => e.PageDebut).HasColumnName("page_debut");
            entity.Property(e => e.TitreChapitre)
                .HasMaxLength(255)
                .HasColumnName("titre_chapitre");

            entity.HasOne(d => d.IdLivreFkNavigation).WithMany(p => p.Chapitres)
                .HasForeignKey(d => d.IdLivreFk)
                .HasConstraintName("chapitre_id_livre_fk_fkey");
        });

        modelBuilder.Entity<Competence>(entity =>
        {
            entity.HasKey(e => e.IdCompetence).HasName("competence_pkey");

            entity.ToTable("competence");

            entity.Property(e => e.IdCompetence).HasColumnName("id_competence");
            entity.Property(e => e.NomCompetence)
                .HasMaxLength(50)
                .HasColumnName("nom_competence");
        });

        modelBuilder.Entity<Cour>(entity =>
        {
            entity.HasKey(e => e.IdCours).HasName("cours_pkey");

            entity.ToTable("cours");

            entity.HasIndex(e => e.CodeCours, "cours_code_cours_key").IsUnique();

            entity.Property(e => e.IdCours).HasColumnName("id_cours");
            entity.Property(e => e.CodeCours)
                .HasMaxLength(20)
                .HasColumnName("code_cours");
            entity.Property(e => e.CouleurAgenda)
                .HasMaxLength(9)
                .HasColumnName("couleur_agenda");
            entity.Property(e => e.NomCours)
                .HasMaxLength(100)
                .HasColumnName("nom_cours");
            entity.Property(e => e.PrefixCours)
                .HasMaxLength(20)
                .HasColumnName("prefix_cours");
        });

        modelBuilder.Entity<CoursNiveau>(entity =>
        {
            entity.HasKey(e => e.IdCoursNiveau).HasName("cours_niveau_pkey");

            entity.ToTable("cours_niveau");

            entity.HasIndex(e => new { e.IdCoursFk, e.IdNiveauFk, e.IdProfFk }, "cours_niveau_id_cours_fk_id_niveau_fk_id_prof_fk_key").IsUnique();

            entity.Property(e => e.IdCoursNiveau).HasColumnName("id_cours_niveau");
            entity.Property(e => e.IdCoursFk).HasColumnName("id_cours_fk");
            entity.Property(e => e.IdNiveauFk).HasColumnName("id_niveau_fk");
            entity.Property(e => e.IdProfFk).HasColumnName("id_prof_fk");

            entity.HasOne(d => d.IdCoursFkNavigation).WithMany(p => p.CoursNiveaus)
                .HasForeignKey(d => d.IdCoursFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cours_niveau_id_cours_fk_fkey");

            entity.HasOne(d => d.IdNiveauFkNavigation).WithMany(p => p.CoursNiveaus)
                .HasForeignKey(d => d.IdNiveauFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cours_niveau_id_niveau_fk_fkey");

            entity.HasOne(d => d.IdProfFkNavigation).WithMany(p => p.CoursNiveaus)
                .HasForeignKey(d => d.IdProfFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cours_niveau_id_prof_fk_fkey");
        });

        modelBuilder.Entity<Domaine>(entity =>
        {
            entity.HasKey(e => e.IdDom).HasName("domaine_pkey");

            entity.ToTable("domaine");

            entity.Property(e => e.IdDom).HasColumnName("id_dom");
            entity.Property(e => e.IdCoursNiveauFk).HasColumnName("id_cours_niveau_fk");
            entity.Property(e => e.Nom)
                .HasMaxLength(255)
                .HasColumnName("nom");

            entity.HasOne(d => d.IdCoursNiveauFkNavigation).WithMany(p => p.Domaines)
                .HasForeignKey(d => d.IdCoursNiveauFk)
                .HasConstraintName("domaine_id_cours_niveau_fk_fkey");
        });

        modelBuilder.Entity<Livre>(entity =>
        {
            entity.HasKey(e => e.IdLivre).HasName("livre_pkey");

            entity.ToTable("livre");

            entity.HasIndex(e => e.Isbn, "livre_isbn_key").IsUnique();

            entity.Property(e => e.IdLivre).HasColumnName("id_livre");
            entity.Property(e => e.Auteur)
                .HasMaxLength(150)
                .HasColumnName("auteur");
            entity.Property(e => e.Isbn)
                .HasMaxLength(13)
                .HasColumnName("isbn");
            entity.Property(e => e.MaisonEdition)
                .HasMaxLength(100)
                .HasColumnName("maison_edition");
            entity.Property(e => e.TitreLivre)
                .HasMaxLength(255)
                .HasColumnName("titre_livre");
        });

        modelBuilder.Entity<Niveau>(entity =>
        {
            entity.HasKey(e => e.IdNiveau).HasName("niveau_pkey");

            entity.ToTable("niveau");

            entity.HasIndex(e => e.CodeNiveau, "niveau_code_niveau_key").IsUnique();

            entity.Property(e => e.IdNiveau).HasColumnName("id_niveau");
            entity.Property(e => e.CodeNiveau)
                .HasMaxLength(5)
                .HasColumnName("code_niveau");
            entity.Property(e => e.NomNiveau)
                .HasMaxLength(50)
                .HasColumnName("nom_niveau");
        });

        modelBuilder.Entity<NomVisee>(entity =>
        {
            entity.HasKey(e => e.IdNomVisee).HasName("nom_visee_pkey");

            entity.ToTable("nom_visee");

            entity.Property(e => e.IdNomVisee).HasColumnName("id_nom_visee");
            entity.Property(e => e.NomVisee1)
                .HasMaxLength(150)
                .HasColumnName("nom_visee");
        });

        modelBuilder.Entity<Planification>(entity =>
        {
            entity.HasKey(e => e.IdPlanning).HasName("planification_pkey");

            entity.ToTable("planification");

            entity.Property(e => e.IdPlanning).HasColumnName("id_planning");
            entity.Property(e => e.DateSeance).HasColumnName("date_seance");
            entity.Property(e => e.HeureDebut).HasColumnName("heure_debut");
            entity.Property(e => e.HeureFin).HasColumnName("heure_fin");
            entity.Property(e => e.IdCalendrierFk).HasColumnName("id_calendrier_fk");
            entity.Property(e => e.IdCoursNiveauFk).HasColumnName("id_cours_niveau_fk");
            entity.Property(e => e.NoteProf).HasColumnName("note_prof");
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Prévue'::character varying")
                .HasColumnName("statut");

            entity.HasOne(d => d.IdCalendrierFkNavigation).WithMany(p => p.Planifications)
                .HasForeignKey(d => d.IdCalendrierFk)
                .HasConstraintName("planification_id_calendrier_fk_fkey");

            entity.HasOne(d => d.IdCoursNiveauFkNavigation).WithMany(p => p.Planifications)
                .HasForeignKey(d => d.IdCoursNiveauFk)
                .HasConstraintName("planification_id_cours_niveau_fk_fkey");
        });

        modelBuilder.Entity<SeanceObjectif>(entity =>
        {
            entity.HasKey(e => e.IdSeanceObj).HasName("seance_objectif_pkey");

            entity.ToTable("seance_objectif");

            entity.Property(e => e.IdSeanceObj).HasColumnName("id_seance_obj");
            entity.Property(e => e.EvaluationPrevue)
                .HasDefaultValue(false)
                .HasColumnName("evaluation_prevue");
            entity.Property(e => e.IdPlanningFk).HasColumnName("id_planning_fk");
            entity.Property(e => e.IdViseeFk).HasColumnName("id_visee_fk");

            entity.HasOne(d => d.IdPlanningFkNavigation).WithMany(p => p.SeanceObjectifs)
                .HasForeignKey(d => d.IdPlanningFk)
                .HasConstraintName("seance_objectif_id_planning_fk_fkey");

            entity.HasOne(d => d.IdViseeFkNavigation).WithMany(p => p.SeanceObjectifs)
                .HasForeignKey(d => d.IdViseeFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("seance_objectif_id_visee_fk_fkey");
        });

        modelBuilder.Entity<SeanceRessource>(entity =>
        {
            entity.HasKey(e => e.IdSeanceRes).HasName("seance_ressource_pkey");

            entity.ToTable("seance_ressource");

            entity.Property(e => e.IdSeanceRes).HasColumnName("id_seance_res");
            entity.Property(e => e.IdChapitreFk).HasColumnName("id_chapitre_fk");
            entity.Property(e => e.IdPlanningFk).HasColumnName("id_planning_fk");

            entity.HasOne(d => d.IdChapitreFkNavigation).WithMany(p => p.SeanceRessources)
                .HasForeignKey(d => d.IdChapitreFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("seance_ressource_id_chapitre_fk_fkey");

            entity.HasOne(d => d.IdPlanningFkNavigation).WithMany(p => p.SeanceRessources)
                .HasForeignKey(d => d.IdPlanningFk)
                .HasConstraintName("seance_ressource_id_planning_fk_fkey");
        });

        modelBuilder.Entity<Sousdomaine>(entity =>
        {
            entity.HasKey(e => e.IdSousDomaine).HasName("sousdomaine_pkey");

            entity.ToTable("sousdomaine");

            entity.Property(e => e.IdSousDomaine).HasColumnName("id_sous_domaine");
            entity.Property(e => e.IdDomFk).HasColumnName("id_dom_fk");
            entity.Property(e => e.NomComp)
                .HasMaxLength(50)
                .HasColumnName("nom_comp");

            entity.HasOne(d => d.IdDomFkNavigation).WithMany(p => p.Sousdomaines)
                .HasForeignKey(d => d.IdDomFk)
                .HasConstraintName("sousdomaine_id_dom_fk_fkey");
        });

        modelBuilder.Entity<Utilisateur>(entity =>
        {
            entity.HasKey(e => e.IdUser).HasName("utilisateur_pkey");

            entity.ToTable("utilisateur");

            entity.HasIndex(e => e.Email, "utilisateur_email_key").IsUnique();

            entity.Property(e => e.IdUser).HasColumnName("id_user");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.NomComplet)
                .HasMaxLength(150)
                .HasColumnName("nom_complet");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.RoleSysteme)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PROF'::character varying")
                .HasColumnName("role_systeme");
        });

        modelBuilder.Entity<UtilisationChapitre>(entity =>
        {
            entity.HasKey(e => e.IdUtilisation).HasName("utilisation_chapitre_pkey");

            entity.ToTable("utilisation_chapitre");

            entity.Property(e => e.IdUtilisation).HasColumnName("id_utilisation");
            entity.Property(e => e.IdChapitreFk).HasColumnName("id_chapitre_fk");
            entity.Property(e => e.IdCoursNiveauFk).HasColumnName("id_cours_niveau_fk");
            entity.Property(e => e.Statut)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Recommandé'::character varying")
                .HasColumnName("statut");

            entity.HasOne(d => d.IdChapitreFkNavigation).WithMany(p => p.UtilisationChapitres)
                .HasForeignKey(d => d.IdChapitreFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("utilisation_chapitre_id_chapitre_fk_fkey");

            entity.HasOne(d => d.IdCoursNiveauFkNavigation).WithMany(p => p.UtilisationChapitres)
                .HasForeignKey(d => d.IdCoursNiveauFk)
                .HasConstraintName("utilisation_chapitre_id_cours_niveau_fk_fkey");
        });

        modelBuilder.Entity<UtilisationLivre>(entity =>
        {
            entity.HasKey(e => e.IdUtilisation).HasName("utilisation_livre_pkey");

            entity.ToTable("utilisation_livre");

            entity.Property(e => e.IdUtilisation).HasColumnName("id_utilisation");
            entity.Property(e => e.IdCoursNiveauFk).HasColumnName("id_cours_niveau_fk");
            entity.Property(e => e.IdLivreFk).HasColumnName("id_livre_fk");
            entity.Property(e => e.Statut)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Recommandé'::character varying")
                .HasColumnName("statut");

            entity.HasOne(d => d.IdCoursNiveauFkNavigation).WithMany(p => p.UtilisationLivres)
                .HasForeignKey(d => d.IdCoursNiveauFk)
                .HasConstraintName("utilisation_livre_id_cours_niveau_fk_fkey");

            entity.HasOne(d => d.IdLivreFkNavigation).WithMany(p => p.UtilisationLivres)
                .HasForeignKey(d => d.IdLivreFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("utilisation_livre_id_livre_fk_fkey");
        });

        modelBuilder.Entity<Visee>(entity =>
        {
            entity.HasKey(e => e.IdVisee).HasName("visees_pkey");

            entity.ToTable("visees");

            entity.Property(e => e.IdVisee).HasColumnName("id_visee");
            entity.Property(e => e.IdCompFk).HasColumnName("id_comp_fk");
            entity.Property(e => e.IdDomaineFk).HasColumnName("id_domaine_fk");
            entity.Property(e => e.IdNomViseeFk).HasColumnName("id_nom_visee_fk");
            entity.Property(e => e.IdSousDomaineFk).HasColumnName("id_sous_domaine_fk");

            entity.HasOne(d => d.IdCompFkNavigation).WithMany(p => p.Visees)
                .HasForeignKey(d => d.IdCompFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("visees_id_comp_fk_fkey");

            entity.HasOne(d => d.IdDomaineFkNavigation).WithMany(p => p.Visees)
                .HasForeignKey(d => d.IdDomaineFk)
                .HasConstraintName("visees_id_domaine_fk_fkey");

            entity.HasOne(d => d.IdNomViseeFkNavigation).WithMany(p => p.Visees)
                .HasForeignKey(d => d.IdNomViseeFk)
                .HasConstraintName("visees_id_nom_visee_fk_fkey");

            entity.HasOne(d => d.IdSousDomaineFkNavigation).WithMany(p => p.Visees)
                .HasForeignKey(d => d.IdSousDomaineFk)
                .HasConstraintName("visees_id_sous_domaine_fk_fkey");

            entity.HasMany(d => d.IdViseesMaitriserFks).WithMany(p => p.IdViseeFks)
                .UsingEntity<Dictionary<string, object>>(
                    "LienViseeMaitrise",
                    r => r.HasOne<ViseesMaitriser>().WithMany()
                        .HasForeignKey("IdViseesMaitriserFk")
                        .HasConstraintName("lien_visee_maitrise_id_visees_maitriser_fk_fkey"),
                    l => l.HasOne<Visee>().WithMany()
                        .HasForeignKey("IdViseeFk")
                        .HasConstraintName("lien_visee_maitrise_id_visee_fk_fkey"),
                    j =>
                    {
                        j.HasKey("IdViseeFk", "IdViseesMaitriserFk").HasName("lien_visee_maitrise_pkey");
                        j.ToTable("lien_visee_maitrise");
                        j.IndexerProperty<int>("IdViseeFk").HasColumnName("id_visee_fk");
                        j.IndexerProperty<int>("IdViseesMaitriserFk").HasColumnName("id_visees_maitriser_fk");
                    });
        });

        modelBuilder.Entity<ViseesMaitriser>(entity =>
        {
            entity.HasKey(e => e.IdViseesMaitriser).HasName("visees_maitriser_pkey");

            entity.ToTable("visees_maitriser");

            entity.Property(e => e.IdViseesMaitriser).HasColumnName("id_visees_maitriser");
            entity.Property(e => e.NomViseesMaitriser).HasColumnName("nom_visees_maitriser");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
