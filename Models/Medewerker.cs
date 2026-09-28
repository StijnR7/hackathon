namespace SportschoolDeKast.Models
{
    // Een medewerker die abonnementen mag inzien en beheren.
    public class Medewerker
    {
        public int Id { get; }
        public string Naam { get; }

        // Alleen de SHA-256-hash van de pincode wordt bewaard, nooit de pincode zelf.
        public string PincodeHash { get; }

        public Medewerker(int id, string naam, string pincodeHash)
        {
            Id = id;
            Naam = naam;
            PincodeHash = pincodeHash;
        }
    }
}
