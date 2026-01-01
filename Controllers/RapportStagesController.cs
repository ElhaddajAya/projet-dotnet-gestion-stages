using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GestionStages.Data;
using GestionStages.Models;

namespace GestionStages.Controllers
{
    public class RapportStagesController : Controller
    {
        private readonly StagesDbContext _context;

        public RapportStagesController(StagesDbContext context)
        {
            _context = context;
        }

        // GET: RapportStages
        public async Task<IActionResult> Index()
        {
            var stagesDbContext = _context.RapportsStages.Include(r => r.Convention);
            return View(await stagesDbContext.ToListAsync());
        }

        // GET: RapportStages/Details/5
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

        // GET: RapportStages/Create
        public IActionResult Create()
        {
            ViewData["ConventionId"] = new SelectList(_context.Conventions, "Id", "Id");
            return View();
        }

        // POST: RapportStages/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Titre,NomFichier,DateDepot,ConventionId")] RapportStage rapportStage)
        {
            if (ModelState.IsValid)
            {
                _context.Add(rapportStage);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ConventionId"] = new SelectList(_context.Conventions, "Id", "Id", rapportStage.ConventionId);
            return View(rapportStage);
        }

        // GET: RapportStages/Edit/5
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
            ViewData["ConventionId"] = new SelectList(_context.Conventions, "Id", "Id", rapportStage.ConventionId);
            return View(rapportStage);
        }

        // POST: RapportStages/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
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
            ViewData["ConventionId"] = new SelectList(_context.Conventions, "Id", "Id", rapportStage.ConventionId);
            return View(rapportStage);
        }

        // GET: RapportStages/Delete/5
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

        // POST: RapportStages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
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
