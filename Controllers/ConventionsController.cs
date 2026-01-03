using GestionStages.Data;
using GestionStages.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GestionStages.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ConventionsController : Controller
    {
        private readonly StagesDbContext _context;

        public ConventionsController(StagesDbContext context)
        {
            _context = context;
        }

        // GET: Conventions
        public async Task<IActionResult> Index()
        {
            var conventions = _context.Conventions
                .Include(c => c.Candidature)
                    .ThenInclude(cand => cand.Etudiant)
                .Include(c => c.Candidature)
                    .ThenInclude(cand => cand.OffreStage)
                        .ThenInclude(o => o.Entreprise);

            return View(await conventions.ToListAsync());
        }

        // GET: Conventions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var convention = await _context.Conventions
                .Include(c => c.Candidature)
                    .ThenInclude(cand => cand.Etudiant)
                .Include(c => c.Candidature)
                    .ThenInclude(cand => cand.OffreStage)
                        .ThenInclude(o => o.Entreprise)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (convention == null) return NotFound();

            return View(convention);
        }

        // GET: Conventions/Create
        public async Task<IActionResult> Create()
        {
            var candidatures = await _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                .Where(c => c.Statut == "Acceptée")
                .Select(c => new
                {
                    c.Id,
                    Display = c.Etudiant.Nom + " " + c.Etudiant.Prenom + " - " + c.OffreStage.Titre
                })
                .ToListAsync();

            ViewData["CandidatureId"] = new SelectList(candidatures, "Id", "Display");
            return View();
        }

        // POST: Conventions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DateSignature,DateDebut,DateFin,Statut,CandidatureId")] Convention convention)
        {
            ModelState.Remove("Candidature");
            ModelState.Remove("Rapport");

            if (ModelState.IsValid)
            {
                _context.Add(convention);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Convention créée avec succès !";
                return RedirectToAction(nameof(Index));
            }

            var candidatures = await _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                .Where(c => c.Statut == "Acceptée")
                .Select(c => new
                {
                    c.Id,
                    Display = c.Etudiant.Nom + " " + c.Etudiant.Prenom + " - " + c.OffreStage.Titre
                })
                .ToListAsync();

            ViewData["CandidatureId"] = new SelectList(candidatures, "Id", "Display", convention.CandidatureId);
            return View(convention);
        }

        // GET: Conventions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var convention = await _context.Conventions.FindAsync(id);
            if (convention == null) return NotFound();

            var candidatures = await _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                .Select(c => new
                {
                    c.Id,
                    Display = c.Etudiant.Nom + " " + c.Etudiant.Prenom + " - " + c.OffreStage.Titre
                })
                .ToListAsync();

            ViewData["CandidatureId"] = new SelectList(candidatures, "Id", "Display", convention.CandidatureId);
            return View(convention);
        }

        // POST: Conventions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DateSignature,DateDebut,DateFin,Statut,CandidatureId")] Convention convention)
        {
            if (id != convention.Id) return NotFound();

            ModelState.Remove("Candidature");
            ModelState.Remove("Rapport");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(convention);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Convention modifiée avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConventionExists(convention.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            var candidatures = await _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                .Select(c => new
                {
                    c.Id,
                    Display = c.Etudiant.Nom + " " + c.Etudiant.Prenom + " - " + c.OffreStage.Titre
                })
                .ToListAsync();

            ViewData["CandidatureId"] = new SelectList(candidatures, "Id", "Display", convention.CandidatureId);
            return View(convention);
        }

        // GET: Conventions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var convention = await _context.Conventions
                .Include(c => c.Candidature)
                    .ThenInclude(cand => cand.Etudiant)
                .Include(c => c.Candidature)
                    .ThenInclude(cand => cand.OffreStage)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (convention == null) return NotFound();

            return View(convention);
        }

        // POST: Conventions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var convention = await _context.Conventions.FindAsync(id);
            if (convention != null)
            {
                _context.Conventions.Remove(convention);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Convention supprimée avec succès !";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ConventionExists(int id)
        {
            return _context.Conventions.Any(e => e.Id == id);
        }
    }
}