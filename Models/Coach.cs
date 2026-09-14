using System.Collections.Generic;

namespace SportschoolDeKast.Models
{
    // Een personal coach met een agenda van beschikbare/geboekte momenten.
    public class Coach
    {
        public string Naam { get; }

        // Beschikbare momenten (tekst, bv. "Dinsdag 10:00"). Zodra een moment
        // geboekt is, wordt het verplaatst naar GeboekteMomenten zodat het
        // niet dubbel geboekt kan worden.
        public List<string> BeschikbareMomenten { get; } = new List<string>();
        public Dictionary<string, int> GeboekteMomenten { get; } = new Dictionary<string, int>(); // moment -> sporterId

        public Coach(string naam, IEnumerable<string> beschikbareMomenten)
        {
            Naam = naam;
            BeschikbareMomenten.AddRange(beschikbareMomenten);
        }
    }
}
