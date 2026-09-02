// Importation des attributs d'autorisation ASP.NET Core
using Microsoft.AspNetCore.Authorization;
// Importation des types de base pour les contrôleurs MVC/API et les helpers de résultat
using Microsoft.AspNetCore.Mvc;
// Importation d'Entity Framework Core pour les requêtes asynchrones en base de données
using Microsoft.EntityFrameworkCore;
// Importation des entités du référentiel (création de visées et de liens)
using seragenda.Models;
// Importation des claims pour identifier l'enseignant connecté
using System.Security.Claims;

namespace seragenda.Controllers
{
    // Les routes de ce contrôleur sont préfixées par /api/ref (chemin explicite, sans le jeton [controller])
    [Route("api/ref")]
    // Indique que cette classe est un contrôleur d'API
    [ApiController]
    // Tous les points de terminaison nécessitent un token JWT valide
    [Authorize]
    // Fournit des données de référence en lecture seule utilisées pour alimenter les listes de sélection en cascade côté client.
    // Expose quatre lookups liés : catégories, cours (filtrés par catégorie), niveaux et domaines.
    // Utilisation prévue :
    //   1. Charger toutes les catégories → l'utilisateur en choisit une.
    //   2. Charger les cours de la catégorie sélectionnée → l'utilisateur en choisit un.
    //   3. Charger les niveaux du cours sélectionné → l'utilisateur en choisit un.
    //   4. Charger les domaines du cours + niveau sélectionnés → l'utilisateur en choisit un.
    public class RefController : ControllerBase
    {
        // Contexte de base de données Entity Framework pour interroger les tables de référence
        private readonly AgendaContext _context;

        // Constructeur — reçoit le contexte de base de données par injection de dépendances.
        // Paramètre context : le contexte de base de données EF Core
        public RefController(AgendaContext context)
        {
            _context = context;
        }

        // GET /api/ref/categories
        // Retourne toutes les catégories de matières, triées par leur ordre d'affichage
        [HttpGet("categories")]
        // Récupère tous les enregistrements de catégories depuis la table categorie_cours.
        // Retourne l'identifiant, le nom et l'ordre de tri pour chaque catégorie.
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.CategorieCours
                // Trier par la colonne d'ordre explicite pour que la liste déroulante corresponde à la séquence prévue
                .OrderBy(c => c.Ordre)
                .Select(c => new { c.IdCat, c.NomCat, c.Ordre })
                .ToListAsync();

            return Ok(categories);
        }

        // GET /api/ref/cours/{idCat}
        // Retourne tous les cours appartenant à une catégorie spécifique, triés alphabétiquement
        [HttpGet("cours/{idCat:int}")]
        // Récupère tous les enregistrements de cours (matières) appartenant à une catégorie donnée.
        // Retourne uniquement les champs nécessaires à l'affichage et aux lookups suivants :
        // le code unique, le nom d'affichage et la couleur de l'agenda.
        // Paramètre idCat : la clé primaire de la catégorie à filtrer
        public async Task<IActionResult> GetCours(int idCat)
        {
            var cours = await _context.Cours
                // Filtrer uniquement les matières appartenant à la catégorie demandée
                .Where(c => c.IdCatFk == idCat)
                // Trier alphabétiquement pour que la liste déroulante soit ordonnée
                .OrderBy(c => c.NomCours)
                // Projeter uniquement les colonnes dont le client a besoin ; évite la sur-récupération
                .Select(c => new { c.CodeCours, c.NomCours, c.CouleurAgenda })
                .ToListAsync();

            return Ok(cours);
        }

        // GET /api/ref/niveaux/{codeCours}
        // Retourne les niveaux d'enseignement distincts disponibles pour un code de cours donné
        [HttpGet("niveaux/{codeCours}")]
        // Récupère tous les niveaux scolaires (années) associés à un cours spécifique.
        // La liste des niveaux est dérivée de la table de liaison many-to-many CoursNiveau.
        // Les doublons sont supprimés avec Distinct() au cas où plusieurs professeurs enseignent au même niveau.
        // Paramètre codeCours : le code unique du cours (ex. : "MATH", "FR")
        public async Task<IActionResult> GetNiveaux(string codeCours)
        {
            var niveaux = await _context.CoursNiveaus
                // Filtrer les enregistrements de liaison où le cours associé correspond au code demandé
                .Where(cn => cn.IdCoursFkNavigation.CodeCours == codeCours)
                // Naviguer à travers la table de liaison vers l'entité Niveau
                .Select(cn => cn.IdNiveauFkNavigation)
                // Supprimer les doublons générés par plusieurs professeurs enseignant au même niveau
                .Distinct()
                // Trier par le code de niveau pour un ordre cohérent
                .OrderBy(n => n.CodeNiveau)
                // Projeter uniquement les champs nécessaires à l'affichage et aux lookups ultérieurs
                .Select(n => new { n.CodeNiveau, n.NomNiveau })
                .ToListAsync();

            return Ok(niveaux);
        }

        // GET /api/ref/niveaux
        // Retourne tous les niveaux d'enseignement qui possèdent au moins un domaine
        // renseigné (avec des visées). Alimente la PREMIÈRE liste déroulante de la
        // cascade réordonnée où l'année (niveau) est choisie en premier.
        [HttpGet("niveaux")]
        public async Task<IActionResult> GetNiveauxTous()
        {
            var moi = await GetUserId() ?? 0;

            var niveaux = await _context.CoursNiveaus
                // Ne garder que les niveaux dont au moins une combinaison cours-niveau
                // possède des domaines contenant des visées (évite les branches vides).
                // Les liaisons de l'enseignant connecté sont gardées même sans visée :
                // sans cela, ce qu'il vient de créer n'apparaîtrait jamais.
                .Where(cn => cn.Domaines.Any(d => d.Visees.Any()) || cn.IdProfFk == moi)
                // Naviguer vers l'entité Niveau à travers la table de liaison
                .Select(cn => cn.IdNiveauFkNavigation)
                // Supprimer les doublons (plusieurs cours/professeurs partagent un même niveau)
                .Distinct()
                .OrderBy(n => n.CodeNiveau)
                .Select(n => new { n.CodeNiveau, n.NomNiveau })
                .ToListAsync();

            return Ok(niveaux);
        }

        // GET /api/ref/categories/by-niveau/{codeNiveau}
        // Retourne les catégories possédant au moins un cours enseigné au niveau donné
        // (et dont la combinaison cours-niveau contient des domaines avec visées).
        // Deuxième étape de la cascade réordonnée : Année → Catégorie.
        [HttpGet("categories/by-niveau/{codeNiveau}")]
        public async Task<IActionResult> GetCategoriesByNiveau(string codeNiveau)
        {
            var moi = await GetUserId() ?? 0;

            var categories = await _context.CategorieCours
                .Where(cat => cat.Cours.Any(co => co.CoursNiveaus.Any(cn =>
                    cn.IdNiveauFkNavigation.CodeNiveau == codeNiveau &&
                    (cn.Domaines.Any(d => d.Visees.Any()) || cn.IdProfFk == moi))))
                .OrderBy(cat => cat.Ordre)
                .Select(cat => new { cat.IdCat, cat.NomCat, cat.Ordre })
                .ToListAsync();

            return Ok(categories);
        }

        // GET /api/ref/cours/by-cat-niveau/{idCat}/{codeNiveau}
        // Retourne les cours d'une catégorie enseignés à un niveau donné (avec visées).
        // Troisième étape de la cascade réordonnée : Année → Catégorie → Cours.
        [HttpGet("cours/by-cat-niveau/{idCat:int}/{codeNiveau}")]
        public async Task<IActionResult> GetCoursByCatNiveau(int idCat, string codeNiveau)
        {
            var moi = await GetUserId() ?? 0;

            var cours = await _context.Cours
                .Where(c => c.IdCatFk == idCat &&
                    c.CoursNiveaus.Any(cn =>
                        cn.IdNiveauFkNavigation.CodeNiveau == codeNiveau &&
                        (cn.Domaines.Any(d => d.Visees.Any()) || cn.IdProfFk == moi)))
                .OrderBy(c => c.NomCours)
                .Select(c => new { c.CodeCours, c.NomCours, c.CouleurAgenda })
                .ToListAsync();

            return Ok(cours);
        }

        // GET /api/ref/domaines/{codeCours}/{codeNiveau}
        // Retourne les domaines pour une combinaison cours + niveau donnée
        [HttpGet("domaines/{codeCours}/{codeNiveau}")]
        // Récupère tous les domaines pédagogiques pour une combinaison cours et niveau spécifique.
        // Paramètre codeCours : le code unique du cours
        // Paramètre codeNiveau : le code unique du niveau scolaire
        public async Task<IActionResult> GetDomaines(string codeCours, string codeNiveau)
        {
            var moi = await GetUserId() ?? 0;

            var domaines = await _context.CoursNiveaus
                .Where(cn =>
                    cn.IdCoursFkNavigation.CodeCours   == codeCours &&
                    cn.IdNiveauFkNavigation.CodeNiveau == codeNiveau)
                .SelectMany(cn => cn.Domaines)
                // Un champ sans visée reste proposé à son propre enseignant : c'est le
                // point de départ obligé quand il vient de le créer.
                .Where(d => d.Visees.Any() || d.IdCoursNiveauFkNavigation.IdProfFk == moi)
                .OrderBy(d => d.Nom)
                .Select(d => new { d.IdDom, d.Nom })
                .ToListAsync();

            return Ok(domaines);
        }

        // GET /api/ref/sous-domaines/{idDomaine}
        // Retourne les sous-domaines rattachés à un domaine donné
        [HttpGet("sous-domaines/{idDomaine:int}")]
        public async Task<IActionResult> GetSousDomaines(int idDomaine)
        {
            var list = await _context.Sousdomaines
                .Where(s => s.IdDomFk == idDomaine)
                .OrderBy(s => s.NomComp)
                .Select(s => new { s.IdSousDomaine, s.NomComp })
                .ToListAsync();

            return Ok(list);
        }

        // GET /api/ref/visees/{idDomaine}?sousDomaine={idSousDomaine}
        // Retourne les visées d'un domaine, filtrées optionnellement par sous-domaine
        [HttpGet("visees/{idDomaine:int}")]
        public async Task<IActionResult> GetVisees(int idDomaine, [FromQuery] int? sousDomaine)
        {
            var query = _context.Visees
                .Include(v => v.IdNomViseeFkNavigation)
                .Include(v => v.IdCompFkNavigation)
                .Where(v => v.IdDomaineFk == idDomaine);

            if (sousDomaine.HasValue && sousDomaine.Value > 0)
                query = query.Where(v => v.IdSousDomaineFk == sousDomaine.Value);

            var list = await query
                .OrderBy(v => v.IdNomViseeFkNavigation.NomVisee1)
                .ThenBy(v => v.IdCompFkNavigation.NomCompetence)
                .Select(v => new
                {
                    v.IdVisee,
                    IdNomVisee    = v.IdNomViseeFk,
                    NomVisee      = v.IdNomViseeFkNavigation.NomVisee1,
                    IdCompetence  = v.IdCompFk,
                    NomCompetence = v.IdCompFkNavigation.NomCompetence,
                    Label         = v.IdNomViseeFkNavigation.NomVisee1 + " — " + v.IdCompFkNavigation.NomCompetence
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET /api/ref/visees-maitriser/{idVisee}
        // Retourne les visées à maîtriser liées à une visée donnée (via la table de jointure many-to-many)
        [HttpGet("visees-maitriser/{idVisee:int}")]
        public async Task<IActionResult> GetViseesMaitriser(int idVisee)
        {
            var visee = await _context.Visees
                .Include(v => v.IdViseesMaitriserFks)
                .FirstOrDefaultAsync(v => v.IdVisee == idVisee);

            if (visee == null) return NotFound();

            var list = visee.IdViseesMaitriserFks
                .OrderBy(vm => vm.NomViseesMaitriser)
                .Select(vm => new { vm.IdViseesMaitriser, vm.NomViseesMaitriser })
                .ToList();

            return Ok(list);
        }

        // GET /api/ref/appartenir/{idVm}?idVisee={idVisee}
        // Retourne les entrées appartenir_visee_aptitude d'une visée à maîtriser.
        // Si aucune entrée n'existe et qu'une visée est fournie, retourne la compétence
        // de cette visée comme "Attendus" de repli (IdAppartenirViseeAptitude négatif = -IdCompetence).
        [HttpGet("appartenir/{idVm:int}")]
        public async Task<IActionResult> GetAppartenir(int idVm, [FromQuery] int? idVisee)
        {
            var list = await _context.AppartenirViseeAptitudes
                .Include(a => a.IdAptitudeFkNavigation)
                .Include(a => a.IdCompetenceFkNavigation)
                .Where(a => a.IdViseesMaitriserFk == idVm)
                .OrderBy(a => a.IdAptitudeFkNavigation != null ? a.IdAptitudeFkNavigation.NomAptitude : "")
                .Select(a => new
                {
                    a.IdAppartenirViseeAptitude,
                    IdAptitude    = a.IdAptitudeFk,
                    NomAptitude   = a.IdAptitudeFkNavigation != null ? a.IdAptitudeFkNavigation.NomAptitude : null,
                    a.IdCompetenceFk,
                    NomCompetence = a.IdCompetenceFkNavigation.NomCompetence
                })
                .ToListAsync();

            // Repli : si aucune entrée appartenir et qu'une visée est connue,
            // on retourne la compétence (Attendus) de cette visée comme aptitude
            if (!list.Any() && idVisee.HasValue)
            {
                var visee = await _context.Visees
                    .Include(v => v.IdCompFkNavigation)
                    .FirstOrDefaultAsync(v => v.IdVisee == idVisee.Value);

                if (visee != null)
                {
                    var comp = visee.IdCompFkNavigation;
                    return Ok(new[] { new
                    {
                        IdAppartenirViseeAptitude = -comp.IdCompetence,   // négatif = entrée de repli
                        IdAptitude                = (int?)null,
                        NomAptitude               = (string?)null,
                        IdCompetenceFk            = comp.IdCompetence,
                        NomCompetence             = comp.NomCompetence
                    }});
                }
            }

            return Ok(list);
        }

        // ──────────────────────────────────────────────────────────────────
        // TABLES COMPLÈTES ET COMPLÉMENT DU RÉFÉRENTIEL
        //
        // Le référentiel est incomplet : un champ (domaine) n'a pas toujours
        // toutes les compétences ou visées dont l'enseignant a besoin. Ces
        // points de terminaison lui donnent les tables entières, puis créent
        // les liens manquants pour que sa sélection existe vraiment en base.
        // Ils sont ouverts à tout utilisateur authentifié, contrairement à
        // api/admin-data qui reste réservé au rôle ADMIN.
        // ──────────────────────────────────────────────────────────────────

        // GET /api/ref/competences
        // Retourne toutes les compétences de la table, triées par nom
        [HttpGet("competences")]
        public async Task<IActionResult> GetToutesCompetences()
        {
            var list = await _context.Competences
                .OrderBy(c => c.NomCompetence)
                .Select(c => new { c.IdCompetence, c.NomCompetence })
                .ToListAsync();

            return Ok(list);
        }

        // GET /api/ref/nom-visees
        // Retourne tous les intitulés de visée de la table, triés par nom
        [HttpGet("nom-visees")]
        public async Task<IActionResult> GetTousNomVisees()
        {
            var list = await _context.NomVisees
                .OrderBy(nv => nv.NomVisee1)
                .Select(nv => new { nv.IdNomVisee, NomVisee = nv.NomVisee1 })
                .ToListAsync();

            return Ok(list);
        }

        // GET /api/ref/visees-maitriser
        // Retourne toutes les visées à maîtriser de la table, triées par nom.
        // (La variante avec identifiant ne rend que celles liées à une visée.)
        [HttpGet("visees-maitriser")]
        public async Task<IActionResult> GetToutesViseesMaitriser()
        {
            var list = await _context.ViseesMaitrisers
                .OrderBy(vm => vm.NomViseesMaitriser)
                .Select(vm => new { vm.IdViseesMaitriser, vm.NomViseesMaitriser })
                .ToListAsync();

            return Ok(list);
        }

        // Clé primaire de l'utilisateur connecté, résolue depuis le claim Name (son email).
        // Null si le claim est absent ou si l'utilisateur est introuvable.
        private async Task<int?> GetUserId()
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (email == null) return null;
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
            return user?.IdUser;
        }

        // Année, catégorie et cours sont des données fixes du référentiel : elles ne
        // s'ajoutent pas depuis la cascade. La complétion par l'enseignant commence au
        // champ (domaine), qui lui appartient déjà par sa liaison cours_niveau.

        // POST /api/ref/domaines
        // Ajoute un champ (table domaine) pour un cours et une année. Un champ appartient
        // à une liaison cours_niveau, propre à un enseignant : celle du compte connecté est
        // utilisée, et créée si elle n'existe pas encore. Rejouable sur le nom du champ.
        [HttpPost("domaines")]
        public async Task<IActionResult> CreerDomaine([FromBody] CreerDomaineDto dto)
        {
            var nom = (dto.Nom ?? "").Trim();
            if (nom.Length == 0) return BadRequest(new { message = "Le nom du champ est obligatoire." });

            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            var cours = await _context.Cours.FirstOrDefaultAsync(c => c.CodeCours == dto.CodeCours);
            if (cours == null) return BadRequest(new { message = "Cours introuvable." });

            var niveau = await _context.Niveaus.FirstOrDefaultAsync(n => n.CodeNiveau == dto.CodeNiveau);
            if (niveau == null) return BadRequest(new { message = "Année introuvable." });

            var coursNiveau = await _context.CoursNiveaus.FirstOrDefaultAsync(
                cn => cn.IdCoursFk == cours.IdCours
                   && cn.IdNiveauFk == niveau.IdNiveau
                   && cn.IdProfFk == userId.Value);

            if (coursNiveau == null)
            {
                coursNiveau = new CoursNiveau
                {
                    IdCoursFk  = cours.IdCours,
                    IdNiveauFk = niveau.IdNiveau,
                    IdProfFk   = userId.Value
                };
                _context.CoursNiveaus.Add(coursNiveau);
                try { await _context.SaveChangesAsync(); }
                catch { return BadRequest(new { message = "Erreur lors de la création de la liaison cours/année." }); }
            }

            // Un champ de même nom déjà présent pour ce cours et cette année est réutilisé,
            // quel que soit l'enseignant : c'est ce qui évite les doublons entre collègues.
            var existant = await _context.Domaines.FirstOrDefaultAsync(
                d => d.Nom.ToLower() == nom.ToLower()
                  && d.IdCoursNiveauFkNavigation.IdCoursFk == cours.IdCours
                  && d.IdCoursNiveauFkNavigation.IdNiveauFk == niveau.IdNiveau);

            if (existant != null) return Ok(new { existant.IdDom, existant.Nom, Creee = false });

            var domaine = new Domaine { Nom = nom, IdCoursNiveauFk = coursNiveau.IdCoursNiveau };
            _context.Domaines.Add(domaine);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Erreur lors de la création du champ." }); }

            return Ok(new { domaine.IdDom, domaine.Nom, Creee = true });
        }

        // POST /api/ref/sous-domaines
        // Ajoute un domaine (table sous_domaine) sous un champ donné. Rejouable sur le nom.
        [HttpPost("sous-domaines")]
        public async Task<IActionResult> CreerSousDomaine([FromBody] CreerSousDomaineDto dto)
        {
            var nom = (dto.Nom ?? "").Trim();
            if (nom.Length == 0) return BadRequest(new { message = "Le nom du domaine est obligatoire." });

            if (!await _context.Domaines.AnyAsync(d => d.IdDom == dto.IdDomaine))
                return BadRequest(new { message = "Champ introuvable." });

            var existant = await _context.Sousdomaines.FirstOrDefaultAsync(
                sd => sd.IdDomFk == dto.IdDomaine && sd.NomComp.ToLower() == nom.ToLower());

            if (existant != null) return Ok(new { existant.IdSousDomaine, Creee = false });

            var sousDomaine = new Sousdomaine { NomComp = nom, IdDomFk = dto.IdDomaine };
            _context.Sousdomaines.Add(sousDomaine);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Erreur lors de la création du domaine." }); }

            return Ok(new { sousDomaine.IdSousDomaine, Creee = true });
        }

        // POST /api/ref/competences
        // Ajoute une compétence au référentiel. Si le nom existe déjà (à la casse et
        // aux espaces près), son identifiant est simplement renvoyé.
        [HttpPost("competences")]
        public async Task<IActionResult> CreerCompetence([FromBody] CreerNommeDto dto)
        {
            var nom = (dto.Nom ?? "").Trim();
            if (nom.Length == 0) return BadRequest(new { message = "Le nom est obligatoire." });

            var existante = await _context.Competences
                .FirstOrDefaultAsync(c => c.NomCompetence.ToLower() == nom.ToLower());

            if (existante != null) return Ok(new { existante.IdCompetence, Creee = false });

            var competence = new Competence { NomCompetence = nom };
            _context.Competences.Add(competence);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Erreur lors de la création de la compétence." }); }

            return Ok(new { competence.IdCompetence, Creee = true });
        }

        // POST /api/ref/nom-visees
        // Ajoute un intitulé de visée au référentiel. Rejouable, comme ci-dessus.
        [HttpPost("nom-visees")]
        public async Task<IActionResult> CreerNomVisee([FromBody] CreerNommeDto dto)
        {
            var nom = (dto.Nom ?? "").Trim();
            if (nom.Length == 0) return BadRequest(new { message = "Le nom est obligatoire." });

            var existant = await _context.NomVisees
                .FirstOrDefaultAsync(nv => nv.NomVisee1.ToLower() == nom.ToLower());

            if (existant != null) return Ok(new { existant.IdNomVisee, Creee = false });

            var nomVisee = new NomVisee { NomVisee1 = nom };
            _context.NomVisees.Add(nomVisee);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Erreur lors de la création de l'intitulé de visée." }); }

            return Ok(new { nomVisee.IdNomVisee, Creee = true });
        }

        // POST /api/ref/visees-maitriser
        // Ajoute une visée à maîtriser au référentiel. Rejouable, comme ci-dessus.
        [HttpPost("visees-maitriser")]
        public async Task<IActionResult> CreerViseeMaitriser([FromBody] CreerNommeDto dto)
        {
            var nom = (dto.Nom ?? "").Trim();
            if (nom.Length == 0) return BadRequest(new { message = "Le nom est obligatoire." });

            var existante = await _context.ViseesMaitrisers
                .FirstOrDefaultAsync(vm => vm.NomViseesMaitriser.ToLower() == nom.ToLower());

            if (existante != null) return Ok(new { existante.IdViseesMaitriser, Creee = false });

            var vm2 = new ViseesMaitriser { NomViseesMaitriser = nom };
            _context.ViseesMaitrisers.Add(vm2);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Erreur lors de la création de la visée à maîtriser." }); }

            return Ok(new { vm2.IdViseesMaitriser, Creee = true });
        }

        // POST /api/ref/visees
        // Rattache un intitulé de visée et une compétence à un champ (domaine) et,
        // éventuellement, à un domaine (sous-domaine). Si la visée existe déjà, son
        // identifiant est simplement renvoyé : l'appel est donc rejouable sans risque.
        [HttpPost("visees")]
        public async Task<IActionResult> CreerVisee([FromBody] CreerViseeDto dto)
        {
            if (dto.IdDomaine <= 0 || dto.IdNomVisee <= 0 || dto.IdCompetence <= 0)
                return BadRequest(new { message = "Champ, visée et compétence sont obligatoires." });

            int? idSousDomaine = dto.IdSousDomaine > 0 ? dto.IdSousDomaine : null;

            // Les identifiants doivent exister, sinon la contrainte de clé étrangère
            // renverrait une erreur illisible côté client.
            if (!await _context.Domaines.AnyAsync(d => d.IdDom == dto.IdDomaine))
                return BadRequest(new { message = "Champ introuvable." });
            if (!await _context.NomVisees.AnyAsync(nv => nv.IdNomVisee == dto.IdNomVisee))
                return BadRequest(new { message = "Intitulé de visée introuvable." });
            if (!await _context.Competences.AnyAsync(c => c.IdCompetence == dto.IdCompetence))
                return BadRequest(new { message = "Compétence introuvable." });
            if (idSousDomaine != null &&
                !await _context.Sousdomaines.AnyAsync(sd => sd.IdSousDomaine == idSousDomaine))
                return BadRequest(new { message = "Domaine introuvable." });

            var existante = await _context.Visees.FirstOrDefaultAsync(
                v => v.IdDomaineFk     == dto.IdDomaine
                  && v.IdSousDomaineFk == idSousDomaine
                  && v.IdNomViseeFk    == dto.IdNomVisee
                  && v.IdCompFk        == dto.IdCompetence);

            if (existante != null) return Ok(new { existante.IdVisee, Creee = false });

            var visee = new Visee
            {
                IdDomaineFk     = dto.IdDomaine,
                IdSousDomaineFk = idSousDomaine,
                IdNomViseeFk    = dto.IdNomVisee,
                IdCompFk        = dto.IdCompetence
            };

            _context.Visees.Add(visee);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Erreur lors de la création de la visée." }); }

            return Ok(new { visee.IdVisee, Creee = true });
        }

        // POST /api/ref/lien-visee-maitrise
        // Relie une visée à une visée à maîtriser (table de jointure lien_visee_maitrise).
        // Rejouable : si le lien existe déjà, la requête réussit sans rien changer.
        [HttpPost("lien-visee-maitrise")]
        public async Task<IActionResult> CreerLienViseeMaitrise([FromBody] CreerLienDto dto)
        {
            var visee = await _context.Visees
                .Include(v => v.IdViseesMaitriserFks)
                .FirstOrDefaultAsync(v => v.IdVisee == dto.IdVisee);

            if (visee == null) return NotFound(new { message = "Visée introuvable." });

            if (visee.IdViseesMaitriserFks.Any(vm => vm.IdViseesMaitriser == dto.IdViseesMaitriser))
                return Ok(new { Creee = false });

            var vm = await _context.ViseesMaitrisers
                .FirstOrDefaultAsync(x => x.IdViseesMaitriser == dto.IdViseesMaitriser);

            if (vm == null) return NotFound(new { message = "Visée à maîtriser introuvable." });

            visee.IdViseesMaitriserFks.Add(vm);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Erreur lors de la création du lien." }); }

            return Ok(new { Creee = true });
        }

        // Corps attendu par les POST qui ajoutent une entrée simplement nommée
        public class CreerNommeDto
        {
            public string? Nom { get; set; }
        }

        // Corps attendu par POST /api/ref/domaines
        public class CreerDomaineDto
        {
            public string? Nom { get; set; }
            public string? CodeCours { get; set; }
            public string? CodeNiveau { get; set; }
        }

        // Corps attendu par POST /api/ref/sous-domaines
        public class CreerSousDomaineDto
        {
            public string? Nom { get; set; }
            public int IdDomaine { get; set; }
        }

        // Corps attendu par POST /api/ref/visees
        public class CreerViseeDto
        {
            public int IdDomaine { get; set; }
            public int IdSousDomaine { get; set; }   // 0 = aucun
            public int IdNomVisee { get; set; }
            public int IdCompetence { get; set; }
        }

        // Corps attendu par POST /api/ref/lien-visee-maitrise
        public class CreerLienDto
        {
            public int IdVisee { get; set; }
            public int IdViseesMaitriser { get; set; }
        }
    }
}
