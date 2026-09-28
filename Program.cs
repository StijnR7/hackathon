using System;
using System.Linq;
using SportschoolDeKast.Models;
using SportschoolDeKast.Services;

namespace SportschoolDeKast
{
    // Presentatielaag: toont menu's, vraagt invoer en toont de meldingen die
    // de service-laag (Sportschool) teruggeeft. Bevat zelf geen bedrijfsregels.
    public static class Program
    {
        private static readonly Sportschool _sportschool = new Sportschool();

        public static void Main()
        {
            SeedData();

            Console.WriteLine("=================================================");
            Console.WriteLine("   SPORTSCHOOL 'DE KAST' - SELFSERVICE CLI");
            Console.WriteLine("=================================================");

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Hoofdmenu:");
                Console.WriteLine(" 1. Inloggen als sporter");
                Console.WriteLine(" 2. Inloggen als medewerker");
                Console.WriteLine(" 0. Afsluiten");

                switch (VraagTekst("> "))
                {
                    case "1": SporterSessie(); break;
                    case "2": MedewerkerSessie(); break;
                    case "0": Console.WriteLine("Tot ziens!"); return;
                    default: Console.WriteLine("Ongeldige keuze, probeer opnieuw."); break;
                }
            }
        }

        // ================================================================
        // SPORTER
        // ================================================================

        private static void SporterSessie()
        {
            int id = VraagGetal("Je sporter-ID (staat op je pas): ");
            var sporter = _sportschool.ZoekSporter(id);
            if (sporter == null)
            {
                Console.WriteLine("Onbekend sporter-ID.");
                return;
            }

            Console.WriteLine($"Welkom, {sporter.Naam}.");

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine($"Sportermenu ({sporter.Naam}):");
                Console.WriteLine(" 1. Inchecken (toegang)");
                Console.WriteLine(" 2. Mijn abonnement bekijken");
                Console.WriteLine(" 3. Inschrijven voor een cursus");
                Console.WriteLine(" 4. Cursusinschrijving annuleren");
                Console.WriteLine(" 5. Afspraak met personal coach");
                Console.WriteLine(" 6. Abonnement opzeggen");
                Console.WriteLine(" 0. Uitloggen");

                switch (VraagTekst("> "))
                {
                    case "1": Console.WriteLine(_sportschool.VerleenToegang(sporter.Id).Melding); break;
                    case "2": Console.WriteLine(_sportschool.StatusVan(sporter)); break;
                    case "3": CursusInschrijven(sporter); break;
                    case "4": CursusAnnuleren(sporter); break;
                    case "5": CoachAfspraak(sporter); break;
                    case "6": AbonnementOpzeggen(sporter, "Sporter"); break;
                    case "0": return;
                    default: Console.WriteLine("Ongeldige keuze, probeer opnieuw."); break;
                }
            }
        }

        private static void CursusInschrijven(Sporter sporter)
        {
            var recht = _sportschool.MagCursussenVolgen(sporter);
            if (!recht.Succes)
            {
                Console.WriteLine(recht.Melding);
                return;
            }

            ToonCursussen();
            string naam = VraagTekst("Naam van de cursus: ");
            var cursus = _sportschool.ZoekCursus(naam);

            // Inschrijving is pas geldig na bevestiging (US-05).
            if (cursus != null && !VraagJaNee($"Inschrijven voor {cursus.Naam} op {cursus.Moment}? (ja/nee): "))
            {
                Console.WriteLine("Inschrijving afgebroken.");
                return;
            }

            Console.WriteLine(_sportschool.SchrijfInVoorCursus(sporter.Id, naam).Melding);
        }

        private static void CursusAnnuleren(Sporter sporter)
        {
            var mijnCursussen = _sportschool.Cursussen
                .Where(c => c.IngeschrevenSporterIds.Contains(sporter.Id))
                .ToList();

            if (mijnCursussen.Count == 0)
            {
                Console.WriteLine("Je bent voor geen enkele cursus ingeschreven.");
                return;
            }

            Console.WriteLine("Je inschrijvingen:");
            foreach (var c in mijnCursussen)
                Console.WriteLine($" - {c.Naam} ({c.Moment})");

            string naam = VraagTekst("Welke cursus annuleren? ");
            Console.WriteLine(_sportschool.AnnuleerCursusInschrijving(sporter.Id, naam).Melding);
        }

        private static void CoachAfspraak(Sporter sporter)
        {
            Console.WriteLine("Beschikbare coaches en momenten:");
            foreach (var coach in _sportschool.Coaches)
            {
                string momenten = coach.BeschikbareMomenten.Count > 0
                    ? string.Join(", ", coach.BeschikbareMomenten)
                    : "(geen vrije momenten)";
                Console.WriteLine($" - {coach.Naam}: {momenten}");
            }

            string coachNaam = VraagTekst("Naam van de coach: ");
            string moment = VraagTekst("Gewenst moment (bv. 'Dinsdag 10:00'): ");
            Console.WriteLine(_sportschool.PlanAfspraakMetCoach(sporter.Id, coachNaam, moment).Melding);
        }

        // Annulering vraagt altijd eerst om bevestiging (US-02).
        private static void AbonnementOpzeggen(Sporter sporter, string door)
        {
            if (!VraagJaNee($"Weet je zeker dat je het abonnement van {sporter.Naam} wilt opzeggen? (ja/nee): "))
            {
                Console.WriteLine("Opzegging afgebroken.");
                return;
            }

            Console.WriteLine(_sportschool.AnnuleerAbonnement(sporter.Id, door).Melding);
        }

        // ================================================================
        // MEDEWERKER
        // ================================================================

        private static void MedewerkerSessie()
        {
            int id = VraagGetal("Medewerker-ID: ");
            string pincode = VraagTekst("Pincode: ");

            var medewerker = _sportschool.LogInMedewerker(id, pincode);
            if (medewerker == null)
            {
                // Bewust geen onderscheid tussen "onbekend ID" en "foute pincode".
                Console.WriteLine("Inloggen mislukt: ID of pincode onjuist.");
                return;
            }

            Console.WriteLine($"Ingelogd als {medewerker.Naam}.");

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine($"Medewerkermenu ({medewerker.Naam}):");
                Console.WriteLine(" 1. Sporter zoeken en status inzien");
                Console.WriteLine(" 2. Abonnement wijzigen");
                Console.WriteLine(" 3. Abonnement opzeggen namens sporter");
                Console.WriteLine(" 4. Overzicht cursussen");
                Console.WriteLine(" 5. Toegangslog bekijken");
                Console.WriteLine(" 6. Wijzigingslog bekijken");
                Console.WriteLine(" 0. Uitloggen");

                switch (VraagTekst("> "))
                {
                    case "1": ZoekSporter(); break;
                    case "2": WijzigAbonnement(medewerker); break;
                    case "3": OpzeggenNamensSporter(medewerker); break;
                    case "4": ToonCursussen(); break;
                    case "5": ToonLog("Toegangslog", _sportschool.ToegangsLog); break;
                    case "6": ToonLog("Wijzigingslog", _sportschool.WijzigingsLog); break;
                    case "0": return;
                    default: Console.WriteLine("Ongeldige keuze, probeer opnieuw."); break;
                }
            }
        }

        private static void ZoekSporter()
        {
            string zoekterm = VraagTekst("Zoek op naam of ID: ");
            var resultaten = _sportschool.ZoekSporters(zoekterm);

            if (resultaten.Count == 0)
            {
                Console.WriteLine("Geen sporters gevonden.");
                return;
            }

            foreach (var s in resultaten)
                Console.WriteLine(_sportschool.StatusVan(s));
        }

        private static void WijzigAbonnement(Medewerker medewerker)
        {
            int id = VraagGetal("Sporter-ID: ");
            var sporter = _sportschool.ZoekSporter(id);
            if (sporter == null)
            {
                Console.WriteLine("Onbekende sporter.");
                return;
            }

            Console.WriteLine("Huidig: " + _sportschool.StatusVan(sporter));
            Console.WriteLine("Nieuw abonnementstype:");
            Console.WriteLine(" 1. 1x per week");
            Console.WriteLine(" 2. 2x per week");
            Console.WriteLine(" 3. Onbeperkt");

            AbonnementType? type = VraagTekst("> ") switch
            {
                "1" => AbonnementType.EenKeerPerWeek,
                "2" => AbonnementType.TweeKeerPerWeek,
                "3" => AbonnementType.Onbeperkt,
                _ => null
            };

            if (type == null)
            {
                Console.WriteLine("Ongeldige keuze, er is niets gewijzigd.");
                return;
            }

            bool addendum = VraagJaNee("Cursus-addendum? (ja/nee): ");
            Console.WriteLine(_sportschool.WijzigAbonnement(medewerker, id, type.Value, addendum).Melding);
        }

        private static void OpzeggenNamensSporter(Medewerker medewerker)
        {
            int id = VraagGetal("Sporter-ID: ");
            var sporter = _sportschool.ZoekSporter(id);
            if (sporter == null)
            {
                Console.WriteLine("Onbekende sporter.");
                return;
            }

            AbonnementOpzeggen(sporter, $"Medewerker #{medewerker.Id}");
        }

        // ================================================================
        // GEDEELD
        // ================================================================

        private static void ToonCursussen()
        {
            Console.WriteLine("\n--- Cursussen ---");
            foreach (var c in _sportschool.Cursussen)
                Console.WriteLine($"{c.Naam} ({c.Moment}) - {c.IngeschrevenSporterIds.Count}/{c.MaxDeelnemers} plekken bezet");
        }

        private static void ToonLog(string titel, System.Collections.Generic.List<string> log)
        {
            Console.WriteLine($"\n--- {titel} ---");
            if (log.Count == 0)
            {
                Console.WriteLine("(leeg)");
                return;
            }
            foreach (var regel in log)
                Console.WriteLine(regel);
        }

        // ---- Invoerhulpen (validatie van alle gebruikersinvoer, US-08) ----

        private static string VraagTekst(string vraag)
        {
            Console.Write(vraag);
            string invoer = Console.ReadLine() ?? "0";
            return invoer.Trim();
        }

        private static int VraagGetal(string vraag)
        {
            while (true)
            {
                string invoer = VraagTekst(vraag);
                if (int.TryParse(invoer, out int getal) && getal >= 0)
                    return getal;

                Console.WriteLine("Ongeldige invoer, voer een geheel getal in (0 of hoger).");
            }
        }

        private static bool VraagJaNee(string vraag)
        {
            while (true)
            {
                string invoer = VraagTekst(vraag).ToLower();
                if (invoer == "ja" || invoer == "j") return true;
                if (invoer == "nee" || invoer == "n" || invoer == "0") return false;

                Console.WriteLine("Antwoord met 'ja' of 'nee'.");
            }
        }

        // Dummydata voor het prototype. De pincode (1234) is een demowaarde;
        // opgeslagen wordt alleen de hash.
        private static void SeedData()
        {
            _sportschool.Sporters.Add(new Sporter(1, "Anna de Vries", AbonnementType.EenKeerPerWeek));
            _sportschool.Sporters.Add(new Sporter(2, "Bram Jansen", AbonnementType.TweeKeerPerWeek, heeftCursusAddendum: true));
            _sportschool.Sporters.Add(new Sporter(3, "Chantal Bakker", AbonnementType.Onbeperkt, heeftCursusAddendum: true));
            _sportschool.Sporters.Add(new Sporter(4, "Dirk Smit", AbonnementType.EenKeerPerWeek));

            _sportschool.Cursussen.Add(new Cursus("Yoga", "Maandag 18:00", 2));
            _sportschool.Cursussen.Add(new Cursus("Pilates", "Woensdag 19:00", 2));
            _sportschool.Cursussen.Add(new Cursus("Paaldansen", "Vrijdag 20:00", 1));

            _sportschool.Coaches.Add(new Coach("Jan", new[] { "Dinsdag 10:00", "Dinsdag 11:00", "Donderdag 09:00" }));
            _sportschool.Coaches.Add(new Coach("Erik", new[] { "Maandag 08:00", "Woensdag 17:00" }));

            _sportschool.Medewerkers.Add(new Medewerker(1, "Receptie De Kast", Sportschool.HashPincode("1234")));
        }
    }
}
