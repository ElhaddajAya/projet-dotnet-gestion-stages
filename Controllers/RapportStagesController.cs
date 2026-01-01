using GestionStages.Data;
using GestionStages.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestionStages.Controllers
{
    [Authorize] // Tous les utilisateurs connectés
    public class RapportStagesController : Controller
    {
        private readonly StagesDbContext _context;

        public RapportStagesController(StagesDbContext context)
        {
            _context = context;
        }

        // GET: RapportStages - Admin et Etudiant peuvent voir
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Index()
        {
            var stagesDbContext = _context.RapportsStages.Include(r => r.Convention);
            return View(await stagesDbContext.ToListAsync());
        }

        // GET: RapportStages/Details/5 - Admin et Etudiant peuvent voir les détails
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rapportStage = await _context.RapportsStages
                .Include(r => r.Convention)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (rapportStage == null)
            {
                return NotFound();
            }

            return View(rapportStage);
        }

        // GET: RapportStages/Create - Admin et Etudiant peuvent créer
        [Authorize(Roles = "Admin,Etudiant")]
        public IActionResult Create()
        {
            var conventions = _context.Conventions
                .Include(c => c.Candidature)
                    .ThenInclude(ca => ca.Etudiant)
                .Select(c => new
                {
                    c.Id,
                    Display = "Convention #" + c.Id + " - " + c.Candidature.Etudiant.Nom
                })
                .ToList();

            ViewData["ConventionId"] = new SelectList(conventions, "Id", "Display");
            return View();
        }

        // POST: RapportStages/Create - Admin et Etudiant peuvent créer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Create([Bind("Id,Titre,NomFichier,DateDepot,ConventionId")] RapportStage rapportStage)
        {
            if (ModelState.IsValid)
            {
                _context.Add(rapportStage);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var conventions = _context.Conventions
                .Include(c => c.Candidature)
                    .ThenInclude(ca => ca.Etudiant)
                .Select(c => new
                {
                    c.Id,
                    Display = "Convention #" + c.Id + " - " + c.Candidature.Etudiant.Nom
                })
                .ToList();

            ViewData["ConventionId"] = new SelectList(conventions, "Id", "Display", rapportStage.ConventionId);
            return View(rapportStage);
        }

        // GET: RapportStages/Edit/5 - Admin et Etudiant peuvent modifier
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rapportStage = await _context.RapportsStages.FindAsync(id);
            if (rapportStage == null)
            {
                return NotFound();
            }

            var conventions = _context.Conventions
                .Include(c => c.Candidature)
                    .ThenInclude(ca => ca.Etudiant)
                .Select(c => new
                {
                    c.Id,
                    Display = "Convention #" + c.Id + " - " + c.Candidature.Etudiant.Nom
                })
                .ToList();

            ViewData["ConventionId"] = new SelectList(conventions, "Id", "Display", rapportStage.ConventionId);
            return View(rapportStage);
        }

        // POST: RapportStages/Edit/5 - Admin et Etudiant peuvent modifier
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titre,NomFichier,DateDepot,ConventionId")] RapportStage rapportStage)
        {
            if (id != rapportStage.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(rapportStage);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RapportStageExists(rapportStage.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            var conventions = _context.Conventions
                .Include(c => c.Candidature)
                    .ThenInclude(ca => ca.Etudiant)
                .Select(c => new
                {
                    c.Id,
                    Display = "Convention #" + c.Id + " - " + c.Candidature.Etudiant.Nom
                })
                .ToList();

            ViewData["ConventionId"] = new SelectList(conventions, "Id", "Display", rapportStage.ConventionId);
            return View(rapportStage);
        }

        // GET: RapportStages/Delete/5 - Seulement Admin peut supprimer
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rapportStage = await _context.RapportsStages
                .Include(r => r.Convention)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (rapportStage == null)
            {
                return NotFound();
            }

            return View(rapportStage);
        }

        // POST: RapportStages/Delete/5 - Seulement Admin peut supprimer
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rapportStage = await _context.RapportsStages.FindAsync(id);
            if (rapportStage != null)
            {
                _context.RapportsStages.Remove(rapportStage);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RapportStageExists(int id)
        {
            return _context.RapportsStages.Any(e => e.Id == id);
        }
    }
}
