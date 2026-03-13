// Importation des attributs d'autorisation ASP.NET Core
using Microsoft.AspNetCore.Authorization;
// Importation des types de base pour les contrôleurs MVC/API
using Microsoft.AspNetCore.Mvc;
// Importation d'Entity Framework Core pour les requêtes asynchrones
using Microsoft.EntityFrameworkCore;
// Importation des modèles d'entités du projet
using seragenda.Models;

namespace seragenda.Controllers
{
    // Marque cette classe comme contrôleur API avec liaison automatique du modèle
    [ApiController]
    // Toutes les routes sont préfixées par /api/admin-data
    [Route("api/admin-data")]
    // Restreint tous les endpoints aux utilisateurs ayant le rôle ADMIN
    [Authorize(Roles = "ADMIN")]
    // Contrôleur d'administration pour la gestion des données pédagogiques de référence.
    // Fournit des opérations CRUD pour : catégories, cours, niveaux, liaisons cours-niveau,
    // domaines, compétences, aptitudes, noms de visées, visées à maîtriser et sous-domaines.
    public class AdminDataController : ControllerBase
    {
        // Contexte EF Core pour accéder à toutes les tables de la base de données
        private readonly AgendaContext _context;

        // Constructeur — reçoit le contexte de base de données par injection de dépendances.
        // context : le contexte EF Core de la base de données agenda
        public AdminDataController(AgendaContext context)
        {
            _context = context;
        }

        // ──────────────────────────────────────────────────────────────────
        // CATÉGORIES DE COURS
        // ──────────────────────────────────────────────────────────────────

        // GET /api/admin-data/categories
        // Retourne toutes les catégories triées par ordre d'affichage
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var list = await _context.CategorieCours
                .OrderBy(c => c.Ordre)
                .Select(c => new { c.IdCat, c.NomCat, c.Ordre })
                .ToListAsync();
            return Ok(list);
        }

        // POST /api/admin-data/categories
        // Crée une nouvelle catégorie de cours
        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategorie([FromBody] CreateCategorieDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NomCat))
                return BadRequest(new { message = "Le nom est requis" });

            var cat = new CategorieCours { NomCat = dto.NomCat.Trim(), Ordre = dto.Ordre };
            _context.CategorieCours.Add(cat);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Ce nom de catégorie existe déjà" }); }
            return Ok(new { cat.IdCat, cat.NomCat, cat.Ordre });
        }

        // DELETE /api/admin-data/categories/{id}
        // Supprime une catégorie (échoue si des cours y sont liés)
        [HttpDelete("categories/{id:int}")]
        public async Task<IActionResult> DeleteCategorie(int id)
        {
            var cat = await _context.CategorieCours.FindAsync(id);
            if (cat == null) return NotFound();
            _context.CategorieCours.Remove(cat);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Impossible : des cours sont liés à cette catégorie" }); }
            return Ok();
        }

        // ──────────────────────────────────────────────────────────────────
        // COURS
        // ──────────────────────────────────────────────────────────────────

        // GET /api/admin-data/cours
        // Retourne tous les cours avec le nom de leur catégorie parente
        [HttpGet("cours")]
        public async Task<IActionResult> GetCours()
        {
            var list = await _context.Cours
                .Include(c => c.IdCatFkNavigation)
                .OrderBy(c => c.NomCours)
                .Select(c => new
                {
                    c.IdCours, c.NomCours, c.CodeCours, c.PrefixCours,
                    c.CouleurAgenda, c.IdCatFk,
                    NomCat = c.IdCatFkNavigation != null ? c.IdCatFkNavigation.NomCat : null
                })
                .ToListAsync();
            return Ok(list);
        }

        // POST /api/admin-data/cours
        // Crée un nouveau cours dans une catégorie donnée
        [HttpPost("cours")]
        public async Task<IActionResult> CreateCours([FromBody] CreateCoursAdminDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NomCours) || string.IsNullOrWhiteSpace(dto.CodeCours))
                return BadRequest(new { message = "Nom et code sont requis" });

            var cours = new Cour
            {
                NomCours      = dto.NomCours.Trim(),
                CodeCours     = dto.CodeCours.Trim().ToUpper(),
                PrefixCours   = string.IsNullOrWhiteSpace(dto.PrefixCours) ? dto.CodeCours.Trim().ToUpper() : dto.PrefixCours.Trim(),
                CouleurAgenda = string.IsNullOrWhiteSpace(dto.CouleurAgenda) ? "#3B82F6" : dto.CouleurAgenda.Trim(),
                IdCatFk       = dto.IdCatFk
            };
            _context.Cours.Add(cours);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Ce code de cours existe déjà" }); }
            return Ok(new { cours.IdCours, cours.NomCours, cours.CodeCours });
        }

        // DELETE /api/admin-data/cours/{id}
        // Supprime un cours (échoue si des liaisons cours-niveau existent)
        [HttpDelete("cours/{id:int}")]
        public async Task<IActionResult> DeleteCours(int id)
        {
            var cours = await _context.Cours.FindAsync(id);
            if (cours == null) return NotFound();
            _context.Cours.Remove(cours);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Impossible : des liaisons cours-niveau existent pour ce cours" }); }
            return Ok();
        }

        // ──────────────────────────────────────────────────────────────────
        // NIVEAUX
        // ──────────────────────────────────────────────────────────────────

        // GET /api/admin-data/niveaux
        // Retourne tous les niveaux d'enseignement triés par ordre
        [HttpGet("niveaux")]
        public async Task<IActionResult> GetNiveaux()
        {
            var list = await _context.Niveaus
                .OrderBy(n => n.Ordre)
                .Select(n => new { n.IdNiveau, n.CodeNiveau, n.NomNiveau, n.Ordre })
                .ToListAsync();
            return Ok(list);
        }

        // POST /api/admin-data/niveaux
        // Crée un nouveau niveau d'enseignement
        [HttpPost("niveaux")]
        public async Task<IActionResult> CreateNiveau([FromBody] CreateNiveauAdminDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CodeNiveau) || string.IsNullOrWhiteSpace(dto.NomNiveau))
                return BadRequest(new { message = "Code et nom sont requis" });

            var niv = new Niveau
            {
                CodeNiveau = dto.CodeNiveau.Trim().ToUpper(),
                NomNiveau  = dto.NomNiveau.Trim(),
                Ordre      = dto.Ordre
            };
            _context.Niveaus.Add(niv);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Ce code de niveau existe déjà" }); }
            return Ok(new { niv.IdNiveau, niv.CodeNiveau, niv.NomNiveau });
        }

        // DELETE /api/admin-data/niveaux/{id}
        // Supprime un niveau (échoue si des liaisons cours-niveau existent)
        [HttpDelete("niveaux/{id:int}")]
        public async Task<IActionResult> DeleteNiveau(int id)
        {
            var niv = await _context.Niveaus.FindAsync(id);
            if (niv == null) return NotFound();
            _context.Niveaus.Remove(niv);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Impossible : des liaisons cours-niveau existent pour ce niveau" }); }
            return Ok();
        }

        // ──────────────────────────────────────────────────────────────────
        // PROFESSEURS (lookup uniquement — pas de création ici)
        // ──────────────────────────────────────────────────────────────────

        // GET /api/admin-data/professeurs
        // Retourne la liste des utilisateurs (PROF et ADMIN) pour les sélecteurs
        [HttpGet("professeurs")]
        public async Task<IActionResult> GetProfesseurs()
        {
            var list = await _context.Utilisateurs
                .Where(u => u.RoleSysteme == "PROF" || u.RoleSysteme == "ADMIN")
                .OrderBy(u => u.Nom)
                .Select(u => new { u.IdUser, u.Email, u.Nom, u.Prenom })
                .ToListAsync();
            return Ok(list);
        }

        // ──────────────────────────────────────────────────────────────────
        // LIAISONS COURS-NIVEAU
        // ──────────────────────────────────────────────────────────────────

        // GET /api/admin-data/cours-niveaux
        // Retourne toutes les liaisons cours-niveau avec les noms associés
        [HttpGet("cours-niveaux")]
        public async Task<IActionResult> GetCoursNiveaux()
        {
            var list = await _context.CoursNiveaus
                .Include(cn => cn.IdCoursFkNavigation)
                .Include(cn => cn.IdNiveauFkNavigation)
                .Include(cn => cn.IdProfFkNavigation)
                .OrderBy(cn => cn.IdCoursFkNavigation.NomCours)
                .ThenBy(cn => cn.IdNiveauFkNavigation.Ordre)
                .Select(cn => new
                {
                    cn.IdCoursNiveau,
                    cn.IdCoursFk,  NomCours  = cn.IdCoursFkNavigation.NomCours,
                    cn.IdNiveauFk, NomNiveau = cn.IdNiveauFkNavigation.NomNiveau,
                    cn.IdProfFk,   EmailProf = cn.IdProfFkNavigation.Email,
                    NomProf = (cn.IdProfFkNavigation.Prenom ?? "") + " " + (cn.IdProfFkNavigation.Nom ?? "")
                })
                .ToListAsync();
            return Ok(list);
        }

        // POST /api/admin-data/cours-niveaux
        // Crée une nouvelle liaison cours + niveau + professeur
        [HttpPost("cours-niveaux")]
        public async Task<IActionResult> CreateCoursNiveau([FromBody] CreateCoursNiveauAdminDto dto)
        {
            var cn = new CoursNiveau
            {
                IdCoursFk  = dto.IdCoursFk,
                IdNiveauFk = dto.IdNiveauFk,
                IdProfFk   = dto.IdProfFk
            };
            _context.CoursNiveaus.Add(cn);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Cette liaison existe déjà ou les références sont invalides" }); }
            return Ok(new { cn.IdCoursNiveau });
        }

        // DELETE /api/admin-data/cours-niveaux/{id}
        // Supprime une liaison cours-niveau (échoue si des domaines ou planifications y sont liés)
        [HttpDelete("cours-niveaux/{id:int}")]
        public async Task<IActionResult> DeleteCoursNiveau(int id)
        {
            var cn = await _context.CoursNiveaus.FindAsync(id);
            if (cn == null) return NotFound();
            _context.CoursNiveaus.Remove(cn);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Impossible : des domaines ou planifications sont liés à cette liaison" }); }
            return Ok();
        }

        // ──────────────────────────────────────────────────────────────────
        // DOMAINES
        // ──────────────────────────────────────────────────────────────────

        // GET /api/admin-data/domaines
        // Retourne tous les domaines avec leur contexte cours et niveau
        [HttpGet("domaines")]
        public async Task<IActionResult> GetDomaines()
        {
            var list = await _context.Domaines
                .Include(d => d.IdCoursNiveauFkNavigation)
                    .ThenInclude(cn => cn.IdCoursFkNavigation)
                .Include(d => d.IdCoursNiveauFkNavigation)
                    .ThenInclude(cn => cn.IdNiveauFkNavigation)
                .OrderBy(d => d.IdCoursNiveauFkNavigation.IdCoursFkNavigation.NomCours)
                .ThenBy(d => d.Nom)
                .Select(d => new
                {
                    d.IdDom, d.Nom, d.IdCoursNiveauFk,
                    NomCours  = d.IdCoursNiveauFkNavigation.IdCoursFkNavigation.NomCours,
                    NomNiveau = d.IdCoursNiveauFkNavigation.IdNiveauFkNavigation.NomNiveau
                })
                .ToListAsync();
            return Ok(list);
        }

        // POST /api/admin-data/domaines
        // Crée un nouveau domaine lié à une liaison cours-niveau
        [HttpPost("domaines")]
        public async Task<IActionResult> CreateDomaine([FromBody] CreateDomaineAdminDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nom))
                return BadRequest(new { message = "Le nom est requis" });

            var dom = new Domaine { Nom = dto.Nom.Trim(), IdCoursNiveauFk = dto.IdCoursNiveauFk };
            _context.Domaines.Add(dom);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Erreur lors de la création du domaine" }); }
            return Ok(new { dom.IdDom, dom.Nom });
        }

        // DELETE /api/admin-data/domaines/{id}
        // Supprime un domaine (échoue si des sous-domaines ou visées y sont liés)
        [HttpDelete("domaines/{id:int}")]
        public async Task<IActionResult> DeleteDomaine(int id)
        {
            var dom = await _context.Domaines.FindAsync(id);
            if (dom == null) return NotFound();
            _context.Domaines.Remove(dom);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Impossible : des sous-domaines ou visées sont liés à ce domaine" }); }
            return Ok();
        }

        // ──────────────────────────────────────────────────────────────────
        // COMPÉTENCES
        // ──────────────────────────────────────────────────────────────────

        // GET /api/admin-data/competences
        [HttpGet("competences")]
        public async Task<IActionResult> GetCompetences()
        {
            var list = await _context.Competences
                .OrderBy(c => c.NomCompetence)
                .Select(c => new { c.IdCompetence, c.NomCompetence })
                .ToListAsync();
            return Ok(list);
        }

        // POST /api/admin-data/competences
        [HttpPost("competences")]
        public async Task<IActionResult> CreateCompetence([FromBody] SimpleNomAdminDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nom))
                return BadRequest(new { message = "Le nom est requis" });
            var comp = new Competence { NomCompetence = dto.Nom.Trim() };
            _context.Competences.Add(comp);
            await _context.SaveChangesAsync();
            return Ok(new { comp.IdCompetence, comp.NomCompetence });
        }

        // DELETE /api/admin-data/competences/{id}
        [HttpDelete("competences/{id:int}")]
        public async Task<IActionResult> DeleteCompetence(int id)
        {
            var comp = await _context.Competences.FindAsync(id);
            if (comp == null) return NotFound();
            _context.Competences.Remove(comp);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Impossible : des visées sont liées à cette compétence" }); }
            return Ok();
        }

        // ──────────────────────────────────────────────────────────────────
        // APTITUDES
        // ──────────────────────────────────────────────────────────────────

        // GET /api/admin-data/aptitudes
        [HttpGet("aptitudes")]
        public async Task<IActionResult> GetAptitudes()
        {
            var list = await _context.Aptitudes
                .OrderBy(a => a.NomAptitude)
                .Select(a => new { a.IdAptitude, a.NomAptitude })
                .ToListAsync();
            return Ok(list);
        }

        // POST /api/admin-data/aptitudes
        [HttpPost("aptitudes")]
        public async Task<IActionResult> CreateAptitude([FromBody] SimpleNomAdminDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nom))
                return BadRequest(new { message = "Le nom est requis" });
            var apt = new Aptitude { NomAptitude = dto.Nom.Trim() };
            _context.Aptitudes.Add(apt);
            await _context.SaveChangesAsync();
            return Ok(new { apt.IdAptitude, apt.NomAptitude });
        }

        // DELETE /api/admin-data/aptitudes/{id}
        [HttpDelete("aptitudes/{id:int}")]
        public async Task<IActionResult> DeleteAptitude(int id)
        {
            var apt = await _context.Aptitudes.FindAsync(id);
            if (apt == null) return NotFound();
            _context.Aptitudes.Remove(apt);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Impossible : des entrées sont liées à cette aptitude" }); }
            return Ok();
        }

        // ──────────────────────────────────────────────────────────────────
        // NOMS DE VISÉES
        // ──────────────────────────────────────────────────────────────────

        // GET /api/admin-data/nom-visees
        [HttpGet("nom-visees")]
        public async Task<IActionResult> GetNomVisees()
        {
            var list = await _context.NomVisees
                .OrderBy(n => n.NomVisee1)
                .Select(n => new { n.IdNomVisee, n.NomVisee1 })
                .ToListAsync();
            return Ok(list);
        }

        // POST /api/admin-data/nom-visees
        [HttpPost("nom-visees")]
        public async Task<IActionResult> CreateNomVisee([FromBody] SimpleNomAdminDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nom))
                return BadRequest(new { message = "Le nom est requis" });
            var nv = new NomVisee { NomVisee1 = dto.Nom.Trim() };
            _context.NomVisees.Add(nv);
            await _context.SaveChangesAsync();
            return Ok(new { nv.IdNomVisee, nv.NomVisee1 });
        }

        // DELETE /api/admin-data/nom-visees/{id}
        [HttpDelete("nom-visees/{id:int}")]
        public async Task<IActionResult> DeleteNomVisee(int id)
        {
            var nv = await _context.NomVisees.FindAsync(id);
            if (nv == null) return NotFound();
            _context.NomVisees.Remove(nv);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Impossible : des visées sont liées à ce nom" }); }
            return Ok();
        }

        // ──────────────────────────────────────────────────────────────────
        // VISÉES À MAÎTRISER
        // ──────────────────────────────────────────────────────────────────

        // GET /api/admin-data/visees-maitriser
        [HttpGet("visees-maitriser")]
        public async Task<IActionResult> GetViseesMaitriser()
        {
            var list = await _context.ViseesMaitrisers
                .OrderBy(v => v.NomViseesMaitriser)
                .Select(v => new { v.IdViseesMaitriser, v.NomViseesMaitriser })
                .ToListAsync();
            return Ok(list);
        }

        // POST /api/admin-data/visees-maitriser
        [HttpPost("visees-maitriser")]
        public async Task<IActionResult> CreateViseesMaitriser([FromBody] SimpleNomAdminDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nom))
                return BadRequest(new { message = "Le nom est requis" });
            var vm = new ViseesMaitriser { NomViseesMaitriser = dto.Nom.Trim() };
            _context.ViseesMaitrisers.Add(vm);
            await _context.SaveChangesAsync();
            return Ok(new { vm.IdViseesMaitriser, vm.NomViseesMaitriser });
        }

        // DELETE /api/admin-data/visees-maitriser/{id}
        [HttpDelete("visees-maitriser/{id:int}")]
        public async Task<IActionResult> DeleteViseesMaitriser(int id)
        {
            var vm = await _context.ViseesMaitrisers.FindAsync(id);
            if (vm == null) return NotFound();
            _context.ViseesMaitrisers.Remove(vm);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Impossible : des liaisons sont attachées à cette visée à maîtriser" }); }
            return Ok();
        }

        // ──────────────────────────────────────────────────────────────────
        // SOUS-DOMAINES
        // ──────────────────────────────────────────────────────────────────

        // GET /api/admin-data/sous-domaines
        // Retourne tous les sous-domaines avec le nom du domaine parent
        [HttpGet("sous-domaines")]
        public async Task<IActionResult> GetSousDomaines()
        {
            var list = await _context.Sousdomaines
                .Include(s => s.IdDomFkNavigation)
                .OrderBy(s => s.IdDomFkNavigation.Nom)
                .ThenBy(s => s.NomComp)
                .Select(s => new { s.IdSousDomaine, s.NomComp, s.IdDomFk, NomDom = s.IdDomFkNavigation.Nom })
                .ToListAsync();
            return Ok(list);
        }

        // POST /api/admin-data/sous-domaines
        // Crée un nouveau sous-domaine lié à un domaine
        [HttpPost("sous-domaines")]
        public async Task<IActionResult> CreateSousDomaine([FromBody] CreateSousDomaineAdminDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NomComp))
                return BadRequest(new { message = "Le nom est requis" });
            var sd = new Sousdomaine { NomComp = dto.NomComp.Trim(), IdDomFk = dto.IdDomFk };
            _context.Sousdomaines.Add(sd);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Erreur lors de la création du sous-domaine" }); }
            return Ok(new { sd.IdSousDomaine, sd.NomComp });
        }

        // DELETE /api/admin-data/sous-domaines/{id}
        [HttpDelete("sous-domaines/{id:int}")]
        public async Task<IActionResult> DeleteSousDomaine(int id)
        {
            var sd = await _context.Sousdomaines.FindAsync(id);
            if (sd == null) return NotFound();
            _context.Sousdomaines.Remove(sd);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Impossible : des visées sont liées à ce sous-domaine" }); }
            return Ok();
        }

        // ──────────────────────────────────────────────────────────────────
        // VISÉES (objectifs d'apprentissage)
        // ──────────────────────────────────────────────────────────────────

        // GET /api/admin-data/visees
        // Retourne toutes les visées avec leur contexte complet (chaîne cours → domaine → compétence)
        [HttpGet("visees")]
        public async Task<IActionResult> GetVisees()
        {
            var list = await _context.Visees
                .Include(v => v.IdNomViseeFkNavigation)
                .Include(v => v.IdDomaineFkNavigation)
                    .ThenInclude(d => d.IdCoursNiveauFkNavigation)
                        .ThenInclude(cn => cn.IdCoursFkNavigation)
                .Include(v => v.IdDomaineFkNavigation)
                    .ThenInclude(d => d.IdCoursNiveauFkNavigation)
                        .ThenInclude(cn => cn.IdNiveauFkNavigation)
                .Include(v => v.IdSousDomaineFkNavigation)
                .Include(v => v.IdCompFkNavigation)
                .OrderBy(v => v.IdDomaineFkNavigation.IdCoursNiveauFkNavigation.IdCoursFkNavigation.NomCours)
                .ThenBy(v => v.IdDomaineFkNavigation.Nom)
                .Select(v => new
                {
                    v.IdVisee,
                    v.IdNomViseeFk,    NomViseeType   = v.IdNomViseeFkNavigation.NomVisee1,
                    v.IdDomaineFk,     NomDomaine     = v.IdDomaineFkNavigation.Nom,
                    v.IdSousDomaineFk, NomSousDomaine = v.IdSousDomaineFkNavigation != null ? v.IdSousDomaineFkNavigation.NomComp : null,
                    v.IdCompFk,        NomCompetence  = v.IdCompFkNavigation.NomCompetence,
                    NomCours  = v.IdDomaineFkNavigation.IdCoursNiveauFkNavigation.IdCoursFkNavigation.NomCours,
                    NomNiveau = v.IdDomaineFkNavigation.IdCoursNiveauFkNavigation.IdNiveauFkNavigation.NomNiveau
                })
                .ToListAsync();
            return Ok(list);
        }

        // POST /api/admin-data/visees
        // Crée une nouvelle visée (objectif d'apprentissage)
        [HttpPost("visees")]
        public async Task<IActionResult> CreateVisee([FromBody] CreateViseeAdminDto dto)
        {
            var visee = new Visee
            {
                IdNomViseeFk    = dto.IdNomViseeFk,
                IdDomaineFk     = dto.IdDomaineFk,
                IdSousDomaineFk = dto.IdSousDomaineFk,
                IdCompFk        = dto.IdCompFk
            };
            _context.Visees.Add(visee);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Erreur lors de la création de la visée" }); }
            return Ok(new { visee.IdVisee });
        }

        // DELETE /api/admin-data/visees/{id}
        // Supprime une visée
        [HttpDelete("visees/{id:int}")]
        public async Task<IActionResult> DeleteVisee(int id)
        {
            var visee = await _context.Visees.FindAsync(id);
            if (visee == null) return NotFound();
            _context.Visees.Remove(visee);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Impossible : cette visée est utilisée dans des séances" }); }
            return Ok();
        }

        // ──────────────────────────────────────────────────────────────────
        // LIEN VISÉE ↔ VISÉE À MAÎTRISER  (lien_visee_maitrise)
        // ──────────────────────────────────────────────────────────────────

        // GET /api/admin-data/lien-visee-maitrise
        // Retourne toutes les liaisons existantes entre visées et visées à maîtriser
        [HttpGet("lien-visee-maitrise")]
        public async Task<IActionResult> GetLiensViseeMaitrise()
        {
            var list = await _context.Visees
                .Include(v => v.IdViseesMaitriserFks)
                .Include(v => v.IdDomaineFkNavigation)
                    .ThenInclude(d => d.IdCoursNiveauFkNavigation)
                        .ThenInclude(cn => cn.IdCoursFkNavigation)
                .Include(v => v.IdNomViseeFkNavigation)
                .Where(v => v.IdViseesMaitriserFks.Any())
                .SelectMany(v => v.IdViseesMaitriserFks.Select(vm => new
                {
                    IdVisee            = v.IdVisee,
                    ContexteVisee      = v.IdNomViseeFkNavigation.NomVisee1 + " / " +
                                         v.IdDomaineFkNavigation.IdCoursNiveauFkNavigation.IdCoursFkNavigation.NomCours + " — " +
                                         v.IdDomaineFkNavigation.Nom,
                    IdViseesMaitriser  = vm.IdViseesMaitriser,
                    NomViseesMaitriser = vm.NomViseesMaitriser
                }))
                .ToListAsync();
            return Ok(list);
        }

        // POST /api/admin-data/lien-visee-maitrise
        // Crée le lien entre une visée et une visée à maîtriser
        [HttpPost("lien-visee-maitrise")]
        public async Task<IActionResult> CreateLienViseeMaitrise([FromBody] CreateLienViseeMaitriseDto dto)
        {
            // Chargement de la visée avec sa collection de visées à maîtriser déjà liées
            var visee = await _context.Visees
                .Include(v => v.IdViseesMaitriserFks)
                .FirstOrDefaultAsync(v => v.IdVisee == dto.IdVisee);
            if (visee == null) return NotFound(new { message = "Visée introuvable" });

            // Vérification que la visée à maîtriser cible existe
            var vm = await _context.ViseesMaitrisers.FindAsync(dto.IdViseesMaitriser);
            if (vm == null) return NotFound(new { message = "Visée à maîtriser introuvable" });

            // Vérifie que la liaison n'existe pas déjà pour éviter un doublon
            if (visee.IdViseesMaitriserFks.Any(x => x.IdViseesMaitriser == dto.IdViseesMaitriser))
                return BadRequest(new { message = "Cette liaison existe déjà" });

            // Ajout de la visée à maîtriser dans la collection EF (génère l'INSERT dans lien_visee_maitrise)
            visee.IdViseesMaitriserFks.Add(vm);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE /api/admin-data/lien-visee-maitrise/{idVisee}/{idVm}
        // Supprime le lien entre une visée et une visée à maîtriser
        [HttpDelete("lien-visee-maitrise/{idVisee:int}/{idVm:int}")]
        public async Task<IActionResult> DeleteLienViseeMaitrise(int idVisee, int idVm)
        {
            var visee = await _context.Visees
                .Include(v => v.IdViseesMaitriserFks)
                .FirstOrDefaultAsync(v => v.IdVisee == idVisee);
            if (visee == null) return NotFound();

            var vm = visee.IdViseesMaitriserFks.FirstOrDefault(x => x.IdViseesMaitriser == idVm);
            if (vm == null) return NotFound();

            // Retrait de la visée à maîtriser de la collection (génère le DELETE dans lien_visee_maitrise)
            visee.IdViseesMaitriserFks.Remove(vm);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ──────────────────────────────────────────────────────────────────
        // APPARTENIR VISÉE APTITUDE  (visées_maitriser ↔ aptitude ↔ compétence)
        // ──────────────────────────────────────────────────────────────────

        // GET /api/admin-data/appartenir-visee-aptitude
        // Retourne toutes les liaisons visée_maitriser + aptitude + compétence
        [HttpGet("appartenir-visee-aptitude")]
        public async Task<IActionResult> GetAppartenirViseeAptitude()
        {
            var list = await _context.AppartenirViseeAptitudes
                .Include(a => a.IdViseesMaitriserFkNavigation)
                .Include(a => a.IdAptitudeFkNavigation)
                .Include(a => a.IdCompetenceFkNavigation)
                .OrderBy(a => a.IdViseesMaitriserFkNavigation.NomViseesMaitriser)
                .Select(a => new
                {
                    a.IdAppartenirViseeAptitude,
                    a.IdViseesMaitriserFk, NomVm        = a.IdViseesMaitriserFkNavigation.NomViseesMaitriser,
                    a.IdAptitudeFk,        NomAptitude  = a.IdAptitudeFkNavigation != null ? a.IdAptitudeFkNavigation.NomAptitude : null,
                    a.IdCompetenceFk,      NomComp      = a.IdCompetenceFkNavigation.NomCompetence
                })
                .ToListAsync();
            return Ok(list);
        }

        // POST /api/admin-data/appartenir-visee-aptitude
        // Crée une liaison visée_maitriser + aptitude + compétence
        [HttpPost("appartenir-visee-aptitude")]
        public async Task<IActionResult> CreateAppartenirViseeAptitude([FromBody] CreateAppartenirDto dto)
        {
            var entry = new AppartenirViseeAptitude
            {
                IdViseesMaitriserFk = dto.IdViseesMaitriserFk,
                IdAptitudeFk        = dto.IdAptitudeFk,
                IdCompetenceFk      = dto.IdCompetenceFk
            };
            _context.AppartenirViseeAptitudes.Add(entry);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Erreur lors de la création de la liaison" }); }
            return Ok(new { entry.IdAppartenirViseeAptitude });
        }

        // DELETE /api/admin-data/appartenir-visee-aptitude/{id}
        // Supprime une liaison visée_maitriser + aptitude + compétence
        [HttpDelete("appartenir-visee-aptitude/{id:int}")]
        public async Task<IActionResult> DeleteAppartenirViseeAptitude(int id)
        {
            var entry = await _context.AppartenirViseeAptitudes.FindAsync(id);
            if (entry == null) return NotFound();
            _context.AppartenirViseeAptitudes.Remove(entry);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // DTOs DU CONTRÔLEUR AdminDataController
    // ──────────────────────────────────────────────────────────────────────

    // DTO de création pour une catégorie de cours
    public class CreateCategorieDto
    {
        public string NomCat { get; set; } = "";
        public int Ordre { get; set; }
    }

    // DTO de création pour un cours avec sa catégorie et sa couleur d'agenda
    public class CreateCoursAdminDto
    {
        public string NomCours    { get; set; } = "";
        public string CodeCours   { get; set; } = "";
        public string? PrefixCours  { get; set; }
        public string? CouleurAgenda { get; set; }
        public int? IdCatFk       { get; set; }
    }

    // DTO de création pour un niveau d'enseignement
    public class CreateNiveauAdminDto
    {
        public string CodeNiveau { get; set; } = "";
        public string NomNiveau  { get; set; } = "";
        public int? Ordre        { get; set; }
    }

    // DTO de création pour une liaison cours-niveau-professeur
    public class CreateCoursNiveauAdminDto
    {
        public int IdCoursFk  { get; set; }
        public int IdNiveauFk { get; set; }
        public int IdProfFk   { get; set; }
    }

    // DTO de création pour un domaine pédagogique
    public class CreateDomaineAdminDto
    {
        public string Nom            { get; set; } = "";
        public int IdCoursNiveauFk   { get; set; }
    }

    // DTO de création pour un sous-domaine
    public class CreateSousDomaineAdminDto
    {
        public string NomComp { get; set; } = "";
        public int IdDomFk    { get; set; }
    }

    // DTO générique pour les entités ne contenant qu'un nom (compétence, aptitude, nom visée, visée à maîtriser)
    public class SimpleNomAdminDto
    {
        public string Nom { get; set; } = "";
    }

    // DTO de création pour une visée (objectif d'apprentissage)
    public class CreateViseeAdminDto
    {
        public int  IdNomViseeFk    { get; set; }
        public int  IdDomaineFk     { get; set; }
        public int? IdSousDomaineFk { get; set; }
        public int  IdCompFk        { get; set; }
    }

    // DTO de création pour la liaison visée ↔ visée à maîtriser (lien_visee_maitrise)
    public class CreateLienViseeMaitriseDto
    {
        public int IdVisee           { get; set; }
        public int IdViseesMaitriser { get; set; }
    }

    // DTO de création pour la liaison visée_maitriser ↔ aptitude ↔ compétence (appartenir_visee_aptitude)
    public class CreateAppartenirDto
    {
        public int  IdViseesMaitriserFk { get; set; }
        public int? IdAptitudeFk        { get; set; }
        public int  IdCompetenceFk      { get; set; }
    }
}
