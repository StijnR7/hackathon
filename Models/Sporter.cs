using System;
using System.Collections.Generic;

namespace SportschoolDeKast.Models
{
    // Een lid van de sportschool.
    public class Sporter
    {
        public int Id { get; }
        public string Naam { get; }
        public AbonnementType Abonnement { get; set; }
        public bool HeeftCursusAddendum { get; set; }

        // Null = lopend abonnement. Na opzegging staat hier de datum waarop de
        // opzegtermijn afloopt; tot die datum houdt de sporter nog toegang.
        public DateTime? Einddatum { get; set; }

        // Tijdstippen van toegestane bezoeken. Het aantal bezoeken "deze week"
        // wordt hieruit berekend, zodat de weeklimiet elke maandag vanzelf reset.
        public List<DateTime> Bezoeken { get; } = new List<DateTime>();

        public Sporter(int id, string naam, AbonnementType abonnement, bool heeftCursusAddendum = false)
        {
            Id = id;
            Naam = naam;
            Abonnement = abonnement;
            HeeftCursusAddendum = heeftCursusAddendum;
        }
    }
}
