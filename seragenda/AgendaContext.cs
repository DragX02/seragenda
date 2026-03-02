// Import base .NET types
using System;
using System.Collections.Generic;
// Import Entity Framework Core for DbContext, DbSet, and model builder
using Microsoft.EntityFrameworkCore;
// Import all entity models
using seragenda.Models;

// File-scoped namespace (C# 10+ style)
namespace seragenda;

/// <summary>
/// The EF Core database context for the Agenda application.
/// Provides <see cref="DbSet{T}"/> properties for every entity type mapped to the PostgreSQL database.
/// The OnModelCreating method configures all column mappings, primary keys, foreign keys,
/// unique indexes, default values, and navigation properties using the Fluent API.
/// This class is declared as partial to support generated code in a companion partial class
/// (e.g., from EF Core scaffolding or source generators).
/// </summary>
public partial class AgendaContext : DbContext
{
    /// <summary>
    /// Parameterless constructor required by EF Core design-time tools (migrations, scaffolding).
    /// Not used at runtime when the DI container provides options.
    /// </summary>
    public AgendaContext()
    {
    }

    /// <summary>
    /// Primary constructor used at runtime.
    /// ASP.NET Core's DI container calls this constructor with the options configured in Program.cs.
    /// </summary>
    /// <param name="options">EF Core options including the Npgsql connection string</param>
    public AgendaContext(DbContextOptions<AgendaContext> options)
        : base(options)
    {
    }

    // DbSet for the "abonnement" table — subscription records for each user
    public virtual DbSet<Abonnement> Abonnements { get; set; }

    // DbSet for the "appartenir_visee_aptitude" table — links mastery targets to competencies and aptitudes
    public virtual DbSet<AppartenirViseeAptitude> AppartenirViseeAptitudes { get; set; }

    // DbSet for the "aptitude" table — specific observable skills or behaviours
    public virtual DbSet<Aptitude> Aptitudes { get; set; }

    // DbSet for the "calendrier_scolaire" table — school calendar events (holidays, back-to-school, etc.)
    public virtual DbSet<CalendrierScolaire> CalendrierScolaires { get; set; }

    // DbSet for the "chapitre" table — book chapters in the resource catalogue
    public virtual DbSet<Chapitre> Chapitres { get; set; }

    // DbSet for the "competence" table — broad competency categories
    public virtual DbSet<Competence> Competences { get; set; }

    // DbSet for the "cours" table — subjects/courses in the curriculum
    public virtual DbSet<Cour> Cours { get; set; }

    // DbSet for the "cours_niveau" table — many-to-many between subjects, levels, and teachers
    public virtual DbSet<CoursNiveau> CoursNiveaus { get; set; }

    // DbSet for the "domaine" table — pedagogical domains within a course-level combination
    public virtual DbSet<Domaine> Domaines { get; set; }

    // DbSet for the "livre" table — textbooks in the resource catalogue
    public virtual DbSet<Livre> Livres { get; set; }

    // DbSet for the "niveau" table — education levels (year groups / grades)
    public virtual DbSet<Niveau> Niveaus { get; set; }

    // DbSet for the "nom_visee" table — label dictionary for learning objective types
    public virtual DbSet<NomVisee> NomVisees { get; set; }

    // DbSet for the "planification" table — planned lesson sessions
    public virtual DbSet<Planification> Planifications { get; set; }

    // DbSet for the "seance_objectif" table — learning objectives targeted in a session
    public virtual DbSet<SeanceObjectif> SeanceObjectifs { get; set; }

    // DbSet for the "seance_ressource" table — book chapters used in a session
    public virtual DbSet<SeanceRessource> SeanceRessources { get; set; }

    // DbSet for the "sousdomaine" table — sub-domains within a pedagogical domain
    public virtual DbSet<Sousdomaine> Sousdomaines { get; set; }

    // DbSet for the "utilisateur" table — all registered user accounts
    public virtual DbSet<Utilisateur> Utilisateurs { get; set; }

    // DbSet for the "utilisation_chapitre" table — chapter usage records per course-level
    public virtual DbSet<UtilisationChapitre> UtilisationChapitres { get; set; }

    // DbSet for the "utilisation_livre" table — book usage records per course-level
    public virtual DbSet<UtilisationLivre> UtilisationLivres { get; set; }

    // DbSet for the "visees" table — individual learning objectives
    public virtual DbSet<Visee> Visees { get; set; }

    // DbSet for the "visees_maitriser" table — high-level mastery targets
    public virtual DbSet<ViseesMaitriser> ViseesMaitrisers { get; set; }

    // DbSet for the "user_course" table — recurring course schedule entries per user
    public virtual DbSet<UserCourse> UserCourses { get; set; }

    // DbSet for the "user_note" table — personal timed notes on the daily agenda
    public virtual DbSet<UserNote> UserNotes { get; set; }

    // DbSet for the "license" table — license key records managed by admins
    public virtual DbSet<License> Licenses { get; set; }

    /// <summary>
    /// Configures the EF Core model using the Fluent API.
    /// Maps every entity class to its corresponding database table and column,
    /// defines primary keys, foreign key constraints, unique indexes, computed columns,
    /// default values, and navigation property relationships.
    /// Called automatically by EF Core when the model is first accessed.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder used to configure entity mappings</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- Abonnement entity configuration ---
        modelBuilder.Entity<Abonnement>(entity =>
        {
            // Primary key with the named constraint used in PostgreSQL
            entity.HasKey(e => e.IdAbonnement).HasName("abonnement_pkey");

            // Maps to the "abonnement" PostgreSQL table
            entity.ToTable("abonnement");

            // Column name mappings (snake_case in DB → PascalCase in C#)
            entity.Property(e => e.IdAbonnement).HasColumnName("id_abonnement");
            entity.Property(e => e.DateDebut).HasColumnName("date_debut");
            entity.Property(e => e.DateFin).HasColumnName("date_fin");
            entity.Property(e => e.IdUserFk).HasColumnName("id_user_fk");
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                // PostgreSQL default: 'Actif'::character varying
                .HasDefaultValueSql("'Actif'::character varying")
                .HasColumnName("statut");
            entity.Property(e => e.TypeAbo)
                .HasMaxLength(50)
                .HasColumnName("type_abo");

            // Foreign key: Abonnement.IdUserFk → Utilisateur.IdUser (one user has many subscriptions)
            entity.HasOne(d => d.IdUserFkNavigation).WithMany(p => p.Abonnements)
                .HasForeignKey(d => d.IdUserFk)
                .HasConstraintName("abonnement_id_user_fk_fkey");
        });

        // --- AppartenirViseeAptitude entity configuration ---
        modelBuilder.Entity<AppartenirViseeAptitude>(entity =>
        {
            entity.HasKey(e => e.IdAppartenirViseeAptitude).HasName("appartenir_visee_aptitude_pkey");
            entity.ToTable("appartenir_visee_aptitude");

            entity.Property(e => e.IdAppartenirViseeAptitude).HasColumnName("id_appartenir_visee_aptitude");
            entity.Property(e => e.IdAptitudeFk).HasColumnName("id_aptitude_fk");
            entity.Property(e => e.IdCompetenceFk).HasColumnName("id_competence_fk");
            entity.Property(e => e.IdViseesMaitriserFk).HasColumnName("id_visees_maitriser_fk");

            // Foreign key: optional link to Aptitude (nullable FK — aptitude may not be set)
            entity.HasOne(d => d.IdAptitudeFkNavigation).WithMany(p => p.AppartenirViseeAptitudes)
                .HasForeignKey(d => d.IdAptitudeFk)
                .HasConstraintName("appartenir_visee_aptitude_id_aptitude_fk_fkey");

            // Foreign key: required link to Competence (ClientSetNull prevents cascade delete)
            entity.HasOne(d => d.IdCompetenceFkNavigation).WithMany(p => p.AppartenirViseeAptitudes)
                .HasForeignKey(d => d.IdCompetenceFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("appartenir_visee_aptitude_id_competence_fk_fkey");

            // Foreign key: required link to ViseesMaitriser (ClientSetNull prevents cascade delete)
            entity.HasOne(d => d.IdViseesMaitriserFkNavigation).WithMany(p => p.AppartenirViseeAptitudes)
                .HasForeignKey(d => d.IdViseesMaitriserFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("appartenir_visee_aptitude_id_visees_maitriser_fk_fkey");
        });

        // --- Aptitude entity configuration ---
        modelBuilder.Entity<Aptitude>(entity =>
        {
            entity.HasKey(e => e.IdAptitude).HasName("aptitude_pkey");
            entity.ToTable("aptitude");

            entity.Property(e => e.IdAptitude).HasColumnName("id_aptitude");
            entity.Property(e => e.NomAptitude)
                .HasMaxLength(50)
                .HasColumnName("nom_aptitude");
        });

        // --- CalendrierScolaire entity configuration ---
        modelBuilder.Entity<CalendrierScolaire>(entity =>
        {
            entity.HasKey(e => e.IdCalendrier).HasName("calendrier_scolaire_pkey");
            entity.ToTable("calendrier_scolaire");

            entity.Property(e => e.IdCalendrier).HasColumnName("id_calendrier");
            // AnneeScolaire is a GENERATED ALWAYS AS (computed) column in PostgreSQL.
            // EF Core must not try to insert/update it — hence stored: true.
            // The SQL formula derives the school year string from the event's start month.
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

        // --- Chapitre entity configuration ---
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

            // Foreign key: chapter belongs to exactly one book; deletes cascade from Livre
            entity.HasOne(d => d.IdLivreFkNavigation).WithMany(p => p.Chapitres)
                .HasForeignKey(d => d.IdLivreFk)
                .HasConstraintName("chapitre_id_livre_fk_fkey");
        });

        // --- Competence entity configuration ---
        modelBuilder.Entity<Competence>(entity =>
        {
            entity.HasKey(e => e.IdCompetence).HasName("competence_pkey");
            entity.ToTable("competence");

            entity.Property(e => e.IdCompetence).HasColumnName("id_competence");
            entity.Property(e => e.NomCompetence)
                .HasMaxLength(50)
                .HasColumnName("nom_competence");
        });

        // --- Cour (subject) entity configuration ---
        modelBuilder.Entity<Cour>(entity =>
        {
            entity.HasKey(e => e.IdCours).HasName("cours_pkey");
            entity.ToTable("cours");

            // Unique index on CodeCours to ensure no two subjects share the same short code
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

        // --- CoursNiveau entity configuration ---
        modelBuilder.Entity<CoursNiveau>(entity =>
        {
            entity.HasKey(e => e.IdCoursNiveau).HasName("cours_niveau_pkey");
            entity.ToTable("cours_niveau");

            // Composite unique index: the same teacher cannot be listed twice for the same subject+level
            entity.HasIndex(e => new { e.IdCoursFk, e.IdNiveauFk, e.IdProfFk }, "cours_niveau_id_cours_fk_id_niveau_fk_id_prof_fk_key").IsUnique();

            entity.Property(e => e.IdCoursNiveau).HasColumnName("id_cours_niveau");
            entity.Property(e => e.IdCoursFk).HasColumnName("id_cours_fk");
            entity.Property(e => e.IdNiveauFk).HasColumnName("id_niveau_fk");
            entity.Property(e => e.IdProfFk).HasColumnName("id_prof_fk");

            // Foreign key to Cour; ClientSetNull prevents accidental cascade deletion
            entity.HasOne(d => d.IdCoursFkNavigation).WithMany(p => p.CoursNiveaus)
                .HasForeignKey(d => d.IdCoursFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cours_niveau_id_cours_fk_fkey");

            // Foreign key to Niveau
            entity.HasOne(d => d.IdNiveauFkNavigation).WithMany(p => p.CoursNiveaus)
                .HasForeignKey(d => d.IdNiveauFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cours_niveau_id_niveau_fk_fkey");

            // Foreign key to Utilisateur (the teacher); ClientSetNull prevents cascade deletion
            entity.HasOne(d => d.IdProfFkNavigation).WithMany(p => p.CoursNiveaus)
                .HasForeignKey(d => d.IdProfFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cours_niveau_id_prof_fk_fkey");
        });

        // --- Domaine entity configuration ---
        modelBuilder.Entity<Domaine>(entity =>
        {
            entity.HasKey(e => e.IdDom).HasName("domaine_pkey");
            entity.ToTable("domaine");

            entity.Property(e => e.IdDom).HasColumnName("id_dom");
            entity.Property(e => e.IdCoursNiveauFk).HasColumnName("id_cours_niveau_fk");
            entity.Property(e => e.Nom)
                .HasMaxLength(255)
                .HasColumnName("nom");

            // Foreign key: domain belongs to exactly one course-level combination
            entity.HasOne(d => d.IdCoursNiveauFkNavigation).WithMany(p => p.Domaines)
                .HasForeignKey(d => d.IdCoursNiveauFk)
                .HasConstraintName("domaine_id_cours_niveau_fk_fkey");
        });

        // --- Livre entity configuration ---
        modelBuilder.Entity<Livre>(entity =>
        {
            entity.HasKey(e => e.IdLivre).HasName("livre_pkey");
            entity.ToTable("livre");

            // Unique index on ISBN to prevent duplicate book entries
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

        // --- Niveau entity configuration ---
        modelBuilder.Entity<Niveau>(entity =>
        {
            entity.HasKey(e => e.IdNiveau).HasName("niveau_pkey");
            entity.ToTable("niveau");

            // Unique index on CodeNiveau (e.g., "1A", "3B")
            entity.HasIndex(e => e.CodeNiveau, "niveau_code_niveau_key").IsUnique();

            entity.Property(e => e.IdNiveau).HasColumnName("id_niveau");
            entity.Property(e => e.CodeNiveau)
                .HasMaxLength(5)
                .HasColumnName("code_niveau");
            entity.Property(e => e.NomNiveau)
                .HasMaxLength(50)
                .HasColumnName("nom_niveau");
        });

        // --- NomVisee entity configuration ---
        modelBuilder.Entity<NomVisee>(entity =>
        {
            entity.HasKey(e => e.IdNomVisee).HasName("nom_visee_pkey");
            entity.ToTable("nom_visee");

            entity.Property(e => e.IdNomVisee).HasColumnName("id_nom_visee");
            // NomVisee1 maps to the "nom_visee" column (the "1" suffix avoids a name clash with the class)
            entity.Property(e => e.NomVisee1)
                .HasMaxLength(150)
                .HasColumnName("nom_visee");
        });

        // --- Planification entity configuration ---
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
                // New sessions default to "Prévue" (planned)
                .HasDefaultValueSql("'Prévue'::character varying")
                .HasColumnName("statut");

            // Optional FK to CalendrierScolaire (a session can be associated with a calendar event)
            entity.HasOne(d => d.IdCalendrierFkNavigation).WithMany(p => p.Planifications)
                .HasForeignKey(d => d.IdCalendrierFk)
                .HasConstraintName("planification_id_calendrier_fk_fkey");

            // Required FK to CoursNiveau (a session must belong to a course-level combination)
            entity.HasOne(d => d.IdCoursNiveauFkNavigation).WithMany(p => p.Planifications)
                .HasForeignKey(d => d.IdCoursNiveauFk)
                .HasConstraintName("planification_id_cours_niveau_fk_fkey");
        });

        // --- SeanceObjectif entity configuration ---
        modelBuilder.Entity<SeanceObjectif>(entity =>
        {
            entity.HasKey(e => e.IdSeanceObj).HasName("seance_objectif_pkey");
            entity.ToTable("seance_objectif");

            entity.Property(e => e.IdSeanceObj).HasColumnName("id_seance_obj");
            // Default value for evaluation_prevue is false (no evaluation planned)
            entity.Property(e => e.EvaluationPrevue)
                .HasDefaultValue(false)
                .HasColumnName("evaluation_prevue");
            entity.Property(e => e.IdPlanningFk).HasColumnName("id_planning_fk");
            entity.Property(e => e.IdViseeFk).HasColumnName("id_visee_fk");

            // FK to Planification: if the session is deleted, session objectives cascade
            entity.HasOne(d => d.IdPlanningFkNavigation).WithMany(p => p.SeanceObjectifs)
                .HasForeignKey(d => d.IdPlanningFk)
                .HasConstraintName("seance_objectif_id_planning_fk_fkey");

            // FK to Visee: ClientSetNull prevents deleting an objective that is referenced in sessions
            entity.HasOne(d => d.IdViseeFkNavigation).WithMany(p => p.SeanceObjectifs)
                .HasForeignKey(d => d.IdViseeFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("seance_objectif_id_visee_fk_fkey");
        });

        // --- SeanceRessource entity configuration ---
        modelBuilder.Entity<SeanceRessource>(entity =>
        {
            entity.HasKey(e => e.IdSeanceRes).HasName("seance_ressource_pkey");
            entity.ToTable("seance_ressource");

            entity.Property(e => e.IdSeanceRes).HasColumnName("id_seance_res");
            entity.Property(e => e.IdChapitreFk).HasColumnName("id_chapitre_fk");
            entity.Property(e => e.IdPlanningFk).HasColumnName("id_planning_fk");

            // FK to Chapitre: ClientSetNull prevents deleting chapters that are used in sessions
            entity.HasOne(d => d.IdChapitreFkNavigation).WithMany(p => p.SeanceRessources)
                .HasForeignKey(d => d.IdChapitreFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("seance_ressource_id_chapitre_fk_fkey");

            // FK to Planification: resource records cascade when the session is deleted
            entity.HasOne(d => d.IdPlanningFkNavigation).WithMany(p => p.SeanceRessources)
                .HasForeignKey(d => d.IdPlanningFk)
                .HasConstraintName("seance_ressource_id_planning_fk_fkey");
        });

        // --- Sousdomaine entity configuration ---
        modelBuilder.Entity<Sousdomaine>(entity =>
        {
            entity.HasKey(e => e.IdSousDomaine).HasName("sousdomaine_pkey");
            entity.ToTable("sousdomaine");

            entity.Property(e => e.IdSousDomaine).HasColumnName("id_sous_domaine");
            entity.Property(e => e.IdDomFk).HasColumnName("id_dom_fk");
            entity.Property(e => e.NomComp)
                .HasMaxLength(50)
                .HasColumnName("nom_comp");

            // FK to Domaine: a sub-domain belongs to exactly one parent domain
            entity.HasOne(d => d.IdDomFkNavigation).WithMany(p => p.Sousdomaines)
                .HasForeignKey(d => d.IdDomFk)
                .HasConstraintName("sousdomaine_id_dom_fk_fkey");
        });

        // --- Utilisateur entity configuration ---
        modelBuilder.Entity<Utilisateur>(entity =>
        {
            entity.HasKey(e => e.IdUser).HasName("utilisateur_pkey");
            entity.ToTable("utilisateur");

            // Unique index on email to enforce one account per email address
            entity.HasIndex(e => e.Email, "utilisateur_email_key").IsUnique();

            entity.Property(e => e.IdUser).HasColumnName("id_user");
            // CreatedAt defaults to CURRENT_TIMESTAMP server-side; stored as timestamp without timezone
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
            // RoleSysteme defaults to "PROF" for all newly created accounts
            entity.Property(e => e.RoleSysteme)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PROF'::character varying")
                .HasColumnName("role_systeme");
            // OAuth provider name (e.g., "Google", "Microsoft"); null for local email/password accounts
            entity.Property(e => e.AuthProvider)
                .HasMaxLength(50)
                .HasColumnName("auth_provider");
            // is_confirmed defaults to false; set to true after email confirmation
            entity.Property(e => e.IsConfirmed)
                .HasColumnName("is_confirmed")
                .HasDefaultValue(false);
            // One-time confirmation token sent to the user's email address
            entity.Property(e => e.ConfirmationToken)
                .HasMaxLength(100)
                .HasColumnName("confirmation_token");
            // Token expiry timestamp; stored without timezone (UTC is assumed by convention)
            entity.Property(e => e.ConfirmationTokenExpiresAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("confirmation_token_expires_at");
        });

        // --- UtilisationChapitre entity configuration ---
        modelBuilder.Entity<UtilisationChapitre>(entity =>
        {
            entity.HasKey(e => e.IdUtilisation).HasName("utilisation_chapitre_pkey");
            entity.ToTable("utilisation_chapitre");

            entity.Property(e => e.IdUtilisation).HasColumnName("id_utilisation");
            entity.Property(e => e.IdChapitreFk).HasColumnName("id_chapitre_fk");
            entity.Property(e => e.IdCoursNiveauFk).HasColumnName("id_cours_niveau_fk");
            entity.Property(e => e.Statut)
                .HasMaxLength(50)
                // New records default to "Recommandé" (recommended usage status)
                .HasDefaultValueSql("'Recommandé'::character varying")
                .HasColumnName("statut");

            // FK to Chapitre: ClientSetNull prevents deleting chapters that are referenced here
            entity.HasOne(d => d.IdChapitreFkNavigation).WithMany(p => p.UtilisationChapitres)
                .HasForeignKey(d => d.IdChapitreFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("utilisation_chapitre_id_chapitre_fk_fkey");

            // FK to CoursNiveau: chapter usage is scoped to a course-level combination
            entity.HasOne(d => d.IdCoursNiveauFkNavigation).WithMany(p => p.UtilisationChapitres)
                .HasForeignKey(d => d.IdCoursNiveauFk)
                .HasConstraintName("utilisation_chapitre_id_cours_niveau_fk_fkey");
        });

        // --- UtilisationLivre entity configuration ---
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

            // FK to CoursNiveau: book usage is scoped to a course-level combination
            entity.HasOne(d => d.IdCoursNiveauFkNavigation).WithMany(p => p.UtilisationLivres)
                .HasForeignKey(d => d.IdCoursNiveauFk)
                .HasConstraintName("utilisation_livre_id_cours_niveau_fk_fkey");

            // FK to Livre: ClientSetNull prevents deleting books that are referenced here
            entity.HasOne(d => d.IdLivreFkNavigation).WithMany(p => p.UtilisationLivres)
                .HasForeignKey(d => d.IdLivreFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("utilisation_livre_id_livre_fk_fkey");
        });

        // --- Visee entity configuration ---
        modelBuilder.Entity<Visee>(entity =>
        {
            entity.HasKey(e => e.IdVisee).HasName("visees_pkey");
            // Stored in the "visees" table (plural with an 's')
            entity.ToTable("visees");

            entity.Property(e => e.IdVisee).HasColumnName("id_visee");
            entity.Property(e => e.IdCompFk).HasColumnName("id_comp_fk");
            entity.Property(e => e.IdDomaineFk).HasColumnName("id_domaine_fk");
            entity.Property(e => e.IdNomViseeFk).HasColumnName("id_nom_visee_fk");
            entity.Property(e => e.IdSousDomaineFk).HasColumnName("id_sous_domaine_fk");

            // FK to Competence: ClientSetNull prevents deleting competencies that have objectives
            entity.HasOne(d => d.IdCompFkNavigation).WithMany(p => p.Visees)
                .HasForeignKey(d => d.IdCompFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("visees_id_comp_fk_fkey");

            // FK to Domaine
            entity.HasOne(d => d.IdDomaineFkNavigation).WithMany(p => p.Visees)
                .HasForeignKey(d => d.IdDomaineFk)
                .HasConstraintName("visees_id_domaine_fk_fkey");

            // FK to NomVisee (the type/label of this objective)
            entity.HasOne(d => d.IdNomViseeFkNavigation).WithMany(p => p.Visees)
                .HasForeignKey(d => d.IdNomViseeFk)
                .HasConstraintName("visees_id_nom_visee_fk_fkey");

            // Optional FK to Sousdomaine (sub-domain is not required)
            entity.HasOne(d => d.IdSousDomaineFkNavigation).WithMany(p => p.Visees)
                .HasForeignKey(d => d.IdSousDomaineFk)
                .HasConstraintName("visees_id_sous_domaine_fk_fkey");

            // Many-to-many between Visee and ViseesMaitriser via the "lien_visee_maitrise" join table.
            // EF Core manages this as a shadow entity with no explicit C# class.
            entity.HasMany(d => d.IdViseesMaitriserFks).WithMany(p => p.IdViseeFks)
                .UsingEntity<Dictionary<string, object>>(
                    "LienViseeMaitrise",
                    // Right side: each join row references one ViseesMaitriser
                    r => r.HasOne<ViseesMaitriser>().WithMany()
                        .HasForeignKey("IdViseesMaitriserFk")
                        .HasConstraintName("lien_visee_maitrise_id_visees_maitriser_fk_fkey"),
                    // Left side: each join row references one Visee
                    l => l.HasOne<Visee>().WithMany()
                        .HasForeignKey("IdViseeFk")
                        .HasConstraintName("lien_visee_maitrise_id_visee_fk_fkey"),
                    // Join table configuration
                    j =>
                    {
                        // Composite primary key of the join table
                        j.HasKey("IdViseeFk", "IdViseesMaitriserFk").HasName("lien_visee_maitrise_pkey");
                        j.ToTable("lien_visee_maitrise");
                        j.IndexerProperty<int>("IdViseeFk").HasColumnName("id_visee_fk");
                        j.IndexerProperty<int>("IdViseesMaitriserFk").HasColumnName("id_visees_maitriser_fk");
                    });
        });

        // --- ViseesMaitriser entity configuration ---
        modelBuilder.Entity<ViseesMaitriser>(entity =>
        {
            entity.HasKey(e => e.IdViseesMaitriser).HasName("visees_maitriser_pkey");
            entity.ToTable("visees_maitriser");

            entity.Property(e => e.IdViseesMaitriser).HasColumnName("id_visees_maitriser");
            // Text column with no explicit max length — can store long mastery-target descriptions
            entity.Property(e => e.NomViseesMaitriser).HasColumnName("nom_visees_maitriser");
        });

        // --- UserCourse entity configuration ---
        modelBuilder.Entity<UserCourse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_course_pkey");
            entity.ToTable("user_course");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IdUserFk).HasColumnName("id_user_fk");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            // Hex colour code (max "#RRGGBBAA" = 9 chars including the '#')
            entity.Property(e => e.Color).HasMaxLength(9).HasColumnName("color");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            // Bitmask integer for recurring day-of-week selection
            entity.Property(e => e.DaysOfWeek).HasColumnName("days_of_week");

            // FK to Utilisateur; no navigation property on the "many" side (WithMany with no argument)
            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.IdUserFk)
                .HasConstraintName("user_course_id_user_fk_fkey");
        });

        // --- UserNote entity configuration ---
        modelBuilder.Entity<UserNote>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_note_pkey");
            entity.ToTable("user_note");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IdUserFk).HasColumnName("id_user_fk");
            // Stored as a PostgreSQL "date" type (no time component)
            entity.Property(e => e.Date).HasColumnType("date").HasColumnName("date");
            entity.Property(e => e.Hour).HasColumnName("hour");
            entity.Property(e => e.EndHour).HasColumnName("end_hour");
            // Text column for the note's plain-text content
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone").HasColumnName("created_at");
            entity.Property(e => e.ModifiedAt).HasColumnType("timestamp without time zone").HasColumnName("modified_at");

            // FK to Utilisateur; no navigation property on the "many" side
            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.IdUserFk)
                .HasConstraintName("user_note_id_user_fk_fkey");
        });

        // --- License entity configuration ---
        modelBuilder.Entity<License>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("license_pkey");
            entity.ToTable("license");

            // Unique index on the hashed code column.
            // Because SHA-256 hashes are always lowercase hex, LOWER(code) == code,
            // so this standard index is equivalent to a case-insensitive unique index.
            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("license_code_key");

            entity.Property(e => e.Id).HasColumnName("id");
            // Stores the SHA-256 hash of the plain license code (64 lowercase hex chars)
            entity.Property(e => e.Code).HasMaxLength(100).HasColumnName("code");
            // Defaults to true (active) when a new license is created
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone").HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnType("timestamp without time zone").HasColumnName("expires_at");
            entity.Property(e => e.Label).HasMaxLength(100).HasColumnName("label");
            entity.Property(e => e.AssignedUserId).HasColumnName("assigned_user_id");
            entity.Property(e => e.AssignedAt).HasColumnType("timestamp without time zone").HasColumnName("assigned_at");

            // Optional FK to Utilisateur: when the user is deleted, AssignedUserId is set to NULL
            entity.HasOne(e => e.AssignedUser).WithMany()
                .HasForeignKey(e => e.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("license_assigned_user_id_fkey");
        });

        // Call any additional model configuration defined in the generated partial class
        OnModelCreatingPartial(modelBuilder);
    }

    /// <summary>
    /// Partial method hook for extending the model configuration in a generated or companion class.
    /// Called at the end of OnModelCreating to allow additional Fluent API configuration
    /// without modifying this file.
    /// </summary>
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
