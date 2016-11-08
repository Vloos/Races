using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Proyecto_Inutil.Models;

namespace Proyecto_Inutil.Controllers
{
    public class etiquetasController : Controller
    {
        private epiEntities1 db = new epiEntities1();

        // GET: etiquetas
        public async Task<ActionResult> Index()
        {
            return View(await db.etiquetas.ToListAsync());
        }

        // GET: etiquetas/Details/5
        public async Task<ActionResult> Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            etiqueta etiqueta = await db.etiquetas.FindAsync(id);
            if (etiqueta == null)
            {
                return HttpNotFound();
            }

            //el objetivo es que envíe a la vista un objeto NotEt que consta de la etiqueta y una lista de las noticias con esa etiqueta
            // para ello en Models he hecho una clase llamada NotEt (de Noticias Etiqueta) que contiene la etiqueta y sus noticias
            // Con esta linea se consigue la lista de las noticias de la etiqueta:
            List<noticia> noticielas = db.noticias.Where(s => s.etiquetas_noticias.Any(e => e.etiqueta_id == etiqueta.id)).ToList();
            NotEt meh = new NotEt(etiqueta, noticielas);

            return View(meh);
        }

        // GET: etiquetas/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: etiquetas/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "id,nombre")] etiqueta etiqueta)
        {
            if (ModelState.IsValid)
            {
                db.etiquetas.Add(etiqueta);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(etiqueta);
        }

        // GET: etiquetas/Edit/5
        public async Task<ActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            etiqueta etiqueta = await db.etiquetas.FindAsync(id);
            if (etiqueta == null)
            {
                return HttpNotFound();
            }
            return View(etiqueta);
        }

        // POST: etiquetas/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "id,nombre")] etiqueta etiqueta)
        {
            if (ModelState.IsValid)
            {
                db.Entry(etiqueta).State = System.Data.Entity.EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(etiqueta);
        }

        // GET: etiquetas/Delete/5
        public async Task<ActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            etiqueta etiqueta = await db.etiquetas.FindAsync(id);
            if (etiqueta == null)
            {
                return HttpNotFound();
            }
            return View(etiqueta);
        }

        // POST: etiquetas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            etiqueta etiqueta = await db.etiquetas.FindAsync(id);
            db.etiquetas.Remove(etiqueta);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
