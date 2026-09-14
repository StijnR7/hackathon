using System;
using System.Collections.Generic;
using System.Linq;
using SportschoolDeKast.Models;

namespace SportschoolDeKast.Services
{
    // Bevat alle bedrijfsregels van De Kast. De CLI (Program) roept alleen
    // methoden op deze klasse aan; alle validatie en logica staat hier zodat
    // het herbruikbaar en testbaar is, los van de console-UI.
    public class Sportschool
    {
        public List<Sporter> Sporters { get; } = new List<Sporter>();
        public List<Cursus> Cursussen { get; } = new List<Cursus>();
        public List<Coach> Coaches { get; } = new List<Coach>();

        // Eenvoudige logging van toegangspogingen. In een echte applicatie
        // zou dit naar een bestand/database gaan; hier volstaat een lijst
        // in het geheugen.
        public List<string> ToegangsLog { get; } = new List<string>();

        public Sporter? ZoekSporter(int id) => Sporters.FirstOrDefault(s => s.Id == id);

        // Bepaalt of een sporter naar binnen mag en verwerkt het bezoek.
        // Regels:
        //  - Onbeperkt abonnement: altijd toegang (geen limiet).
        //  - 1x/2x per week: toegang zolang het aantal bezoeken deze week
        //    onder de limiet zit; daarna wordt de toegang geweigerd.
        //  - Een niet-actief (opgezegd) abonnement geeft nooit toegang.
        // Elke poging (geslaagd of geweigerd) wordt gelogd.
        public (bool Toegestaan, string Melding) VerleenToegang(int sporterId)
        {
            var sporter = ZoekSporter(sporterId);
            if (sporter == null)
            {
                Log(sporterId, "ONBEKEND", "Sporter niet gevonden");
                return (false, "Onbekende sporter. Toegang geweigerd.");
            }

            if (!sporter.AbonnementActief)
            {
                Log(sporterId, sporter.Naam, "Geweigerd: abonnement niet actief");
                return (false, $"{sporter.Naam}: abonnement is opgezegd/niet actief. Toegang geweigerd.");
            }

            int limiet = sporter.Abonnement switch
            {
                AbonnementType.EenKeerPerWeek => 1,
                AbonnementType.TweeKeerPerWeek => 2,
                AbonnementType.Onbeperkt => int.MaxValue,
                _ => 0
            };

            if (sporter.BezoekenDezeWeek >= limiet)
            {
                Log(sporterId, sporter.Naam, "Geweigerd: weeklimiet bereikt");
                return (false, $"{sporter.Naam}: weeklimiet ({limiet}x) al bereikt. Toegang geweigerd.");
            }

            sporter.BezoekenDezeWeek++;
            string limietTekst = limiet == int.MaxValue ? "onbeperkt" : limiet.ToString();
            Log(sporterId, sporter.Naam, $"Toegang verleend (bezoek {sporter.BezoekenDezeWeek}/{limietTekst})");
            return (true, $"{sporter.Naam}: toegang verleend. Welkom!");
        }

        private void Log(int sporterId, string naam, string bericht)
        {
            ToegangsLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Sporter #{sporterId} ({naam}): {bericht}");
        }

        // Zet het abonnement van een sporter op inactief. De aanroeper (CLI)
        // is verantwoordelijk voor het vragen van een bevestiging aan de
        // gebruiker voordat deze methode wordt aangeroepen.
        public (bool Succes, string Melding) AnnuleerAbonnement(int sporterId)
        {
            var sporter = ZoekSporter(sporterId);
            if (sporter == null)
                return (false, "Onbekende sporter.");

            if (!sporter.AbonnementActief)
                return (false, $"{sporter.Naam} had al geen actief abonnement.");

            sporter.AbonnementActief = false;
            return (true, $"Abonnement van {sporter.Naam} is geannuleerd. Toegang vervalt per direct.");
        }

        // Schrijft een sporter in voor een cursus. Vereist een geldig
        // cursus-addendum en voorkomt dubbele inschrijving voor hetzelfde
        // cursusmoment, en controleert de maximale capaciteit.
        public (bool Succes, string Melding) SchrijfInVoorCursus(int sporterId, string cursusNaam)
        {
            var sporter = ZoekSporter(sporterId);
            if (sporter == null)
                return (false, "Onbekende sporter.");

            if (!sporter.HeeftCursusAddendum)
                return (false, $"{sporter.Naam} heeft geen cursus-addendum. Inschrijven is niet mogelijk.");

            var cursus = Cursussen.FirstOrDefault(c => c.Naam.Equals(cursusNaam, StringComparison.OrdinalIgnoreCase));
            if (cursus == null)
                return (false, $"Cursus '{cursusNaam}' bestaat niet.");

            if (cursus.IngeschrevenSporterIds.Contains(sporterId))
                return (false, $"{sporter.Naam} is al ingeschreven voor {cursus.Naam}.");

            if (cursus.IsVol)
                return (false, $"Cursus {cursus.Naam} ({cursus.Moment}) zit vol.");

            cursus.IngeschrevenSporterIds.Add(sporterId);
            return (true, $"{sporter.Naam} is ingeschreven voor {cursus.Naam} ({cursus.Moment}).");
        }

        // Annuleert een cursusinschrijving; de vrijgekomen plek kan daarna
        // weer door iemand anders worden ingenomen.
        public (bool Succes, string Melding) AnnuleerCursusInschrijving(int sporterId, string cursusNaam)
        {
            var cursus = Cursussen.FirstOrDefault(c => c.Naam.Equals(cursusNaam, StringComparison.OrdinalIgnoreCase));
            if (cursus == null)
                return (false, $"Cursus '{cursusNaam}' bestaat niet.");

            if (!cursus.IngeschrevenSporterIds.Remove(sporterId))
                return (false, "Deze sporter was niet ingeschreven voor deze cursus.");

            return (true, $"Inschrijving voor {cursus.Naam} geannuleerd. De plek is weer vrij.");
        }

        // Boekt een beschikbaar moment bij een coach voor een sporter. Het
        // moment wordt verwijderd uit BeschikbareMomenten zodra het geboekt
        // is, zodat dubbele boeking van hetzelfde moment onmogelijk is.
        public (bool Succes, string Melding) PlanAfspraakMetCoach(int sporterId, string coachNaam, string moment)
        {
            var sporter = ZoekSporter(sporterId);
            if (sporter == null)
                return (false, "Onbekende sporter.");

            var coach = Coaches.FirstOrDefault(c => c.Naam.Equals(coachNaam, StringComparison.OrdinalIgnoreCase));
            if (coach == null)
                return (false, $"Coach '{coachNaam}' bestaat niet.");

            if (!coach.BeschikbareMomenten.Contains(moment))
                return (false, $"Moment '{moment}' is niet beschikbaar bij {coach.Naam}.");

            coach.BeschikbareMomenten.Remove(moment);
            coach.GeboekteMomenten[moment] = sporterId;

            return (true, $"Afspraak bevestigd: {sporter.Naam} met {coach.Naam} op {moment}.");
        }
    }
}
