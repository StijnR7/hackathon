using System;
using SportschoolDeKast.Models;
using SportschoolDeKast.Services;

namespace SportschoolDeKast
{
    public static class Program
    {
        private static readonly Sportschool _sportschool = new Sportschool();

        public static void Main()
        {
            SeedData();

            Console.WriteLine("=================================================");
            Console.WriteLine("   SPORTSCHOOL 'DE KAST' - RECEPTIE CLI");
            Console.WriteLine("=================================================");

            bool afsluiten = false;
            while (!afsluiten)
            {
                ToonMenu();
                string keuze = Console.ReadLine()?.Trim() ?? "";

                switch (keuze)
                {
                    case "1": VerleenToegangFlow(); break;
                    case "2": AnnuleerAbonnementFlow(); break;
                    case "3": SchrijfInVoorCursusFlow(); break;
                    case "4": AnnuleerCursusFlow(); break;
                    case "5": PlanCoachAfspraakFlow(); break;
                    case "6": ToonSporters(); break;
                    case "7": ToonCursussen(); break;
                    case "8": ToonToegangsLog(); break;
                    case "0": afsluiten = true; break;
                    default: Console.WriteLine("Ongeldige keuze, probeer opnieuw.\n"); break;
                }
            }

            Console.WriteLine("Tot ziens!");
        }

        private static void ToonMenu()
        {
            Console.WriteLine();
            Console.WriteLine("Kies een actie:");
            Console.WriteLine(" 1. Toegang verlenen (sporter inchecken)");
            Console.WriteLine(" 2. Abonnement annuleren");
            Console.WriteLine(" 3. Inschrijven voor een cursus");
            Console.WriteLine(" 4. Cursusinschrijving annuleren");
            Console.WriteLine(" 5. Afspraak inplannen met personal coach");
            Console.WriteLine(" 6. Overzicht sporters");
            Console.WriteLine(" 7. Overzicht cursussen");
            Console.WriteLine(" 8. Toegangslog bekijken");
            Console.WriteLine(" 0. Afsluiten");
            Console.Write("> ");
        }

        // ---- Flows: elk vraagt input via de console en valideert die,
        //      voordat de bijbehorende Sportschool-methode wordt aangeroepen. ----

        private static void VerleenToegangFlow()
        {
            int id = VraagSporterId();
            var (_, melding) = _sportschool.VerleenToegang(id);
            Console.WriteLine(melding);
        }

        private static void AnnuleerAbonnementFlow()
        {
            int id = VraagSporterId();
            var sporter = _sportschool.ZoekSporter(id);
            if (sporter == null)
            {
                Console.WriteLine("Onbekende sporter.");
                return;
            }

            Console.Write($"Weet je zeker dat je het abonnement van {sporter.Naam} wilt annuleren? (ja/nee): ");
            string bevestiging = Console.ReadLine()?.Trim().ToLower() ?? "";
            if (bevestiging != "ja")
            {
                Console.WriteLine("Annulering afgebroken.");
                return;
            }

            var (_, melding) = _sportschool.AnnuleerAbonnement(id);
            Console.WriteLine(melding);
        }

        private static void SchrijfInVoorCursusFlow()
        {
            int id = VraagSporterId();
            ToonCursussen();
            Console.Write("Naam van de cursus: ");
            string naam = Console.ReadLine()?.Trim() ?? "";

            var (_, melding) = _sportschool.SchrijfInVoorCursus(id, naam);
            Console.WriteLine(melding);
        }

        private static void AnnuleerCursusFlow()
        {
            int id = VraagSporterId();
            Console.Write("Naam van de cursus: ");
            string naam = Console.ReadLine()?.Trim() ?? "";

            var (_, melding) = _sportschool.AnnuleerCursusInschrijving(id, naam);
            Console.WriteLine(melding);
        }

        private static void PlanCoachAfspraakFlow()
        {
            int id = VraagSporterId();

            Console.WriteLine("Beschikbare coaches en momenten:");
            foreach (var coach in _sportschool.Coaches)
            {
                Console.WriteLine($" - {coach.Naam}: {string.Join(", ", coach.BeschikbareMomenten)}");
            }

            Console.Write("Naam van de coach: ");
            string coachNaam = Console.ReadLine()?.Trim() ?? "";
            Console.Write("Gewenst moment (exact overnemen, bv. 'Dinsdag 10:00'): ");
            string moment = Console.ReadLine()?.Trim() ?? "";

            var (_, melding) = _sportschool.PlanAfspraakMetCoach(id, coachNaam, moment);
            Console.WriteLine(melding);
        }

        private static void ToonSporters()
        {
            Console.WriteLine("\n--- Sporters ---");
            foreach (var s in _sportschool.Sporters)
            {
                Console.WriteLine($"#{s.Id} {s.Naam} | Abonnement: {s.Abonnement} | Actief: {s.AbonnementActief} | " +
                                   $"Cursus-addendum: {s.HeeftCursusAddendum} | Bezoeken deze week: {s.BezoekenDezeWeek}");
            }
        }

        private static void ToonCursussen()
        {
            Console.WriteLine("\n--- Cursussen ---");
            foreach (var c in _sportschool.Cursussen)
            {
                Console.WriteLine($"{c.Naam} ({c.Moment}) - {c.IngeschrevenSporterIds.Count}/{c.MaxDeelnemers} plekken bezet");
            }
        }

        private static void ToonToegangsLog()
        {
            Console.WriteLine("\n--- Toegangslog ---");
            if (_sportschool.ToegangsLog.Count == 0)
            {
                Console.WriteLine("(nog geen toegangspogingen)");
                return;
            }
            foreach (var regel in _sportschool.ToegangsLog)
                Console.WriteLine(regel);
        }

        // Vraagt om een sporter-ID en blijft vragen tot een geldig geheel
        // getal is ingevoerd (eenvoudige invoervalidatie).
        private static int VraagSporterId()
        {
            while (true)
            {
                Console.Write("Sporter-ID: ");
                string input = Console.ReadLine()?.Trim() ?? "";
                if (int.TryParse(input, out int id))
                    return id;

                Console.WriteLine("Ongeldige invoer, voer een geheel getal in.");
            }
        }

        // Vult de applicatie bij het opstarten met dummydata voor demo-doeleinden.
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
        }
    }
}
