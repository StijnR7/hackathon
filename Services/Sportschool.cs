using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SportschoolDeKast.Models;

namespace SportschoolDeKast.Services
{
    // Bevat alle bedrijfsregels van De Kast. De CLI (Program) roept alleen
    // methoden op deze klasse aan; alle validatie en logica staat hier zodat
    // het herbruikbaar en testbaar is, los van de console-UI.
    public class Sportschool
    {
        public const int OpzegtermijnInMaanden = 1;

        public List<Sporter> Sporters { get; } = new List<Sporter>();
        public List<Cursus> Cursussen { get; } = new List<Cursus>();
        public List<Coach> Coaches { get; } = new List<Coach>();
        public List<Medewerker> Medewerkers { get; } = new List<Medewerker>();

        // Logging (in het geheugen; in productie naar een bestand/database).
        public List<string> ToegangsLog { get; } = new List<string>();
        public List<string> WijzigingsLog { get; } = new List<string>();

        // Instelbare klok, zodat tests een vaste datum kunnen gebruiken
        // (bijv. om de opzegtermijn en de weekreset te testen).
        public Func<DateTime> Klok { get; set; } = () => DateTime.Now;

        public Sporter? ZoekSporter(int id) => Sporters.FirstOrDefault(s => s.Id == id);

        public bool IsAbonnementActief(Sporter sporter) =>
            sporter.Einddatum == null || Klok().Date < sporter.Einddatum.Value.Date;

        // Telt alleen bezoeken in dezelfde ISO-week als "nu".
        public int BezoekenDezeWeek(Sporter sporter)
        {
            var nu = Klok();
            return sporter.Bezoeken.Count(b =>
                ISOWeek.GetYear(b) == ISOWeek.GetYear(nu) &&
                ISOWeek.GetWeekOfYear(b) == ISOWeek.GetWeekOfYear(nu));
        }

        public static int WeekLimiet(AbonnementType type) => type switch
        {
            AbonnementType.EenKeerPerWeek => 1,
            AbonnementType.TweeKeerPerWeek => 2,
            _ => int.MaxValue
        };

        // ================================================================
        // US-08: Basale toegangscontrole medewerker
        // ================================================================

        public static string HashPincode(string pincode)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(pincode));
            return Convert.ToHexString(hash);
        }

        // Geeft de medewerker terug als ID en pincode kloppen, anders null.
        public Medewerker? LogInMedewerker(int medewerkerId, string pincode)
        {
            var medewerker = Medewerkers.FirstOrDefault(m => m.Id == medewerkerId);
            if (medewerker == null || string.IsNullOrWhiteSpace(pincode))
                return null;

            return medewerker.PincodeHash == HashPincode(pincode) ? medewerker : null;
        }

        // ================================================================
        // US-01: Toegang op basis van abonnementstype
        // ================================================================
        // Regels:
        //  - Onbeperkt: geen bezoeklimiet.
        //  - 1x/2x per week: toegang zolang de weeklimiet niet is bereikt.
        //  - Na afloop van de opzegtermijn: geen toegang meer.
        // Elke poging (geslaagd of geweigerd) wordt gelogd.
        public (bool Toegestaan, string Melding) VerleenToegang(int sporterId)
        {
            var sporter = ZoekSporter(sporterId);
            if (sporter == null)
            {
                LogToegang(sporterId, "Geweigerd: onbekende sporter");
                return (false, "Onbekende sporter. Toegang geweigerd.");
            }

            if (!IsAbonnementActief(sporter))
            {
                LogToegang(sporterId, "Geweigerd: abonnement niet actief");
                return (false, $"{sporter.Naam}: abonnement is opgezegd/niet actief. Toegang geweigerd.");
            }

            int limiet = WeekLimiet(sporter.Abonnement);
            int bezoeken = BezoekenDezeWeek(sporter);

            if (bezoeken >= limiet)
            {
                LogToegang(sporterId, "Geweigerd: weeklimiet bereikt");
                return (false, $"{sporter.Naam}: weeklimiet ({limiet}x) al bereikt. Toegang geweigerd.");
            }

            sporter.Bezoeken.Add(Klok());
            string limietTekst = limiet == int.MaxValue ? "onbeperkt" : limiet.ToString();
            LogToegang(sporterId, $"Toegang verleend (bezoek {bezoeken + 1}/{limietTekst})");
            return (true, $"{sporter.Naam}: toegang verleend. Welkom!");
        }

        // Logt alleen het sporter-ID, geen naam: de log bevat zo min mogelijk persoonsgegevens (US-08).
        private void LogToegang(int sporterId, string bericht)
        {
            ToegangsLog.Add($"[{Klok():yyyy-MM-dd HH:mm:ss}] Sporter #{sporterId}: {bericht}");
        }

        private void LogWijziging(string door, string bericht)
        {
            WijzigingsLog.Add($"[{Klok():yyyy-MM-dd HH:mm:ss}] {door}: {bericht}");
        }

        // ================================================================
        // US-02: Abonnement annuleren (met opzegtermijn)
        // ================================================================
        // De CLI vraagt eerst om bevestiging. Toegang blijft geldig tot het
        // einde van de opzegtermijn.
        public (bool Succes, string Melding) AnnuleerAbonnement(int sporterId, string door = "Sporter")
        {
            var sporter = ZoekSporter(sporterId);
            if (sporter == null)
                return (false, "Onbekende sporter.");

            if (sporter.Einddatum != null)
                return (false, $"{sporter.Naam} heeft het abonnement al opgezegd (einddatum {sporter.Einddatum:dd-MM-yyyy}).");

            sporter.Einddatum = Klok().Date.AddMonths(OpzegtermijnInMaanden);
            LogWijziging(door, $"Abonnement sporter #{sporterId} opgezegd, einddatum {sporter.Einddatum:dd-MM-yyyy}");
            return (true, $"Bevestiging: abonnement van {sporter.Naam} is opgezegd. " +
                          $"Toegang blijft geldig tot {sporter.Einddatum:dd-MM-yyyy}.");
        }

        // ================================================================
        // US-03: Abonnementsbeheer door medewerker
        // ================================================================

        // Zoekt op (deel van) de naam of op exact ID.
        public List<Sporter> ZoekSporters(string zoekterm)
        {
            zoekterm = zoekterm.Trim();
            if (zoekterm.Length == 0)
                return new List<Sporter>();

            if (int.TryParse(zoekterm, out int id))
                return Sporters.Where(s => s.Id == id).ToList();

            return Sporters
                .Where(s => s.Naam.Contains(zoekterm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public string StatusVan(Sporter sporter)
        {
            string status = sporter.Einddatum == null
                ? "Actief"
                : IsAbonnementActief(sporter)
                    ? $"Opgezegd, actief tot {sporter.Einddatum:dd-MM-yyyy}"
                    : $"Beëindigd per {sporter.Einddatum:dd-MM-yyyy}";

            int limiet = WeekLimiet(sporter.Abonnement);
            string limietTekst = limiet == int.MaxValue ? "onbeperkt" : limiet.ToString();

            return $"#{sporter.Id} {sporter.Naam} | {sporter.Abonnement} | {status} | " +
                   $"Cursus-addendum: {(sporter.HeeftCursusAddendum ? "ja" : "nee")} | " +
                   $"Bezoeken deze week: {BezoekenDezeWeek(sporter)}/{limietTekst}";
        }

        public (bool Succes, string Melding) WijzigAbonnement(Medewerker medewerker, int sporterId,
            AbonnementType nieuwType, bool nieuwAddendum)
        {
            var sporter = ZoekSporter(sporterId);
            if (sporter == null)
                return (false, "Onbekende sporter.");

            if (sporter.Abonnement == nieuwType && sporter.HeeftCursusAddendum == nieuwAddendum)
                return (false, "Er is niets gewijzigd.");

            string oud = $"{sporter.Abonnement}, addendum {(sporter.HeeftCursusAddendum ? "ja" : "nee")}";
            string nieuw = $"{nieuwType}, addendum {(nieuwAddendum ? "ja" : "nee")}";

            sporter.Abonnement = nieuwType;
            sporter.HeeftCursusAddendum = nieuwAddendum;

            LogWijziging($"Medewerker #{medewerker.Id}", $"Sporter #{sporterId} gewijzigd van [{oud}] naar [{nieuw}]");
            return (true, $"Abonnement van {sporter.Naam} gewijzigd naar {nieuw}.");
        }

        // ================================================================
        // US-04 + US-05: Inschrijven voor een cursus (addendum verplicht)
        // ================================================================
        // Los aan te roepen, zodat de CLI een sporter zonder recht direct kan
        // afwijzen in plaats van eerst de cursuslijst en bevestiging te tonen.
        public (bool Succes, string Melding) MagCursussenVolgen(Sporter sporter)
        {
            if (!IsAbonnementActief(sporter))
                return (false, $"{sporter.Naam} heeft geen actief abonnement.");

            if (!sporter.HeeftCursusAddendum)
                return (false, $"{sporter.Naam} heeft geen cursus-addendum. Inschrijven is niet mogelijk. " +
                               "Vraag een medewerker om een addendum toe te voegen.");

            return (true, "");
        }

        public (bool Succes, string Melding) SchrijfInVoorCursus(int sporterId, string cursusNaam)
        {
            var sporter = ZoekSporter(sporterId);
            if (sporter == null)
                return (false, "Onbekende sporter.");

            var recht = MagCursussenVolgen(sporter);
            if (!recht.Succes)
                return recht;

            var cursus = ZoekCursus(cursusNaam);
            if (cursus == null)
                return (false, $"Cursus '{cursusNaam}' bestaat niet.");

            if (cursus.IngeschrevenSporterIds.Contains(sporterId))
                return (false, $"{sporter.Naam} is al ingeschreven voor {cursus.Naam}.");

            if (cursus.IsVol)
                return (false, $"Cursus {cursus.Naam} ({cursus.Moment}) zit vol.");

            cursus.IngeschrevenSporterIds.Add(sporterId);
            return (true, $"Bevestiging: {sporter.Naam} is ingeschreven voor {cursus.Naam} ({cursus.Moment}).");
        }

        public Cursus? ZoekCursus(string cursusNaam) =>
            Cursussen.FirstOrDefault(c => c.Naam.Equals(cursusNaam.Trim(), StringComparison.OrdinalIgnoreCase));

        // ================================================================
        // US-06: Cursusinschrijving annuleren
        // ================================================================
        public (bool Succes, string Melding) AnnuleerCursusInschrijving(int sporterId, string cursusNaam)
        {
            var cursus = ZoekCursus(cursusNaam);
            if (cursus == null)
                return (false, $"Cursus '{cursusNaam}' bestaat niet.");

            if (!cursus.IngeschrevenSporterIds.Remove(sporterId))
                return (false, "Deze sporter was niet ingeschreven voor deze cursus.");

            return (true, $"Bevestiging: inschrijving voor {cursus.Naam} geannuleerd. De plek is weer vrij.");
        }

        // ================================================================
        // US-07: Afspraak met personal coach
        // ================================================================
        // Een geboekt moment verdwijnt uit BeschikbareMomenten, zodat het
        // niet dubbel geboekt kan worden.
        public (bool Succes, string Melding) PlanAfspraakMetCoach(int sporterId, string coachNaam, string moment)
        {
            var sporter = ZoekSporter(sporterId);
            if (sporter == null)
                return (false, "Onbekende sporter.");

            var coach = Coaches.FirstOrDefault(c => c.Naam.Equals(coachNaam.Trim(), StringComparison.OrdinalIgnoreCase));
            if (coach == null)
                return (false, $"Coach '{coachNaam}' bestaat niet.");

            string? gekozen = coach.BeschikbareMomenten
                .FirstOrDefault(m => m.Equals(moment.Trim(), StringComparison.OrdinalIgnoreCase));
            if (gekozen == null)
                return (false, $"Moment '{moment}' is niet beschikbaar bij {coach.Naam}.");

            coach.BeschikbareMomenten.Remove(gekozen);
            coach.GeboekteMomenten[gekozen] = sporterId;

            return (true, $"Afspraak bevestigd: {sporter.Naam} met {coach.Naam} op {gekozen}.");
        }
    }
}
