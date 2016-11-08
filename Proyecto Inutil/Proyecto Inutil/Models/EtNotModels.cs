using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Proyecto_Inutil.Models
{

    /**
     * Una noticia con su coleccion de etiquetas
     */

    public class EtNot
    {
        public noticia not { get; set; }
        public List<etiqueta> et { get; set; }

        public EtNot(List<etiqueta> et, noticia not)
        {
            this.et = et;
            this.not = not;
        }
    }

    /**
     * Una etiqueta con su colección de noticias
     */
    public class NotEt
    {
        public List<noticia> not { get; set; }
        public etiqueta et { get; set; }

        public NotEt(etiqueta et, List<noticia> not)
        {
            this.et = et;
            this.not = not;
        }
    }

    public class NoticiaConEtiquetas {
        public noticia not { get; set; }
        public string stEt { get; set; }
        public long usuario_id { get; set; }
    }
}