namespace SportschoolDeKast.Models
{
    // De drie abonnementstypen van De Kast. Het cursus-addendum is geen
    // zelfstandig abonnement maar een aanvulling; dat staat daarom als
    // boolean op Sporter (HeeftCursusAddendum) in plaats van als eigen type.
    public enum AbonnementType
    {
        EenKeerPerWeek,
        TweeKeerPerWeek,
        Onbeperkt
    }
}
