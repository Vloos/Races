//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Proyecto_Inutil.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class etiquetas_noticias
    {
        public long id { get; set; }
        public long noticia_id { get; set; }
        public long etiqueta_id { get; set; }
    
        public virtual etiqueta etiqueta { get; set; }
        public virtual noticia noticia { get; set; }
    }
}