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
    public class EntreprisesController : Controller
    {
        private readonly StagesDbContext _context;

        public EntreprisesController(StagesDbContext context)
        {
            _context = context;
        }

        // GET: Entreprises - Admin et Entreprise peuvent voir la liste
        [Authorize(Roles = "Admin,Entreprise")]
        public async Task<IActionResult> Index()
        {
            // Si l'utilisateur est Admin, on affiche toutes les entreprises
            if (User.IsInRole("Admin"))
            {
                return View(await _context.Entreprises.ToListAsync());
            }

            // Si c'est une Entreprise, on affiche seulement son profil
            var userEmail = User.Identity.Name;
            var entreprises = await _context.Entreprises
                .Where(e => e.EmailContact == userEmail)
                .ToListAsync();

            return View(entreprises);
        }

        // GET: Entreprises/Details/5 - Admin et Entreprise peuvent voir les détails
        [Authorize(Roles = "Admin,Entreprise")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entreprise = await _context.Entreprises
                .FirstOrDefaultAsync(m => m.Id == id);
            if (entreprise == null)
            {
                return NotFound();
            }

            // Si c'est une Entreprise, vérifier que c'est bien son profil
            if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                if (entreprise.EmailContact != userEmail)
                {
                    return Forbid();
                }
            }

            return View(entreprise);
        }

        // GET: Entreprises/Create - Seulement Admin peut créer
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Entreprises/Create - Seulement Admin peut créer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,Nom,Adresse,Telephone,EmailContact,Secteur")] Entreprise entreprise)
        {
            if (ModelState.IsValid)
            {
                _context.Add(entreprise);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(entreprise);
        }

        // GET: Entreprises/Edit/5 - Admin et Entreprise peuvent modifier
        [Authorize(Roles = "Admin,Entreprise")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entreprise = await _context.Entreprises.FindAsync(id);
            if (entreprise == null)
            {
                return NotFound();
            }

            // Si c'est une Entreprise, vérifier que c'est bien son profil
            if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                if (entreprise.EmailContact != userEmail)
                {
                    return Forbid();
                }
            }

            return View(entreprise);
        }

        // POST: Entreprises/Edit/5 - Admin et Entreprise peuvent modifier
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Entreprise")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nom,Adresse,Telephone,EmailContact,Secteur")] Entreprise entreprise)
        {
            if (id != entreprise.Id)
            {
                return NotFound();
            }

            // Si c'est une Entreprise, vérifier que c'est bien son profil
            if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                if (entreprise.EmailContact != userEmail)
                {
                    return Forbid();
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(entreprise);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EntrepriseExists(entreprise.Id))
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
            return View(entreprise);
        }

        // GET: Entreprises/Delete/5 - Seulement Admin peut supprimer
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entreprise = await _context.Entreprises
                .FirstOrDefaultAsync(m => m.Id == id);
            if (entreprise == null)
            {
                return NotFound();
            }

            return View(entreprise);
        }

        // POST: Entreprises/Delete/5 - Seulement Admin peut supprimer
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entreprise = await _context.Entreprises.FindAsync(id);
            if (entreprise != null)
            {
                _context.Entreprises.Remove(entreprise);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EntrepriseExists(int id)
        {
            return _context.Entreprises.Any(e => e.Id == id);
        }
    }
}
