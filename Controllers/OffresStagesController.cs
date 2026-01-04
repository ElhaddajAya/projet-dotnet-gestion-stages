using GestionStages.Data;
using GestionStages.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace GestionStages.Controllers
{
    [Authorize]
    public class OffresStagesController : Controller
    {
        private readonly StagesDbContext _context;

        public OffresStagesController(StagesDbContext context)
        {
            _context = context;
        }

        // GET: OffresStages
        // AVEC RECHERCHE ET FILTRAGE
        public async Task<IActionResult> Index(string searchString, string secteurFilter, int? dureeFilter)
        {
            // Requête de base avec inclusion de l'entreprise
            var offresQuery = _context.OffresStages
                .Include(o => o.Entreprise)
                .AsQueryable();

            // FILTRAGE PAR RÔLE
            if (User.IsInRole("Entreprise"))
            {
                // L'entreprise voit uniquement ses offres
                var userEmail = User.Identity.Name;
                var entreprise = await _context.Entreprises
                    .FirstOrDefaultAsync(e => e.EmailContact == userEmail);

                if (entreprise != null)
                {
                    offresQuery = offresQuery.Where(o => o.EntrepriseId == entreprise.Id);
                }
            }
            // Étudiant et Admin voient toutes les offres

            // RECHERCHE PAR MOTS-CLÉS
            if (!string.IsNullOrEmpty(searchString))
            {
                offresQuery = offresQuery.Where(o =>
                    o.Titre.Contains(searchString) ||
                    o.Description.Contains(searchString) ||
                    o.Entreprise.Nom.Contains(searchString)
                );
            }

            // FILTRE PAR SECTEUR
            if (!string.IsNullOrEmpty(secteurFilter))
            {
                offresQuery = offresQuery.Where(o => o.Entreprise.Secteur == secteurFilter);
            }

            // FILTRE PAR DURÉE
            if (dureeFilter.HasValue && dureeFilter.Value > 0)
            {
                offresQuery = offresQuery.Where(o => o.DureeMois == dureeFilter.Value);
            }

            // TRI PAR DATE (plus récentes en premier)
            offresQuery = offresQuery.OrderByDescending(o => o.DatePublication);

            // PRÉPARER LES DONNÉES POUR LES FILTRES
            // Liste des secteurs distincts (exclure vides et null)
            var secteurs = await _context.Entreprises
                .Where(e => !string.IsNullOrWhiteSpace(e.Secteur))
                .Select(e => e.Secteur)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            ViewBag.Secteurs = secteurs;

            // Liste des durées distinctes
            var durees = await _context.OffresStages
                .Select(o => o.DureeMois)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            ViewBag.Durees = durees;

            // Conserver les valeurs des filtres pour les réafficher
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentSecteur = secteurFilter;
            ViewBag.CurrentDuree = dureeFilter;

            return View(await offresQuery.ToListAsync());
        }

        // GET: OffresStages/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var offreStage = await _context.OffresStages
                .Include(o => o.Entreprise)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (offreStage == null) return NotFound();

            return View(offreStage);
        }

        // GET: OffresStages/Create
        [Authorize(Roles = "Entreprise")]
        public async Task<IActionResult> Create()
        {
            // Récupérer l'entreprise connectée
            var userEmail = User.Identity.Name;
            var entreprise = await _context.Entreprises
                .FirstOrDefaultAsync(e => e.EmailContact == userEmail);

            if (entreprise == null)
            {
                TempData["Error"] = "Profil entreprise introuvable. Veuillez compléter votre profil.";
                return RedirectToAction("Index", "Entreprises");
            }

            // Passer le nom de l'entreprise à la vue
            ViewBag.NomEntreprise = entreprise.Nom;

            return View();
        }

        // POST: OffresStages/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Entreprise")]
        public async Task<IActionResult> Create([Bind("Titre,Description,DureeMois,DateDebutSouhaitee")] OffresStage offreStage)
        {
            ModelState.Remove("Entreprise");
            ModelState.Remove("EntrepriseId");
            ModelState.Remove("DatePublication");

            // Récupérer l'entreprise connectée
            var userEmail = User.Identity.Name;
            var entreprise = await _context.Entreprises
                .FirstOrDefaultAsync(e => e.EmailContact == userEmail);

            if (entreprise == null)
            {
                TempData["Error"] = "Profil entreprise introuvable.";
                return RedirectToAction("Index", "Entreprises");
            }

            offreStage.EntrepriseId = entreprise.Id;
            offreStage.DatePublication = DateTime.Now;

            if (ModelState.IsValid)
            {
                _context.Add(offreStage);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Offre de stage créée avec succès !";
                return RedirectToAction(nameof(Index));
            }

            // En cas d'erreur, repasser le nom
            ViewBag.NomEntreprise = entreprise.Nom;
            return View(offreStage);
        }

        // GET: OffresStages/Edit/5
        [Authorize(Roles = "Entreprise")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var offreStage = await _context.OffresStages
                .Include(o => o.Entreprise)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offreStage == null) return NotFound();

            // Vérifier que l'entreprise modifie bien sa propre offre
            var userEmail = User.Identity.Name;
            if (offreStage.Entreprise.EmailContact != userEmail)
            {
                TempData["Error"] = "Vous ne pouvez modifier que vos propres offres.";
                return RedirectToAction(nameof(Index));
            }

            // Passer le nom de l'entreprise à la vue
            ViewBag.NomEntreprise = offreStage.Entreprise.Nom;

            return View(offreStage);
        }

        // POST: OffresStages/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Entreprise")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titre,Description,DureeMois,DateDebutSouhaitee,DatePublication,EntrepriseId")] OffresStage offreStage)
        {
            if (id != offreStage.Id) return NotFound();

            ModelState.Remove("Entreprise");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(offreStage);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Offre de stage modifiée avec succès !";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OffreStageExists(offreStage.Id))
                        return NotFound();
                    else
                        throw;
                }
            }

            // En cas d'erreur, recharger l'entreprise
            var offreReload = await _context.OffresStages
                .Include(o => o.Entreprise)
                .FirstOrDefaultAsync(o => o.Id == id);

            ViewBag.NomEntreprise = offreReload?.Entreprise?.Nom ?? "";
            return View(offreStage);
        }

        // GET: OffresStages/Delete/5
        [Authorize(Roles = "Entreprise,Admin")]  // Admin peut supprimer
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var offreStage = await _context.OffresStages
                .Include(o => o.Entreprise)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (offreStage == null) return NotFound();

            // Vérifier que l'entreprise supprime bien sa propre offre (sauf Admin)
            if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                if (offreStage.Entreprise.EmailContact != userEmail)
                {
                    TempData["Error"] = "Vous ne pouvez supprimer que vos propres offres.";
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(offreStage);
        }

        // POST: OffresStages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Entreprise,Admin")]  // Admin peut supprimer
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var offreStage = await _context.OffresStages.FindAsync(id);
            if (offreStage != null)
            {
                _context.OffresStages.Remove(offreStage);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Offre de stage supprimée avec succès !";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool OffreStageExists(int id)
        {
            return _context.OffresStages.Any(e => e.Id == id);
        }
    }
}