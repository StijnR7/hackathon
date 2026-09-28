using System;
using System.Linq;
using SportschoolDeKast.Models;
using SportschoolDeKast.Services;
using Xunit;

namespace SportschoolDeKast.Tests
{
    // ====================================================================
    // INTEGRATIETESTS - FASE 3 (WERKPROCES B1-K1-W4 "TEST SOFTWARE")
    // ====================================================================
    // Elke test komt overeen met een testgeval (TC-xx) uit Testplan.txt.
    // Per user story is er minimaal één hoofdscenario en één
    // alternatief scenario. Elke test bouwt een eigen Sportschool op met een
    // vaste klok, zodat de tests onafhankelijk en herhaalbaar zijn.
    // ====================================================================

    public class SportschoolTests
    {
        // Maandag 28 september 2026, 10:00.
        private static readonly DateTime Maandag = new DateTime(2026, 9, 28, 10, 0, 0);

        private static Sportschool MaakTestSportschool(DateTime? nu = null)
        {
            var sportschool = new Sportschool();
            var tijd = nu ?? Maandag;
            sportschool.Klok = () => tijd;

            sportschool.Sporters.Add(new Sporter(1, "Anna de Vries", AbonnementType.EenKeerPerWeek));
            sportschool.Sporters.Add(new Sporter(2, "Bram Jansen", AbonnementType.TweeKeerPerWeek, heeftCursusAddendum: true));
            sportschool.Sporters.Add(new Sporter(3, "Chantal Bakker", AbonnementType.Onbeperkt, heeftCursusAddendum: true));
            sportschool.Sporters.Add(new Sporter(4, "Dirk Smit", AbonnementType.EenKeerPerWeek));

            sportschool.Cursussen.Add(new Cursus("Yoga", "Maandag 18:00", 2));
            sportschool.Cursussen.Add(new Cursus("Pilates", "Woensdag 19:00", 2));
            sportschool.Cursussen.Add(new Cursus("Paaldansen", "Vrijdag 20:00", 1));

            sportschool.Coaches.Add(new Coach("Jan", new[] { "Dinsdag 10:00", "Dinsdag 11:00", "Donderdag 09:00" }));
            sportschool.Coaches.Add(new Coach("Erik", new[] { "Maandag 08:00", "Woensdag 17:00" }));

            sportschool.Medewerkers.Add(new Medewerker(1, "Receptie De Kast", Sportschool.HashPincode("1234")));

            return sportschool;
        }

        private static Medewerker Receptie(Sportschool s) => s.Medewerkers[0];

        // ----------------------------------------------------------------
        // US-01: Toegang op basis van abonnementstype
        // ----------------------------------------------------------------

        [Fact] // TC-01: hoofdscenario
        public void TC01_EenKeerPerWeek_EersteBezoek_GeeftToegang()
        {
            var s = MaakTestSportschool();

            var (toegestaan, melding) = s.VerleenToegang(1);

            Assert.True(toegestaan);
            Assert.Contains("toegang verleend", melding, StringComparison.OrdinalIgnoreCase);
        }

        [Fact] // TC-02: alternatief
        public void TC02_EenKeerPerWeek_TweedeBezoekZelfdeWeek_WordtGeweigerd()
        {
            var s = MaakTestSportschool();
            s.VerleenToegang(1);

            var (toegestaan, melding) = s.VerleenToegang(1);

            Assert.False(toegestaan);
            Assert.Contains("weeklimiet", melding, StringComparison.OrdinalIgnoreCase);
        }

        [Fact] // TC-03: alternatief
        public void TC03_TweeKeerPerWeek_DerdeBezoek_WordtGeweigerd()
        {
            var s = MaakTestSportschool();

            Assert.True(s.VerleenToegang(2).Toegestaan);
            Assert.True(s.VerleenToegang(2).Toegestaan);
            Assert.False(s.VerleenToegang(2).Toegestaan);
        }

        [Fact] // TC-04: hoofdscenario
        public void TC04_Onbeperkt_MeerdereBezoeken_GeeftAltijdToegang()
        {
            var s = MaakTestSportschool();

            for (int i = 0; i < 10; i++)
                Assert.True(s.VerleenToegang(3).Toegestaan);
        }

        [Fact] // TC-05: hoofdscenario - de weeklimiet reset in een nieuwe week
        public void TC05_NieuweWeek_GeeftWeerToegang()
        {
            var s = MaakTestSportschool();
            s.VerleenToegang(1);                      // maandag week 40
            s.Klok = () => Maandag.AddDays(6);        // zondag, nog steeds week 40
            Assert.False(s.VerleenToegang(1).Toegestaan);

            s.Klok = () => Maandag.AddDays(7);        // maandag week 41

            Assert.True(s.VerleenToegang(1).Toegestaan);
        }

        [Fact] // TC-06: alternatief
        public void TC06_OnbekendeSporter_WordtGeweigerd()
        {
            var s = MaakTestSportschool();

            var (toegestaan, melding) = s.VerleenToegang(999);

            Assert.False(toegestaan);
            Assert.Contains("Onbekende sporter", melding);
        }

        [Fact] // TC-07: acceptatiecriterium logging
        public void TC07_ToegangspogingenWordenGelogd()
        {
            var s = MaakTestSportschool();

            s.VerleenToegang(1);   // toegestaan
            s.VerleenToegang(1);   // geweigerd
            s.VerleenToegang(999); // geweigerd

            Assert.Equal(3, s.ToegangsLog.Count);
            Assert.Contains("verleend", s.ToegangsLog[0]);
            Assert.Contains("Geweigerd", s.ToegangsLog[1]);
        }

        // ----------------------------------------------------------------
        // US-02: Abonnement annuleren
        // ----------------------------------------------------------------

        [Fact] // TC-08: hoofdscenario - opzegging met bevestiging en opzegtermijn
        public void TC08_Opzeggen_ZetEinddatumNaOpzegtermijn()
        {
            var s = MaakTestSportschool();

            var (succes, melding) = s.AnnuleerAbonnement(1);

            Assert.True(succes);
            Assert.Equal(new DateTime(2026, 10, 28), s.ZoekSporter(1)!.Einddatum);
            Assert.Contains("Bevestiging", melding);
            Assert.Contains("28-10-2026", melding);
        }

        [Fact] // TC-09: alternatief
        public void TC09_DubbelOpzeggen_GeeftFoutmelding()
        {
            var s = MaakTestSportschool();
            s.AnnuleerAbonnement(1);

            var (succes, melding) = s.AnnuleerAbonnement(1);

            Assert.False(succes);
            Assert.Contains("al opgezegd", melding);
        }

        [Fact] // TC-10: integratie US-01 + US-02 - binnen opzegtermijn nog toegang
        public void TC10_BinnenOpzegtermijn_NogToegang()
        {
            var s = MaakTestSportschool();
            s.AnnuleerAbonnement(3);
            s.Klok = () => new DateTime(2026, 10, 27, 12, 0, 0); // dag voor einddatum

            Assert.True(s.VerleenToegang(3).Toegestaan);
        }

        [Fact] // TC-11: integratie US-01 + US-02 - na einddatum geen toegang
        public void TC11_NaEinddatum_GeenToegang()
        {
            var s = MaakTestSportschool();
            s.AnnuleerAbonnement(3);
            s.Klok = () => new DateTime(2026, 10, 28, 9, 0, 0); // op de einddatum

            var (toegestaan, melding) = s.VerleenToegang(3);

            Assert.False(toegestaan);
            Assert.Contains("niet actief", melding);
        }

        // ----------------------------------------------------------------
        // US-03: Abonnementsbeheer door medewerker
        // ----------------------------------------------------------------

        [Fact] // TC-12: hoofdscenario - zoeken op deel van de naam
        public void TC12_ZoekenOpNaam_VindtSporter()
        {
            var s = MaakTestSportschool();

            var resultaten = s.ZoekSporters("jans");

            Assert.Single(resultaten);
            Assert.Equal("Bram Jansen", resultaten[0].Naam);
        }

        [Fact] // TC-13: hoofdscenario - zoeken op ID en status inzien
        public void TC13_ZoekenOpId_ToontStatus()
        {
            var s = MaakTestSportschool();

            var sporter = s.ZoekSporters("3").Single();
            string status = s.StatusVan(sporter);

            Assert.Contains("Chantal Bakker", status);
            Assert.Contains("Actief", status);
        }

        [Fact] // TC-14: alternatief - zoeken zonder resultaat
        public void TC14_ZoekenZonderResultaat_GeeftLegeLijst()
        {
            var s = MaakTestSportschool();

            Assert.Empty(s.ZoekSporters("Onbekend"));
            Assert.Empty(s.ZoekSporters("   "));
        }

        [Fact] // TC-15: hoofdscenario - wijziging wordt uitgevoerd en gelogd
        public void TC15_AbonnementWijzigen_WordtGelogd()
        {
            var s = MaakTestSportschool();

            var (succes, _) = s.WijzigAbonnement(Receptie(s), 1, AbonnementType.Onbeperkt, true);

            Assert.True(succes);
            Assert.Equal(AbonnementType.Onbeperkt, s.ZoekSporter(1)!.Abonnement);
            Assert.True(s.ZoekSporter(1)!.HeeftCursusAddendum);
            Assert.Single(s.WijzigingsLog);
            Assert.Contains("Medewerker #1", s.WijzigingsLog[0]);
            Assert.Contains("Sporter #1", s.WijzigingsLog[0]);
        }

        [Fact] // TC-16: alternatief - wijziging zonder verandering
        public void TC16_WijzigenZonderVerandering_GeeftFoutmelding()
        {
            var s = MaakTestSportschool();

            var (succes, melding) = s.WijzigAbonnement(Receptie(s), 1, AbonnementType.EenKeerPerWeek, false);

            Assert.False(succes);
            Assert.Contains("niets gewijzigd", melding);
            Assert.Empty(s.WijzigingsLog);
        }

        [Fact] // TC-17: herleidbaarheid - opzegging door medewerker is terug te vinden
        public void TC17_OpzeggingDoorMedewerker_IsHerleidbaar()
        {
            var s = MaakTestSportschool();

            s.AnnuleerAbonnement(4, "Medewerker #1");

            Assert.Single(s.WijzigingsLog);
            Assert.Contains("Medewerker #1", s.WijzigingsLog[0]);
            Assert.Contains("sporter #4", s.WijzigingsLog[0]);
        }

        // ----------------------------------------------------------------
        // US-04 + US-05: Cursusinschrijving (addendum verplicht)
        // ----------------------------------------------------------------

        [Fact] // TC-18: hoofdscenario
        public void TC18_MetAddendum_InschrijvenLukt()
        {
            var s = MaakTestSportschool();

            var (succes, melding) = s.SchrijfInVoorCursus(2, "Yoga");

            Assert.True(succes);
            Assert.Contains("Bevestiging", melding);
            Assert.Contains(2, s.ZoekCursus("Yoga")!.IngeschrevenSporterIds);
        }

        [Fact] // TC-19: alternatief
        public void TC19_ZonderAddendum_InschrijvenWordtGeweigerd()
        {
            var s = MaakTestSportschool();

            var (succes, melding) = s.SchrijfInVoorCursus(1, "Yoga");

            Assert.False(succes);
            Assert.Contains("addendum", melding, StringComparison.OrdinalIgnoreCase);
        }

        [Fact] // TC-20: alternatief
        public void TC20_DubbeleInschrijving_WordtVoorkomen()
        {
            var s = MaakTestSportschool();
            s.SchrijfInVoorCursus(2, "Yoga");

            var (succes, melding) = s.SchrijfInVoorCursus(2, "yoga");

            Assert.False(succes);
            Assert.Contains("al ingeschreven", melding);
            Assert.Single(s.ZoekCursus("Yoga")!.IngeschrevenSporterIds);
        }

        [Fact] // TC-21: alternatief
        public void TC21_VolleCursus_WordtGeweigerd()
        {
            var s = MaakTestSportschool();
            s.SchrijfInVoorCursus(2, "Paaldansen");

            var (succes, melding) = s.SchrijfInVoorCursus(3, "Paaldansen");

            Assert.False(succes);
            Assert.Contains("vol", melding);
        }

        [Fact] // TC-22: alternatief
        public void TC22_NietBestaandeCursus_GeeftFoutmelding()
        {
            var s = MaakTestSportschool();

            var (succes, melding) = s.SchrijfInVoorCursus(2, "Kickboksen");

            Assert.False(succes);
            Assert.Contains("bestaat niet", melding);
        }

        [Fact] // TC-23: integratie US-03 + US-04 - addendum toegevoegd door medewerker
        public void TC23_NaToevoegenAddendum_KanSporterInschrijven()
        {
            var s = MaakTestSportschool();
            Assert.False(s.SchrijfInVoorCursus(4, "Pilates").Succes);

            s.WijzigAbonnement(Receptie(s), 4, AbonnementType.EenKeerPerWeek, true);

            Assert.True(s.SchrijfInVoorCursus(4, "Pilates").Succes);
        }

        [Fact] // TC-35: hertest bevinding B-01 - inschrijfrecht is vooraf te controleren
        public void TC35_InschrijfrechtVooraf_WeigertZonderAddendumEnNaEinddatum()
        {
            var s = MaakTestSportschool();

            Assert.False(s.MagCursussenVolgen(s.ZoekSporter(4)!).Succes); // geen addendum
            Assert.True(s.MagCursussenVolgen(s.ZoekSporter(2)!).Succes);

            s.AnnuleerAbonnement(2);
            s.Klok = () => Maandag.AddMonths(2);

            var (succes, melding) = s.MagCursussenVolgen(s.ZoekSporter(2)!);
            Assert.False(succes);
            Assert.Contains("geen actief abonnement", melding);
        }

        // ----------------------------------------------------------------
        // US-06: Cursusinschrijving annuleren
        // ----------------------------------------------------------------

        [Fact] // TC-24: hoofdscenario
        public void TC24_BestaandeInschrijving_Annuleren_MaaktPlekVrij()
        {
            var s = MaakTestSportschool();
            s.SchrijfInVoorCursus(2, "Yoga");

            var (succes, melding) = s.AnnuleerCursusInschrijving(2, "Yoga");

            Assert.True(succes);
            Assert.Contains("Bevestiging", melding);
            Assert.Empty(s.ZoekCursus("Yoga")!.IngeschrevenSporterIds);
        }

        [Fact] // TC-25: alternatief
        public void TC25_NietIngeschreven_AnnulerenGeeftFoutmelding()
        {
            var s = MaakTestSportschool();

            var (succes, melding) = s.AnnuleerCursusInschrijving(2, "Yoga");

            Assert.False(succes);
            Assert.Contains("niet ingeschreven", melding);
        }

        [Fact] // TC-26: integratie - vrijgekomen plek is beschikbaar voor een ander
        public void TC26_NaAnnuleren_KomtPlekVrijVoorAndereSporter()
        {
            var s = MaakTestSportschool();
            s.SchrijfInVoorCursus(2, "Paaldansen");
            s.AnnuleerCursusInschrijving(2, "Paaldansen");

            Assert.True(s.SchrijfInVoorCursus(3, "Paaldansen").Succes);
        }

        // ----------------------------------------------------------------
        // US-07: Afspraak met personal coach
        // ----------------------------------------------------------------

        [Fact] // TC-27: hoofdscenario
        public void TC27_BeschikbaarMoment_KanGeboektWorden()
        {
            var s = MaakTestSportschool();

            var (succes, melding) = s.PlanAfspraakMetCoach(1, "jan", "dinsdag 10:00");

            Assert.True(succes);
            Assert.Contains("bevestigd", melding);
            Assert.DoesNotContain("Dinsdag 10:00", s.Coaches[0].BeschikbareMomenten);
        }

        [Fact] // TC-28: alternatief
        public void TC28_DubbeleBoeking_WordtVoorkomen()
        {
            var s = MaakTestSportschool();
            s.PlanAfspraakMetCoach(1, "Jan", "Dinsdag 10:00");

            var (succes, melding) = s.PlanAfspraakMetCoach(4, "Jan", "Dinsdag 10:00");

            Assert.False(succes);
            Assert.Contains("niet beschikbaar", melding);
            Assert.Equal(1, s.Coaches[0].GeboekteMomenten["Dinsdag 10:00"]);
        }

        [Fact] // TC-29: alternatief
        public void TC29_OnbekendeCoach_GeeftFoutmelding()
        {
            var s = MaakTestSportschool();

            var (succes, melding) = s.PlanAfspraakMetCoach(1, "Sanne", "Dinsdag 10:00");

            Assert.False(succes);
            Assert.Contains("bestaat niet", melding);
        }

        [Fact] // TC-30: alternatief
        public void TC30_NietBeschikbaarMoment_GeeftFoutmelding()
        {
            var s = MaakTestSportschool();

            var (succes, melding) = s.PlanAfspraakMetCoach(1, "Jan", "Zaterdag 23:00");

            Assert.False(succes);
            Assert.Contains("niet beschikbaar", melding);
        }

        // ----------------------------------------------------------------
        // US-08: Bescherming persoonsgegevens / toegangscontrole
        // ----------------------------------------------------------------

        [Fact] // TC-31: hoofdscenario
        public void TC31_MedewerkerMetJuistePincode_KanInloggen()
        {
            var s = MaakTestSportschool();

            Assert.NotNull(s.LogInMedewerker(1, "1234"));
        }

        [Fact] // TC-32: alternatief
        public void TC32_FoutePincodeOfOnbekendId_InloggenMislukt()
        {
            var s = MaakTestSportschool();

            Assert.Null(s.LogInMedewerker(1, "0000"));
            Assert.Null(s.LogInMedewerker(1, ""));
            Assert.Null(s.LogInMedewerker(99, "1234"));
        }

        [Fact] // TC-33: pincode wordt niet leesbaar opgeslagen
        public void TC33_PincodeWordtAlleenAlsHashOpgeslagen()
        {
            var s = MaakTestSportschool();

            string opgeslagen = s.Medewerkers[0].PincodeHash;

            Assert.NotEqual("1234", opgeslagen);
            Assert.Equal(64, opgeslagen.Length); // SHA-256 in hex
        }

        [Fact] // TC-34: dataminimalisatie - de toegangslog bevat geen namen
        public void TC34_ToegangsLog_BevatGeenNamen()
        {
            var s = MaakTestSportschool();

            s.VerleenToegang(1);
            s.VerleenToegang(2);

            Assert.All(s.ToegangsLog, regel =>
            {
                Assert.DoesNotContain("Anna", regel);
                Assert.DoesNotContain("Bram", regel);
            });
        }
    }
}
