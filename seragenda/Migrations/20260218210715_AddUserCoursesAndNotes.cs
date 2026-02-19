using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace seragenda.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCoursesAndNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "aptitude",
                columns: table => new
                {
                    id_aptitude = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nom_aptitude = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("aptitude_pkey", x => x.id_aptitude);
                });

            migrationBuilder.CreateTable(
                name: "calendrier_scolaire",
                columns: table => new
                {
                    id_calendrier = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nom_evenement = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    date_debut = table.Column<DateOnly>(type: "date", nullable: false),
                    date_fin = table.Column<DateOnly>(type: "date", nullable: false),
                    type_evenement = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    annee_scolaire = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true, computedColumnSql: "\nCASE\n    WHEN (EXTRACT(month FROM date_debut) >= (8)::numeric) THEN (((EXTRACT(year FROM date_debut))::text || '-'::text) || ((EXTRACT(year FROM date_debut) + (1)::numeric))::text)\n    ELSE ((((EXTRACT(year FROM date_debut) - (1)::numeric))::text || '-'::text) || (EXTRACT(year FROM date_debut))::text)\nEND", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("calendrier_scolaire_pkey", x => x.id_calendrier);
                });

            migrationBuilder.CreateTable(
                name: "competence",
                columns: table => new
                {
                    id_competence = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nom_competence = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("competence_pkey", x => x.id_competence);
                });

            migrationBuilder.CreateTable(
                name: "cours",
                columns: table => new
                {
                    id_cours = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nom_cours = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code_cours = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    prefix_cours = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    couleur_agenda = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("cours_pkey", x => x.id_cours);
                });

            migrationBuilder.CreateTable(
                name: "livre",
                columns: table => new
                {
                    id_livre = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    titre_livre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    auteur = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    isbn = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    maison_edition = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("livre_pkey", x => x.id_livre);
                });

            migrationBuilder.CreateTable(
                name: "niveau",
                columns: table => new
                {
                    id_niveau = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code_niveau = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    nom_niveau = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("niveau_pkey", x => x.id_niveau);
                });

            migrationBuilder.CreateTable(
                name: "nom_visee",
                columns: table => new
                {
                    id_nom_visee = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nom_visee = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("nom_visee_pkey", x => x.id_nom_visee);
                });

            migrationBuilder.CreateTable(
                name: "utilisateur",
                columns: table => new
                {
                    id_user = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    nom_complet = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    nom = table.Column<string>(type: "text", nullable: true),
                    prenom = table.Column<string>(type: "text", nullable: true),
                    age = table.Column<int>(type: "integer", nullable: true),
                    role_systeme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'PROF'::character varying"),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    auth_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("utilisateur_pkey", x => x.id_user);
                });

            migrationBuilder.CreateTable(
                name: "visees_maitriser",
                columns: table => new
                {
                    id_visees_maitriser = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nom_visees_maitriser = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("visees_maitriser_pkey", x => x.id_visees_maitriser);
                });

            migrationBuilder.CreateTable(
                name: "chapitre",
                columns: table => new
                {
                    id_chapitre = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_livre_fk = table.Column<int>(type: "integer", nullable: false),
                    numero_chapitre = table.Column<int>(type: "integer", nullable: false),
                    titre_chapitre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    page_debut = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("chapitre_pkey", x => x.id_chapitre);
                    table.ForeignKey(
                        name: "chapitre_id_livre_fk_fkey",
                        column: x => x.id_livre_fk,
                        principalTable: "livre",
                        principalColumn: "id_livre",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "abonnement",
                columns: table => new
                {
                    id_abonnement = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_user_fk = table.Column<int>(type: "integer", nullable: false),
                    date_debut = table.Column<DateOnly>(type: "date", nullable: false),
                    date_fin = table.Column<DateOnly>(type: "date", nullable: false),
                    type_abo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    statut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValueSql: "'Actif'::character varying")
                },
                constraints: table =>
                {
                    table.PrimaryKey("abonnement_pkey", x => x.id_abonnement);
                    table.ForeignKey(
                        name: "abonnement_id_user_fk_fkey",
                        column: x => x.id_user_fk,
                        principalTable: "utilisateur",
                        principalColumn: "id_user",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cours_niveau",
                columns: table => new
                {
                    id_cours_niveau = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_cours_fk = table.Column<int>(type: "integer", nullable: false),
                    id_niveau_fk = table.Column<int>(type: "integer", nullable: false),
                    id_prof_fk = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("cours_niveau_pkey", x => x.id_cours_niveau);
                    table.ForeignKey(
                        name: "cours_niveau_id_cours_fk_fkey",
                        column: x => x.id_cours_fk,
                        principalTable: "cours",
                        principalColumn: "id_cours");
                    table.ForeignKey(
                        name: "cours_niveau_id_niveau_fk_fkey",
                        column: x => x.id_niveau_fk,
                        principalTable: "niveau",
                        principalColumn: "id_niveau");
                    table.ForeignKey(
                        name: "cours_niveau_id_prof_fk_fkey",
                        column: x => x.id_prof_fk,
                        principalTable: "utilisateur",
                        principalColumn: "id_user");
                });

            migrationBuilder.CreateTable(
                name: "user_course",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_user_fk = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    days_of_week = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_course_pkey", x => x.id);
                    table.ForeignKey(
                        name: "user_course_id_user_fk_fkey",
                        column: x => x.id_user_fk,
                        principalTable: "utilisateur",
                        principalColumn: "id_user",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_note",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_user_fk = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    hour = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_note_pkey", x => x.id);
                    table.ForeignKey(
                        name: "user_note_id_user_fk_fkey",
                        column: x => x.id_user_fk,
                        principalTable: "utilisateur",
                        principalColumn: "id_user",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "appartenir_visee_aptitude",
                columns: table => new
                {
                    id_appartenir_visee_aptitude = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_aptitude_fk = table.Column<int>(type: "integer", nullable: true),
                    id_visees_maitriser_fk = table.Column<int>(type: "integer", nullable: false),
                    id_competence_fk = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("appartenir_visee_aptitude_pkey", x => x.id_appartenir_visee_aptitude);
                    table.ForeignKey(
                        name: "appartenir_visee_aptitude_id_aptitude_fk_fkey",
                        column: x => x.id_aptitude_fk,
                        principalTable: "aptitude",
                        principalColumn: "id_aptitude");
                    table.ForeignKey(
                        name: "appartenir_visee_aptitude_id_competence_fk_fkey",
                        column: x => x.id_competence_fk,
                        principalTable: "competence",
                        principalColumn: "id_competence");
                    table.ForeignKey(
                        name: "appartenir_visee_aptitude_id_visees_maitriser_fk_fkey",
                        column: x => x.id_visees_maitriser_fk,
                        principalTable: "visees_maitriser",
                        principalColumn: "id_visees_maitriser");
                });

            migrationBuilder.CreateTable(
                name: "domaine",
                columns: table => new
                {
                    id_dom = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nom = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    id_cours_niveau_fk = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("domaine_pkey", x => x.id_dom);
                    table.ForeignKey(
                        name: "domaine_id_cours_niveau_fk_fkey",
                        column: x => x.id_cours_niveau_fk,
                        principalTable: "cours_niveau",
                        principalColumn: "id_cours_niveau",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "planification",
                columns: table => new
                {
                    id_planning = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_seance = table.Column<DateOnly>(type: "date", nullable: false),
                    heure_debut = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    heure_fin = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    id_cours_niveau_fk = table.Column<int>(type: "integer", nullable: false),
                    id_calendrier_fk = table.Column<int>(type: "integer", nullable: true),
                    note_prof = table.Column<string>(type: "text", nullable: true),
                    statut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValueSql: "'Prévue'::character varying")
                },
                constraints: table =>
                {
                    table.PrimaryKey("planification_pkey", x => x.id_planning);
                    table.ForeignKey(
                        name: "planification_id_calendrier_fk_fkey",
                        column: x => x.id_calendrier_fk,
                        principalTable: "calendrier_scolaire",
                        principalColumn: "id_calendrier");
                    table.ForeignKey(
                        name: "planification_id_cours_niveau_fk_fkey",
                        column: x => x.id_cours_niveau_fk,
                        principalTable: "cours_niveau",
                        principalColumn: "id_cours_niveau",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "utilisation_chapitre",
                columns: table => new
                {
                    id_utilisation = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_chapitre_fk = table.Column<int>(type: "integer", nullable: false),
                    id_cours_niveau_fk = table.Column<int>(type: "integer", nullable: false),
                    statut = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, defaultValueSql: "'Recommandé'::character varying")
                },
                constraints: table =>
                {
                    table.PrimaryKey("utilisation_chapitre_pkey", x => x.id_utilisation);
                    table.ForeignKey(
                        name: "utilisation_chapitre_id_chapitre_fk_fkey",
                        column: x => x.id_chapitre_fk,
                        principalTable: "chapitre",
                        principalColumn: "id_chapitre");
                    table.ForeignKey(
                        name: "utilisation_chapitre_id_cours_niveau_fk_fkey",
                        column: x => x.id_cours_niveau_fk,
                        principalTable: "cours_niveau",
                        principalColumn: "id_cours_niveau",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "utilisation_livre",
                columns: table => new
                {
                    id_utilisation = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_livre_fk = table.Column<int>(type: "integer", nullable: false),
                    id_cours_niveau_fk = table.Column<int>(type: "integer", nullable: false),
                    statut = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, defaultValueSql: "'Recommandé'::character varying")
                },
                constraints: table =>
                {
                    table.PrimaryKey("utilisation_livre_pkey", x => x.id_utilisation);
                    table.ForeignKey(
                        name: "utilisation_livre_id_cours_niveau_fk_fkey",
                        column: x => x.id_cours_niveau_fk,
                        principalTable: "cours_niveau",
                        principalColumn: "id_cours_niveau",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "utilisation_livre_id_livre_fk_fkey",
                        column: x => x.id_livre_fk,
                        principalTable: "livre",
                        principalColumn: "id_livre");
                });

            migrationBuilder.CreateTable(
                name: "sousdomaine",
                columns: table => new
                {
                    id_sous_domaine = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nom_comp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    id_dom_fk = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sousdomaine_pkey", x => x.id_sous_domaine);
                    table.ForeignKey(
                        name: "sousdomaine_id_dom_fk_fkey",
                        column: x => x.id_dom_fk,
                        principalTable: "domaine",
                        principalColumn: "id_dom",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seance_ressource",
                columns: table => new
                {
                    id_seance_res = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_planning_fk = table.Column<int>(type: "integer", nullable: false),
                    id_chapitre_fk = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("seance_ressource_pkey", x => x.id_seance_res);
                    table.ForeignKey(
                        name: "seance_ressource_id_chapitre_fk_fkey",
                        column: x => x.id_chapitre_fk,
                        principalTable: "chapitre",
                        principalColumn: "id_chapitre");
                    table.ForeignKey(
                        name: "seance_ressource_id_planning_fk_fkey",
                        column: x => x.id_planning_fk,
                        principalTable: "planification",
                        principalColumn: "id_planning",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "visees",
                columns: table => new
                {
                    id_visee = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_nom_visee_fk = table.Column<int>(type: "integer", nullable: false),
                    id_domaine_fk = table.Column<int>(type: "integer", nullable: false),
                    id_sous_domaine_fk = table.Column<int>(type: "integer", nullable: true),
                    id_comp_fk = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("visees_pkey", x => x.id_visee);
                    table.ForeignKey(
                        name: "visees_id_comp_fk_fkey",
                        column: x => x.id_comp_fk,
                        principalTable: "competence",
                        principalColumn: "id_competence");
                    table.ForeignKey(
                        name: "visees_id_domaine_fk_fkey",
                        column: x => x.id_domaine_fk,
                        principalTable: "domaine",
                        principalColumn: "id_dom",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "visees_id_nom_visee_fk_fkey",
                        column: x => x.id_nom_visee_fk,
                        principalTable: "nom_visee",
                        principalColumn: "id_nom_visee",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "visees_id_sous_domaine_fk_fkey",
                        column: x => x.id_sous_domaine_fk,
                        principalTable: "sousdomaine",
                        principalColumn: "id_sous_domaine");
                });

            migrationBuilder.CreateTable(
                name: "lien_visee_maitrise",
                columns: table => new
                {
                    id_visee_fk = table.Column<int>(type: "integer", nullable: false),
                    id_visees_maitriser_fk = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("lien_visee_maitrise_pkey", x => new { x.id_visee_fk, x.id_visees_maitriser_fk });
                    table.ForeignKey(
                        name: "lien_visee_maitrise_id_visee_fk_fkey",
                        column: x => x.id_visee_fk,
                        principalTable: "visees",
                        principalColumn: "id_visee",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "lien_visee_maitrise_id_visees_maitriser_fk_fkey",
                        column: x => x.id_visees_maitriser_fk,
                        principalTable: "visees_maitriser",
                        principalColumn: "id_visees_maitriser",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seance_objectif",
                columns: table => new
                {
                    id_seance_obj = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_planning_fk = table.Column<int>(type: "integer", nullable: false),
                    id_visee_fk = table.Column<int>(type: "integer", nullable: false),
                    evaluation_prevue = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("seance_objectif_pkey", x => x.id_seance_obj);
                    table.ForeignKey(
                        name: "seance_objectif_id_planning_fk_fkey",
                        column: x => x.id_planning_fk,
                        principalTable: "planification",
                        principalColumn: "id_planning",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "seance_objectif_id_visee_fk_fkey",
                        column: x => x.id_visee_fk,
                        principalTable: "visees",
                        principalColumn: "id_visee");
                });

            migrationBuilder.CreateIndex(
                name: "IX_abonnement_id_user_fk",
                table: "abonnement",
                column: "id_user_fk");

            migrationBuilder.CreateIndex(
                name: "IX_appartenir_visee_aptitude_id_aptitude_fk",
                table: "appartenir_visee_aptitude",
                column: "id_aptitude_fk");

            migrationBuilder.CreateIndex(
                name: "IX_appartenir_visee_aptitude_id_competence_fk",
                table: "appartenir_visee_aptitude",
                column: "id_competence_fk");

            migrationBuilder.CreateIndex(
                name: "IX_appartenir_visee_aptitude_id_visees_maitriser_fk",
                table: "appartenir_visee_aptitude",
                column: "id_visees_maitriser_fk");

            migrationBuilder.CreateIndex(
                name: "IX_chapitre_id_livre_fk",
                table: "chapitre",
                column: "id_livre_fk");

            migrationBuilder.CreateIndex(
                name: "cours_code_cours_key",
                table: "cours",
                column: "code_cours",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "cours_niveau_id_cours_fk_id_niveau_fk_id_prof_fk_key",
                table: "cours_niveau",
                columns: new[] { "id_cours_fk", "id_niveau_fk", "id_prof_fk" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cours_niveau_id_niveau_fk",
                table: "cours_niveau",
                column: "id_niveau_fk");

            migrationBuilder.CreateIndex(
                name: "IX_cours_niveau_id_prof_fk",
                table: "cours_niveau",
                column: "id_prof_fk");

            migrationBuilder.CreateIndex(
                name: "IX_domaine_id_cours_niveau_fk",
                table: "domaine",
                column: "id_cours_niveau_fk");

            migrationBuilder.CreateIndex(
                name: "IX_lien_visee_maitrise_id_visees_maitriser_fk",
                table: "lien_visee_maitrise",
                column: "id_visees_maitriser_fk");

            migrationBuilder.CreateIndex(
                name: "livre_isbn_key",
                table: "livre",
                column: "isbn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "niveau_code_niveau_key",
                table: "niveau",
                column: "code_niveau",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_planification_id_calendrier_fk",
                table: "planification",
                column: "id_calendrier_fk");

            migrationBuilder.CreateIndex(
                name: "IX_planification_id_cours_niveau_fk",
                table: "planification",
                column: "id_cours_niveau_fk");

            migrationBuilder.CreateIndex(
                name: "IX_seance_objectif_id_planning_fk",
                table: "seance_objectif",
                column: "id_planning_fk");

            migrationBuilder.CreateIndex(
                name: "IX_seance_objectif_id_visee_fk",
                table: "seance_objectif",
                column: "id_visee_fk");

            migrationBuilder.CreateIndex(
                name: "IX_seance_ressource_id_chapitre_fk",
                table: "seance_ressource",
                column: "id_chapitre_fk");

            migrationBuilder.CreateIndex(
                name: "IX_seance_ressource_id_planning_fk",
                table: "seance_ressource",
                column: "id_planning_fk");

            migrationBuilder.CreateIndex(
                name: "IX_sousdomaine_id_dom_fk",
                table: "sousdomaine",
                column: "id_dom_fk");

            migrationBuilder.CreateIndex(
                name: "IX_user_course_id_user_fk",
                table: "user_course",
                column: "id_user_fk");

            migrationBuilder.CreateIndex(
                name: "IX_user_note_id_user_fk",
                table: "user_note",
                column: "id_user_fk");

            migrationBuilder.CreateIndex(
                name: "utilisateur_email_key",
                table: "utilisateur",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_utilisation_chapitre_id_chapitre_fk",
                table: "utilisation_chapitre",
                column: "id_chapitre_fk");

            migrationBuilder.CreateIndex(
                name: "IX_utilisation_chapitre_id_cours_niveau_fk",
                table: "utilisation_chapitre",
                column: "id_cours_niveau_fk");

            migrationBuilder.CreateIndex(
                name: "IX_utilisation_livre_id_cours_niveau_fk",
                table: "utilisation_livre",
                column: "id_cours_niveau_fk");

            migrationBuilder.CreateIndex(
                name: "IX_utilisation_livre_id_livre_fk",
                table: "utilisation_livre",
                column: "id_livre_fk");

            migrationBuilder.CreateIndex(
                name: "IX_visees_id_comp_fk",
                table: "visees",
                column: "id_comp_fk");

            migrationBuilder.CreateIndex(
                name: "IX_visees_id_domaine_fk",
                table: "visees",
                column: "id_domaine_fk");

            migrationBuilder.CreateIndex(
                name: "IX_visees_id_nom_visee_fk",
                table: "visees",
                column: "id_nom_visee_fk");

            migrationBuilder.CreateIndex(
                name: "IX_visees_id_sous_domaine_fk",
                table: "visees",
                column: "id_sous_domaine_fk");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abonnement");

            migrationBuilder.DropTable(
                name: "appartenir_visee_aptitude");

            migrationBuilder.DropTable(
                name: "lien_visee_maitrise");

            migrationBuilder.DropTable(
                name: "seance_objectif");

            migrationBuilder.DropTable(
                name: "seance_ressource");

            migrationBuilder.DropTable(
                name: "user_course");

            migrationBuilder.DropTable(
                name: "user_note");

            migrationBuilder.DropTable(
                name: "utilisation_chapitre");

            migrationBuilder.DropTable(
                name: "utilisation_livre");

            migrationBuilder.DropTable(
                name: "aptitude");

            migrationBuilder.DropTable(
                name: "visees_maitriser");

            migrationBuilder.DropTable(
                name: "visees");

            migrationBuilder.DropTable(
                name: "planification");

            migrationBuilder.DropTable(
                name: "chapitre");

            migrationBuilder.DropTable(
                name: "competence");

            migrationBuilder.DropTable(
                name: "nom_visee");

            migrationBuilder.DropTable(
                name: "sousdomaine");

            migrationBuilder.DropTable(
                name: "calendrier_scolaire");

            migrationBuilder.DropTable(
                name: "livre");

            migrationBuilder.DropTable(
                name: "domaine");

            migrationBuilder.DropTable(
                name: "cours_niveau");

            migrationBuilder.DropTable(
                name: "cours");

            migrationBuilder.DropTable(
                name: "niveau");

            migrationBuilder.DropTable(
                name: "utilisateur");
        }
    }
}
