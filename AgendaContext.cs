// Import des types .NET de base
using System;
using System.Collections.Generic;
// Import d'Entity Framework Core pour DbContext, DbSet et le constructeur de modèle
using Microsoft.EntityFrameworkCore;
// Import de tous les modèles d'entités
using seragenda.Models;

// Espace de noms limité au fichier (style C# 10+)
namespace seragenda;

// Contexte de base de données EF Core pour l'application Agenda.
// Fournit des propriétés DbSet<T> pour chaque type d'entité mappé à la base de données PostgreSQL.
// La méthode OnModelCreating configure tous les mappages de colonnes, clés primaires, clés étrangères,
// index uniques, valeurs par défaut et propriétés de navigation via l'API Fluent.
// Cette classe est déclarée partial pour prendre en charge le code généré dans une classe partielle complémentaire
// (ex. issue du scaffolding EF Core ou de générateurs de sources).
public partial class AgendaContext : DbContext
{
    // Constructeur sans paramètre requis par les outils de conception EF Core (migrations, scaffolding).
    // Non utilisé à l'exécution quand le conteneur DI fournit les options.
    public AgendaContext()
    {
    }

    // Constructeur principal utilisé à l'exécution.
    // Le conteneur DI d'ASP.NET Core appelle ce constructeur avec les options configurées dans Program.cs.
    // options : options EF Core incluant la chaîne de connexion Npgsql
    public AgendaContext(DbContextOptions<AgendaContext> options)
        : base(options)
    {
    }

    // DbSet pour la table "abonnement" — enregistrements d'abonnement pour chaque utilisateur
    public virtual DbSet<Abonnement> Abonnements { get; set; }

    // DbSet pour la table "appartenir_visee_aptitude" — lie les objectifs de maîtrise aux compétences et aptitudes
    public virtual DbSet<AppartenirViseeAptitude> AppartenirViseeAptitudes { get; set; }

    // DbSet pour la table "aptitude" — compétences ou comportements observables spécifiques
    public virtual DbSet<Aptitude> Aptitudes { get; set; }

    // DbSet pour la table "calendrier_scolaire" — événements du calendrier scolaire (vacances, rentrée, etc.)
    public virtual DbSet<CalendrierScolaire> CalendrierScolaires { get; set; }

    // DbSet pour la table "chapitre" — chapitres de livres dans le catalogue de ressources
    public virtual DbSet<Chapitre> Chapitres { get; set; }

    // DbSet pour la table "competence" — catégories de compétences générales
    public virtual DbSet<Competence> Competences { get; set; }

    // DbSet pour la table "categorie_cours" — catégories de matières de premier niveau
    public virtual DbSet<CategorieCours> CategorieCours { get; set; }

    // DbSet pour la table "cours" — matières/cours du programme
    public virtual DbSet<Cour> Cours { get; set; }

    // DbSet pour la table "cours_niveau" — relation plusieurs-à-plusieurs entre matières, niveaux et enseignants
    public virtual DbSet<CoursNiveau> CoursNiveaus { get; set; }

    // DbSet pour la table "domaine" — domaines pédagogiques au sein d'une combinaison cours-niveau
    public virtual DbSet<Domaine> Domaines { get; set; }

    // DbSet pour la table "livre" — manuels scolaires dans le catalogue de ressources
    public virtual DbSet<Livre> Livres { get; set; }

    // DbSet pour la table "niveau" — niveaux d'enseignement (années / classes)
    public virtual DbSet<Niveau> Niveaus { get; set; }

    // DbSet pour la table "nom_visee" — dictionnaire de libellés pour les types d'objectifs d'apprentissage
    public virtual DbSet<NomVisee> NomVisees { get; set; }

    // DbSet pour la table "planification" — séances de cours planifiées
    public virtual DbSet<Planification> Planifications { get; set; }

    // DbSet pour la table "seance_objectif" — objectifs d'apprentissage ciblés dans une séance
    public virtual DbSet<SeanceObjectif> SeanceObjectifs { get; set; }

    // DbSet pour la table "seance_ressource" — chapitres de livres utilisés dans une séance
    public virtual DbSet<SeanceRessource> SeanceRessources { get; set; }

    // DbSet pour la table "sousdomaine" — sous-domaines au sein d'un domaine pédagogique
    public virtual DbSet<Sousdomaine> Sousdomaines { get; set; }

    // DbSet pour la table "utilisateur" — tous les comptes utilisateurs enregistrés
    public virtual DbSet<Utilisateur> Utilisateurs { get; set; }

    // DbSet pour la table "utilisation_chapitre" — enregistrements d'utilisation de chapitres par cours-niveau
    public virtual DbSet<UtilisationChapitre> UtilisationChapitres { get; set; }

    // DbSet pour la table "utilisation_livre" — enregistrements d'utilisation de livres par cours-niveau
    public virtual DbSet<UtilisationLivre> UtilisationLivres { get; set; }

    // DbSet pour la table "visees" — objectifs d'apprentissage individuels
    public virtual DbSet<Visee> Visees { get; set; }

    // DbSet pour la table "visees_maitriser" — objectifs de maîtrise de haut niveau
    public virtual DbSet<ViseesMaitriser> ViseesMaitrisers { get; set; }

    // DbSet pour la table "user_course" — entrées de planification de cours récurrents par utilisateur
    public virtual DbSet<UserCourse> UserCourses { get; set; }

    // DbSet pour la table "user_note" — notes personnelles horodatées dans l'agenda quotidien
    public virtual DbSet<UserNote> UserNotes { get; set; }

    // DbSet pour la table "license" — enregistrements de clés de licence gérés par les administrateurs
    public virtual DbSet<License> Licenses { get; set; }

    // Configure le modèle EF Core via l'API Fluent.
    // Mappe chaque classe d'entité à sa table et colonne de base de données correspondantes,
    // définit les clés primaires, contraintes de clés étrangères, index uniques, colonnes calculées,
    // valeurs par défaut et relations de propriétés de navigation.
    // Appelé automatiquement par EF Core lors du premier accès au modèle.
    // modelBuilder : le constructeur de modèle EF Core utilisé pour configurer les mappages d'entités
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- Configuration de l'entité Abonnement ---
        modelBuilder.Entity<Abonnement>(entity =>
        {
            // Clé primaire avec la contrainte nommée utilisée dans PostgreSQL
            entity.HasKey(e => e.IdAbonnement).HasName("abonnement_pkey");

            // Mappe vers la table PostgreSQL "abonnement"
            entity.ToTable("abonnement");

            // Mappages de noms de colonnes (snake_case en BDD → PascalCase en C#)
            entity.Property(e => e.IdAbonnement).HasColumnName("id_abonnement");
            entity.Property(e => e.DateDebut).HasColumnName("date_debut");
            entity.Property(e => e.DateFin).HasColumnName("date_fin");
            entity.Property(e => e.IdUserFk).HasColumnName("id_user_fk");
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                // Valeur par défaut PostgreSQL : 'Actif'::character varying
                .HasDefaultValueSql("'Actif'::character varying")
                .HasColumnName("statut");
            entity.Property(e => e.TypeAbo)
                .HasMaxLength(50)
                .HasColumnName("type_abo");

            // Clé étrangère : Abonnement.IdUserFk → Utilisateur.IdUser (un utilisateur a plusieurs abonnements)
            entity.HasOne(d => d.IdUserFkNavigation).WithMany(p => p.Abonnements)
                .HasForeignKey(d => d.IdUserFk)
                .HasConstraintName("abonnement_id_user_fk_fkey");
        });

        // --- Configuration de l'entité AppartenirViseeAptitude ---
        modelBuilder.Entity<AppartenirViseeAptitude>(entity =>
        {
            entity.HasKey(e => e.IdAppartenirViseeAptitude).HasName("appartenir_visee_aptitude_pkey");
            entity.ToTable("appartenir_visee_aptitude");

            entity.Property(e => e.IdAppartenirViseeAptitude).HasColumnName("id_appartenir_visee_aptitude");
            entity.Property(e => e.IdAptitudeFk).HasColumnName("id_aptitude_fk");
            entity.Property(e => e.IdCompetenceFk).HasColumnName("id_competence_fk");
            entity.Property(e => e.IdViseesMaitriserFk).HasColumnName("id_visees_maitriser_fk");

            // Clé étrangère : lien optionnel vers Aptitude (FK nullable — l'aptitude peut ne pas être définie)
            entity.HasOne(d => d.IdAptitudeFkNavigation).WithMany(p => p.AppartenirViseeAptitudes)
                .HasForeignKey(d => d.IdAptitudeFk)
                .HasConstraintName("appartenir_visee_aptitude_id_aptitude_fk_fkey");

            // Clé étrangère : lien requis vers Competence (ClientSetNull empêche la suppression en cascade)
            entity.HasOne(d => d.IdCompetenceFkNavigation).WithMany(p => p.AppartenirViseeAptitudes)
                .HasForeignKey(d => d.IdCompetenceFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("appartenir_visee_aptitude_id_competence_fk_fkey");

            // Clé étrangère : lien requis vers ViseesMaitriser (ClientSetNull empêche la suppression en cascade)
            entity.HasOne(d => d.IdViseesMaitriserFkNavigation).WithMany(p => p.AppartenirViseeAptitudes)
                .HasForeignKey(d => d.IdViseesMaitriserFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("appartenir_visee_aptitude_id_visees_maitriser_fk_fkey");
        });

        // --- Configuration de l'entité Aptitude ---
        modelBuilder.Entity<Aptitude>(entity =>
        {
            entity.HasKey(e => e.IdAptitude).HasName("aptitude_pkey");
            entity.ToTable("aptitude");

            entity.Property(e => e.IdAptitude).HasColumnName("id_aptitude");
            entity.Property(e => e.NomAptitude)
                .HasMaxLength(50)
                .HasColumnName("nom_aptitude");
        });

        // --- Configuration de l'entité CalendrierScolaire ---
        modelBuilder.Entity<CalendrierScolaire>(entity =>
        {
            entity.HasKey(e => e.IdCalendrier).HasName("calendrier_scolaire_pkey");
            entity.ToTable("calendrier_scolaire");

            entity.Property(e => e.IdCalendrier).HasColumnName("id_calendrier");
            // AnneeScolaire est une colonne GENERATED ALWAYS AS (calculée) dans PostgreSQL.
            // EF Core ne doit pas essayer de l'insérer/mettre à jour — d'où stored: true.
            // La formule SQL dérive la chaîne de l'année scolaire à partir du mois de début de l'événement.
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

        // --- Configuration de l'entité Chapitre ---
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

            // Clé étrangère : le chapitre appartient exactement à un livre ; les suppressions se propagent depuis Livre
            entity.HasOne(d => d.IdLivreFkNavigation).WithMany(p => p.Chapitres)
                .HasForeignKey(d => d.IdLivreFk)
                .HasConstraintName("chapitre_id_livre_fk_fkey");
        });

        // --- Configuration de l'entité Competence ---
        modelBuilder.Entity<Competence>(entity =>
        {
            entity.HasKey(e => e.IdCompetence).HasName("competence_pkey");
            entity.ToTable("competence");

            entity.Property(e => e.IdCompetence).HasColumnName("id_competence");
            entity.Property(e => e.NomCompetence)
                .HasMaxLength(50)
                .HasColumnName("nom_competence");
        });

        // --- Configuration de l'entité CategorieCours ---
        modelBuilder.Entity<CategorieCours>(entity =>
        {
            entity.HasKey(e => e.IdCat).HasName("categorie_cours_pkey");
            entity.ToTable("categorie_cours");

            entity.Property(e => e.IdCat).HasColumnName("id_cat");
            entity.Property(e => e.NomCat)
                .HasMaxLength(100)
                .HasColumnName("nom_cat");
            entity.Property(e => e.Ordre).HasColumnName("ordre");
        });

        // --- Configuration de l'entité Cour (matière) ---
        modelBuilder.Entity<Cour>(entity =>
        {
            entity.HasKey(e => e.IdCours).HasName("cours_pkey");
            entity.ToTable("cours");

            // Index unique sur CodeCours — deux matières ne peuvent pas avoir le même code
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
            entity.Property(e => e.IdCatFk).HasColumnName("id_cat_fk");

            // Clé étrangère : chaque matière appartient à une seule catégorie
            entity.HasOne(d => d.IdCatFkNavigation).WithMany(p => p.Cours)
                .HasForeignKey(d => d.IdCatFk)
                .HasConstraintName("cours_id_cat_fk_fkey");
        });

        // --- Configuration de l'entité CoursNiveau ---
        modelBuilder.Entity<CoursNiveau>(entity =>
        {
            entity.HasKey(e => e.IdCoursNiveau).HasName("cours_niveau_pkey");
            entity.ToTable("cours_niveau");

            // Index unique composite : le même enseignant ne peut pas être listé deux fois pour la même matière+niveau
            entity.HasIndex(e => new { e.IdCoursFk, e.IdNiveauFk, e.IdProfFk }, "cours_niveau_id_cours_fk_id_niveau_fk_id_prof_fk_key").IsUnique();

            entity.Property(e => e.IdCoursNiveau).HasColumnName("id_cours_niveau");
            entity.Property(e => e.IdCoursFk).HasColumnName("id_cours_fk");
            entity.Property(e => e.IdNiveauFk).HasColumnName("id_niveau_fk");
            entity.Property(e => e.IdProfFk).HasColumnName("id_prof_fk");

            // Clé étrangère vers Cour ; ClientSetNull empêche la suppression en cascade accidentelle
            entity.HasOne(d => d.IdCoursFkNavigation).WithMany(p => p.CoursNiveaus)
                .HasForeignKey(d => d.IdCoursFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cours_niveau_id_cours_fk_fkey");

            // Clé étrangère vers Niveau
            entity.HasOne(d => d.IdNiveauFkNavigation).WithMany(p => p.CoursNiveaus)
                .HasForeignKey(d => d.IdNiveauFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cours_niveau_id_niveau_fk_fkey");

            // Clé étrangère vers Utilisateur (l'enseignant) ; ClientSetNull empêche la suppression en cascade
            entity.HasOne(d => d.IdProfFkNavigation).WithMany(p => p.CoursNiveaus)
                .HasForeignKey(d => d.IdProfFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cours_niveau_id_prof_fk_fkey");
        });

        // --- Configuration de l'entité Domaine ---
        modelBuilder.Entity<Domaine>(entity =>
        {
            entity.HasKey(e => e.IdDom).HasName("domaine_pkey");
            entity.ToTable("domaine");

            entity.Property(e => e.IdDom).HasColumnName("id_dom");
            entity.Property(e => e.IdCoursNiveauFk).HasColumnName("id_cours_niveau_fk");
            entity.Property(e => e.Nom)
                .HasMaxLength(255)
                .HasColumnName("nom");

            // Clé étrangère : le domaine appartient exactement à une combinaison cours-niveau
            entity.HasOne(d => d.IdCoursNiveauFkNavigation).WithMany(p => p.Domaines)
                .HasForeignKey(d => d.IdCoursNiveauFk)
                .HasConstraintName("domaine_id_cours_niveau_fk_fkey");
        });

        // --- Configuration de l'entité Livre ---
        modelBuilder.Entity<Livre>(entity =>
        {
            entity.HasKey(e => e.IdLivre).HasName("livre_pkey");
            entity.ToTable("livre");

            // Index unique sur l'ISBN pour éviter les doublons de livres
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

        // --- Configuration de l'entité Niveau ---
        modelBuilder.Entity<Niveau>(entity =>
        {
            entity.HasKey(e => e.IdNiveau).HasName("niveau_pkey");
            entity.ToTable("niveau");

            // Index unique sur CodeNiveau (ex. "1A", "3B")
            entity.HasIndex(e => e.CodeNiveau, "niveau_code_niveau_key").IsUnique();

            entity.Property(e => e.IdNiveau).HasColumnName("id_niveau");
            entity.Property(e => e.CodeNiveau)
                .HasMaxLength(5)
                .HasColumnName("code_niveau");
            entity.Property(e => e.NomNiveau)
                .HasMaxLength(50)
                .HasColumnName("nom_niveau");
        });

        // --- Configuration de l'entité NomVisee ---
        modelBuilder.Entity<NomVisee>(entity =>
        {
            entity.HasKey(e => e.IdNomVisee).HasName("nom_visee_pkey");
            entity.ToTable("nom_visee");

            entity.Property(e => e.IdNomVisee).HasColumnName("id_nom_visee");
            // NomVisee1 est mappé à la colonne "nom_visee" (le suffixe "1" évite un conflit de nom avec la classe)
            entity.Property(e => e.NomVisee1)
                .HasMaxLength(150)
                .HasColumnName("nom_visee");
        });

        // --- Configuration de l'entité Planification ---
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
                // Les nouvelles séances ont par défaut le statut "Prévue" (planifiée)
                .HasDefaultValueSql("'Prévue'::character varying")
                .HasColumnName("statut");

            // FK optionnelle vers CalendrierScolaire (une séance peut être associée à un événement du calendrier)
            entity.HasOne(d => d.IdCalendrierFkNavigation).WithMany(p => p.Planifications)
                .HasForeignKey(d => d.IdCalendrierFk)
                .HasConstraintName("planification_id_calendrier_fk_fkey");

            // FK requise vers CoursNiveau (une séance doit appartenir à une combinaison cours-niveau)
            entity.HasOne(d => d.IdCoursNiveauFkNavigation).WithMany(p => p.Planifications)
                .HasForeignKey(d => d.IdCoursNiveauFk)
                .HasConstraintName("planification_id_cours_niveau_fk_fkey");
        });

        // --- Configuration de l'entité SeanceObjectif ---
        modelBuilder.Entity<SeanceObjectif>(entity =>
        {
            entity.HasKey(e => e.IdSeanceObj).HasName("seance_objectif_pkey");
            entity.ToTable("seance_objectif");

            entity.Property(e => e.IdSeanceObj).HasColumnName("id_seance_obj");
            // Valeur par défaut de evaluation_prevue est false (aucune évaluation prévue)
            entity.Property(e => e.EvaluationPrevue)
                .HasDefaultValue(false)
                .HasColumnName("evaluation_prevue");
            entity.Property(e => e.IdPlanningFk).HasColumnName("id_planning_fk");
            entity.Property(e => e.IdViseeFk).HasColumnName("id_visee_fk");

            // FK vers Planification : si la séance est supprimée, les objectifs de séance se propagent
            entity.HasOne(d => d.IdPlanningFkNavigation).WithMany(p => p.SeanceObjectifs)
                .HasForeignKey(d => d.IdPlanningFk)
                .HasConstraintName("seance_objectif_id_planning_fk_fkey");

            // FK vers Visee : ClientSetNull empêche la suppression d'un objectif référencé dans des séances
            entity.HasOne(d => d.IdViseeFkNavigation).WithMany(p => p.SeanceObjectifs)
                .HasForeignKey(d => d.IdViseeFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("seance_objectif_id_visee_fk_fkey");
        });

        // --- Configuration de l'entité SeanceRessource ---
        modelBuilder.Entity<SeanceRessource>(entity =>
        {
            entity.HasKey(e => e.IdSeanceRes).HasName("seance_ressource_pkey");
            entity.ToTable("seance_ressource");

            entity.Property(e => e.IdSeanceRes).HasColumnName("id_seance_res");
            entity.Property(e => e.IdChapitreFk).HasColumnName("id_chapitre_fk");
            entity.Property(e => e.IdPlanningFk).HasColumnName("id_planning_fk");

            // FK vers Chapitre : ClientSetNull empêche la suppression de chapitres utilisés dans des séances
            entity.HasOne(d => d.IdChapitreFkNavigation).WithMany(p => p.SeanceRessources)
                .HasForeignKey(d => d.IdChapitreFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("seance_ressource_id_chapitre_fk_fkey");

            // FK vers Planification : les enregistrements de ressources se propagent quand la séance est supprimée
            entity.HasOne(d => d.IdPlanningFkNavigation).WithMany(p => p.SeanceRessources)
                .HasForeignKey(d => d.IdPlanningFk)
                .HasConstraintName("seance_ressource_id_planning_fk_fkey");
        });

        // --- Configuration de l'entité Sousdomaine ---
        modelBuilder.Entity<Sousdomaine>(entity =>
        {
            entity.HasKey(e => e.IdSousDomaine).HasName("sousdomaine_pkey");
            entity.ToTable("sousdomaine");

            entity.Property(e => e.IdSousDomaine).HasColumnName("id_sous_domaine");
            entity.Property(e => e.IdDomFk).HasColumnName("id_dom_fk");
            entity.Property(e => e.NomComp)
                .HasMaxLength(50)
                .HasColumnName("nom_comp");

            // FK vers Domaine : un sous-domaine appartient exactement à un domaine parent
            entity.HasOne(d => d.IdDomFkNavigation).WithMany(p => p.Sousdomaines)
                .HasForeignKey(d => d.IdDomFk)
                .HasConstraintName("sousdomaine_id_dom_fk_fkey");
        });

        // --- Configuration de l'entité Utilisateur ---
        modelBuilder.Entity<Utilisateur>(entity =>
        {
            entity.HasKey(e => e.IdUser).HasName("utilisateur_pkey");
            entity.ToTable("utilisateur");

            // Index unique sur email pour imposer un compte par adresse e-mail
            entity.HasIndex(e => e.Email, "utilisateur_email_key").IsUnique();

            entity.Property(e => e.IdUser).HasColumnName("id_user");
            // CreatedAt utilise par défaut CURRENT_TIMESTAMP côté serveur ; stocké comme timestamp sans fuseau horaire
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
            // RoleSysteme vaut par défaut "PROF" pour tous les nouveaux comptes créés
            entity.Property(e => e.RoleSysteme)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PROF'::character varying")
                .HasColumnName("role_systeme");
            // Nom du fournisseur OAuth (ex. "Google", "Microsoft") ; null pour les comptes locaux e-mail/mot de passe
            entity.Property(e => e.AuthProvider)
                .HasMaxLength(50)
                .HasColumnName("auth_provider");
            // is_confirmed vaut par défaut false ; mis à true après confirmation par e-mail
            entity.Property(e => e.IsConfirmed)
                .HasColumnName("is_confirmed")
                .HasDefaultValue(false);
            // Jeton de confirmation unique envoyé à l'adresse e-mail de l'utilisateur
            entity.Property(e => e.ConfirmationToken)
                .HasMaxLength(100)
                .HasColumnName("confirmation_token");
            // Horodatage d'expiration du jeton ; stocké sans fuseau horaire (UTC supposé par convention)
            entity.Property(e => e.ConfirmationTokenExpiresAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("confirmation_token_expires_at");
        });

        // --- Configuration de l'entité UtilisationChapitre ---
        modelBuilder.Entity<UtilisationChapitre>(entity =>
        {
            entity.HasKey(e => e.IdUtilisation).HasName("utilisation_chapitre_pkey");
            entity.ToTable("utilisation_chapitre");

            entity.Property(e => e.IdUtilisation).HasColumnName("id_utilisation");
            entity.Property(e => e.IdChapitreFk).HasColumnName("id_chapitre_fk");
            entity.Property(e => e.IdCoursNiveauFk).HasColumnName("id_cours_niveau_fk");
            entity.Property(e => e.Statut)
                .HasMaxLength(50)
                // Les nouveaux enregistrements ont par défaut le statut "Recommandé"
                .HasDefaultValueSql("'Recommandé'::character varying")
                .HasColumnName("statut");

            // FK vers Chapitre : ClientSetNull empêche la suppression de chapitres référencés ici
            entity.HasOne(d => d.IdChapitreFkNavigation).WithMany(p => p.UtilisationChapitres)
                .HasForeignKey(d => d.IdChapitreFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("utilisation_chapitre_id_chapitre_fk_fkey");

            // FK vers CoursNiveau : l'utilisation de chapitre est limitée à une combinaison cours-niveau
            entity.HasOne(d => d.IdCoursNiveauFkNavigation).WithMany(p => p.UtilisationChapitres)
                .HasForeignKey(d => d.IdCoursNiveauFk)
                .HasConstraintName("utilisation_chapitre_id_cours_niveau_fk_fkey");
        });

        // --- Configuration de l'entité UtilisationLivre ---
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

            // FK vers CoursNiveau : l'utilisation de livre est limitée à une combinaison cours-niveau
            entity.HasOne(d => d.IdCoursNiveauFkNavigation).WithMany(p => p.UtilisationLivres)
                .HasForeignKey(d => d.IdCoursNiveauFk)
                .HasConstraintName("utilisation_livre_id_cours_niveau_fk_fkey");

            // FK vers Livre : ClientSetNull empêche la suppression de livres référencés ici
            entity.HasOne(d => d.IdLivreFkNavigation).WithMany(p => p.UtilisationLivres)
                .HasForeignKey(d => d.IdLivreFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("utilisation_livre_id_livre_fk_fkey");
        });

        // --- Configuration de l'entité Visee ---
        modelBuilder.Entity<Visee>(entity =>
        {
            entity.HasKey(e => e.IdVisee).HasName("visees_pkey");
            // Stockée dans la table "visees" (pluriel avec 's')
            entity.ToTable("visees");

            entity.Property(e => e.IdVisee).HasColumnName("id_visee");
            entity.Property(e => e.IdCompFk).HasColumnName("id_comp_fk");
            entity.Property(e => e.IdDomaineFk).HasColumnName("id_domaine_fk");
            entity.Property(e => e.IdNomViseeFk).HasColumnName("id_nom_visee_fk");
            entity.Property(e => e.IdSousDomaineFk).HasColumnName("id_sous_domaine_fk");

            // FK vers Competence : ClientSetNull empêche la suppression de compétences ayant des objectifs
            entity.HasOne(d => d.IdCompFkNavigation).WithMany(p => p.Visees)
                .HasForeignKey(d => d.IdCompFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("visees_id_comp_fk_fkey");

            // FK vers Domaine
            entity.HasOne(d => d.IdDomaineFkNavigation).WithMany(p => p.Visees)
                .HasForeignKey(d => d.IdDomaineFk)
                .HasConstraintName("visees_id_domaine_fk_fkey");

            // FK vers NomVisee (le type/libellé de cet objectif)
            entity.HasOne(d => d.IdNomViseeFkNavigation).WithMany(p => p.Visees)
                .HasForeignKey(d => d.IdNomViseeFk)
                .HasConstraintName("visees_id_nom_visee_fk_fkey");

            // FK optionnelle vers Sousdomaine (le sous-domaine n'est pas obligatoire)
            entity.HasOne(d => d.IdSousDomaineFkNavigation).WithMany(p => p.Visees)
                .HasForeignKey(d => d.IdSousDomaineFk)
                .HasConstraintName("visees_id_sous_domaine_fk_fkey");

            // Relation plusieurs-à-plusieurs entre Visee et ViseesMaitriser via la table de jonction "lien_visee_maitrise".
            // EF Core gère cela comme une entité fantôme sans classe C# explicite.
            entity.HasMany(d => d.IdViseesMaitriserFks).WithMany(p => p.IdViseeFks)
                .UsingEntity<Dictionary<string, object>>(
                    "LienViseeMaitrise",
                    // Côté droit : chaque ligne de jointure référence un ViseesMaitriser
                    r => r.HasOne<ViseesMaitriser>().WithMany()
                        .HasForeignKey("IdViseesMaitriserFk")
                        .HasConstraintName("lien_visee_maitrise_id_visees_maitriser_fk_fkey"),
                    // Côté gauche : chaque ligne de jointure référence un Visee
                    l => l.HasOne<Visee>().WithMany()
                        .HasForeignKey("IdViseeFk")
                        .HasConstraintName("lien_visee_maitrise_id_visee_fk_fkey"),
                    // Configuration de la table de jointure
                    j =>
                    {
                        // Clé primaire composite de la table de jointure
                        j.HasKey("IdViseeFk", "IdViseesMaitriserFk").HasName("lien_visee_maitrise_pkey");
                        j.ToTable("lien_visee_maitrise");
                        j.IndexerProperty<int>("IdViseeFk").HasColumnName("id_visee_fk");
                        j.IndexerProperty<int>("IdViseesMaitriserFk").HasColumnName("id_visees_maitriser_fk");
                    });
        });

        // --- Configuration de l'entité ViseesMaitriser ---
        modelBuilder.Entity<ViseesMaitriser>(entity =>
        {
            entity.HasKey(e => e.IdViseesMaitriser).HasName("visees_maitriser_pkey");
            entity.ToTable("visees_maitriser");

            entity.Property(e => e.IdViseesMaitriser).HasColumnName("id_visees_maitriser");
            // Colonne texte sans longueur maximale explicite — peut stocker de longues descriptions d'objectifs de maîtrise
            entity.Property(e => e.NomViseesMaitriser).HasColumnName("nom_visees_maitriser");
        });

        // --- Configuration de l'entité UserCourse ---
        modelBuilder.Entity<UserCourse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_course_pkey");
            entity.ToTable("user_course");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IdUserFk).HasColumnName("id_user_fk");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            // Code couleur hexadécimal (max "#RRGGBBAA" = 9 caractères dont le '#')
            entity.Property(e => e.Color).HasMaxLength(9).HasColumnName("color");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            // Entier bitmask pour la sélection des jours de la semaine récurrents
            entity.Property(e => e.DaysOfWeek).HasColumnName("days_of_week");

            // FK vers Utilisateur ; pas de propriété de navigation côté "many" (WithMany sans argument)
            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.IdUserFk)
                .HasConstraintName("user_course_id_user_fk_fkey");
        });

        // --- Configuration de l'entité UserNote ---
        modelBuilder.Entity<UserNote>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_note_pkey");
            entity.ToTable("user_note");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IdUserFk).HasColumnName("id_user_fk");
            // Stocké comme type PostgreSQL "date" (sans composante horaire)
            entity.Property(e => e.Date).HasColumnType("date").HasColumnName("date");
            entity.Property(e => e.Hour).HasColumnName("hour");
            entity.Property(e => e.EndHour).HasColumnName("end_hour");
            // Colonne texte pour le contenu en texte brut de la note
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone").HasColumnName("created_at");
            entity.Property(e => e.ModifiedAt).HasColumnType("timestamp without time zone").HasColumnName("modified_at");

            // FK vers Utilisateur ; pas de propriété de navigation côté "many"
            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.IdUserFk)
                .HasConstraintName("user_note_id_user_fk_fkey");
        });

        // --- Configuration de l'entité License ---
        modelBuilder.Entity<License>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("license_pkey");
            entity.ToTable("license");

            // Index unique sur la colonne du code haché.
            // Comme les hachages SHA-256 sont toujours en hex minuscule, LOWER(code) == code,
            // donc cet index standard est équivalent à un index unique insensible à la casse.
            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("license_code_key");

            entity.Property(e => e.Id).HasColumnName("id");
            // Stocke le hachage SHA-256 du code de licence en clair (64 caractères hex minuscules)
            entity.Property(e => e.Code).HasMaxLength(100).HasColumnName("code");
            // Vaut par défaut true (actif) quand une nouvelle licence est créée
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone").HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnType("timestamp without time zone").HasColumnName("expires_at");
            entity.Property(e => e.Label).HasMaxLength(100).HasColumnName("label");
            entity.Property(e => e.AssignedUserId).HasColumnName("assigned_user_id");
            entity.Property(e => e.AssignedAt).HasColumnType("timestamp without time zone").HasColumnName("assigned_at");

            // FK optionnelle vers Utilisateur : quand l'utilisateur est supprimé, AssignedUserId est mis à NULL
            entity.HasOne(e => e.AssignedUser).WithMany()
                .HasForeignKey(e => e.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("license_assigned_user_id_fkey");
        });

        // Appel de toute configuration de modèle supplémentaire définie dans la classe partielle générée
        OnModelCreatingPartial(modelBuilder);
    }

    // Méthode partielle hook pour étendre la configuration du modèle dans une classe générée ou complémentaire.
    // Appelée à la fin de OnModelCreating pour permettre une configuration Fluent API supplémentaire
    // sans modifier ce fichier.
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
