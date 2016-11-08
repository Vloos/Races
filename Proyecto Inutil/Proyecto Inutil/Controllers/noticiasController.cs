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
using System.Collections.ObjectModel;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;

namespace Proyecto_Inutil.Controllers
{
    public class noticiasController : Controller
    {
        private epiEntities1 db = new epiEntities1();

        // GET: noticias
        public async Task<ActionResult> Index()
        {
            var noticias = db.noticias.Include(n => n.usuario);
            var tmp = await noticias.ToListAsync();
            return View(await noticias.ToListAsync());
        }

        // GET: noticias/Details/5
        public async Task<ActionResult> Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            noticia noticia = await db.noticias.FindAsync(id);
            if (noticia == null)
            {
                return HttpNotFound();
            }

            //el objetivo es que envíe a la vista un objeto EtNot que consta de la noticia y una lista de sus etiquetas
            // para ello en Models he hecho una clase llamada EtNot (de Etiquetas Noticia) que contiene la noticia y sus etiquetas
            // Con esta linea se consigue la lista de las etiquetas de la noticia:
            List<etiqueta> etiquetuelas = db.etiquetas.Where(s => s.etiquetas_noticias.Any(e => e.noticia_id == noticia.id)).ToList();
            EtNot meh = new EtNot(etiquetuelas, noticia);

            return View(meh);
        }

        // GET: noticias/Create
        public ActionResult Create()
        {
            ViewBag.usuario_id = new SelectList(db.usuarios, "id", "nombre");
            return View();
        }

        // POST: noticias/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.

        /*
         * hacer una clase (modelo) nueva que tenga la noticia y una linea de texto donde se pondrán las etiquetas separadas por comas.
         * la linea se procesa antes de db.noticias.add(noticia), cada etiqueta entre comas se compara con las etiquetas existentes 
         * (si no existe se añade a la tabla de etiquetas) por último se guarda la noticia (db.noticias.add(noticia)) se consigue la id
         * de la noticia y las id de las etiquetas y se guardan en la tabla etiquetas_noticias
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(NoticiaConEtiquetas noticia)
        {
            if (ModelState.IsValid)
            {
                noticia.not.usuario_id = noticia.usuario_id;
                if (noticia.stEt != null)
                {
                    // sacar las etiquetas de noticia.et y compararlas con las etiquetas existentes
                    // guardar en una lista las que si, y en otra las que no
                    List<etiqueta> estan = new List<etiqueta>();
                    List<etiqueta> noEstan = new List<etiqueta>();

                    string[] etStr = noticia.stEt.ToLower().Split(',');
                    List<string> meh = new List<string>();
                    for (int i = 0; i < etStr.Length; i++)
                    {
                        string tmp = (etStr[i].Trim());
                        if (db.etiquetas.Any(e => e.nombre == tmp))
                        {
                            etiqueta esta = await db.etiquetas.FirstAsync(e => e.nombre == tmp);
                            estan.Add(esta);
                        }
                        else
                        {
                            etiqueta noesta = new etiqueta();
                            noesta.nombre = tmp;
                            noEstan.Add(noesta);
                        }
                    }

                    /*
                    guardar noticia
                    guardar etiquetas que no están
                    asignar las etiquetas a la noticia:
                        buscar id de noticia
                        buscar id de etiquetas que están y que no están
                        meter las id de ambas cosas en una tabla
                    guardar bd
                    */

                    db.noticias.Add(noticia.not);

                    foreach (var item in noEstan)
                    {
                        db.etiquetas.Add(item);
                    }

                    foreach (var item in noEstan)
                    {
                        etiquetas_noticias etnot = new etiquetas_noticias();
                        etnot.etiqueta = item;
                        etnot.noticia = noticia.not;
                        db.etiquetas_noticias.Add(etnot);
                    }

                    foreach (var item in estan)
                    {
                        etiquetas_noticias etnot = new etiquetas_noticias();
                        etnot.etiqueta = item;
                        etnot.noticia = noticia.not;
                        db.etiquetas_noticias.Add(etnot);
                    }
                }
                else
                {
                    db.noticias.Add(noticia.not);
                }

                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.usuario_id = new SelectList(db.usuarios, "id", "nombre", noticia.not.usuario_id);
            return View("index");
        }

        /* La vista tiene que trabajar con un solo modelo, por lo tanto tene que recibir el mismo modelo que va a mandar.
         * Va a recibir el modelo NoticiaConEtiquetas.
         * Para ello necesita recuperar los nombres de las etiquetas relacionadas con la noticia y meterlas todas en una cadena de texto
         * separadas por coma.
         */

        // GET: noticias/Edit/5
        public async Task<ActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            noticia noticia = await db.noticias.FindAsync(id);
            NoticiaConEtiquetas ne = new NoticiaConEtiquetas();
            List<etiqueta> etiquetuelas = db.etiquetas.Where(s => s.etiquetas_noticias.Any(e => e.noticia_id == noticia.id)).ToList();
            ne.not = noticia;

            //si hay etiquetas se meten sus nombres en una cadena
            if (etiquetuelas.Count != 0)
            {
                string ets = "";
                foreach (var item in etiquetuelas)
                {
                    ets += item.nombre + ", ";
                }
                ets = ets.Trim(',', ' ').ToLower();
                ne.stEt = ets;
            }

            if (noticia == null)
            {
                return HttpNotFound();
            }

            ViewBag.usuario_id = new SelectList(db.usuarios, "id", "nombre", noticia.usuario_id);
            return View(ne);
        }

        // POST: noticias/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "id,fecha,titulo,contenido,usuario_id")] NoticiaConEtiquetas noticia)
        {
            if (ModelState.IsValid)
            {
                noticia not = noticia.not;
                Debug.Write("Codigo de identificación del autor segun el contenedor: " + noticia.usuario_id + "\n");
                Debug.Write("Codigo de identidad del autor de la noticia segun la noticia" + noticia.not.usuario_id + "\n");



                db.Entry(not).State = System.Data.Entity.EntityState.Modified;

                // TODO guardar aqui las etiquetas de la misma forma que se guardan en la acción create
                Debug.Write("cadena de etiquetas de la noticia: " + noticia.stEt + "\n");

                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.usuario_id = new SelectList(db.usuarios, "id", "nombre", noticia.usuario_id);
            return View(noticia);
        }

        // GET: noticias/Delete/5
        public async Task<ActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            noticia noticia = await db.noticias.FindAsync(id);
            if (noticia == null)
            {
                return HttpNotFound();
            }
            return View(noticia);
        }

        // POST: noticias/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            noticia noticia = await db.noticias.FindAsync(id);
            db.noticias.Remove(noticia);
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
