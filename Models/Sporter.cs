namespace SportschoolDeKast.Models
{
    // Een lid van de sportschool.
    public class Sporter
    {
        public int Id { get; }
        public string Naam { get; }
        public AbonnementType Abonnement { get; set; }
        public bool HeeftCursusAddendum { get; set; }
        public bool AbonnementActief { get; set; } = true;

        // Aantal keer dat deze sporter deze week al is binnengelaten.
        // Wordt gebruikt om de bezoeklimiet van 1x/2x-per-week te handhaven.
        public int BezoekenDezeWeek { get; set; } = 0;

        public Sporter(int id, string naam, AbonnementType abonnement, bool heeftCursusAddendum = false)
        {
            Id = id;
            Naam = naam;
            Abonnement = abonnement;
            HeeftCursusAddendum = heeftCursusAddendum;
        }
    }
}
