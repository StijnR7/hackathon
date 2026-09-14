using System.Collections.Generic;

namespace SportschoolDeKast.Models
{
    // Een cursusmoment (yoga, pilates of paaldansen).
    public class Cursus
    {
        public string Naam { get; }
        public string Moment { get; } // bv. "Maandag 18:00" - als tekst voor eenvoud
        public int MaxDeelnemers { get; }
        public List<int> IngeschrevenSporterIds { get; } = new List<int>();

        public Cursus(string naam, string moment, int maxDeelnemers)
        {
            Naam = naam;
            Moment = moment;
            MaxDeelnemers = maxDeelnemers;
        }

        public bool IsVol => IngeschrevenSporterIds.Count >= MaxDeelnemers;
    }
}
