using SportschoolDeKast.Models;
using SportschoolDeKast.Services;
using Xunit;

namespace SportschoolDeKast.Tests
{
    // ====================================================================
    // TESTPLAN UITVOERING - FASE 3 (WERKPROCES B1-K1-W4 "TEST SOFTWARE")
    // ====================================================================
    // Elke test hieronder komt overeen met een scenario uit Testplan.txt.
    // Per user story is er minimaal:
    //   - 1 hoofdscenario (happy path)
    //   - 1 alternatief/negatief scenario
    // zoals gevraagd in de leeswijzer voor Fase 3.
    //
    // Testtype: unit-/integratietests op de service-laag (Sportschool.cs),
    // omdat daar alle bedrijfsregels/acceptatiecriteria worden afgehandeld.
    // Elke test bouwt zijn eigen Sportschool-object op (geen gedeelde state
    // tussen tests), zodat tests onafhankelijk en herhaalbaar zijn.
    // ====================================================================

    public class SportschoolTests
    {
        // Helper: bouwt een Sportschool met dezelfde soort dummydata als
        // Program.cs, zodat de testcondities overeenkomen met de praktijk.
        private static Sportschool MaakTestSportschool()
        {
            var sportschool = new Sportschool();

            sportschool.Sporters.Add(new Sporter(1, "Anna de Vries", AbonnementType.EenKeerPerWeek));
            sportschool.Sporters.Add(new Sporter(2, "Bram Jansen", AbonnementType.TweeKeerPerWeek, heeftCursusAddendum: true));
            sportschool.Sporters.Add(new Sporter(3, "Chantal Bakker", AbonnementType.Onbeperkt, heeftCursusAddendum: true));
            sportschool.Sporters.Add(new Sporter(4, "Dirk Smit", AbonnementType.EenKeerPerWeek));

            sportschool.Cursussen.Add(new Cursus("Yoga", "Maandag 18:00", 2));
            sportschool.Cursussen.Add(new Cursus("Pilates", "Woensdag 19:00", 2));
            sportschool.Cursussen.Add(new Cursus("Paaldansen", "Vrijdag 20:00", 1));

            sportschool.Coaches.Add(new Coach("Jan", new[] { "Dinsdag 10:00", "Dinsdag 11:00", "Donderdag 09:00" }));
            sportschool.Coaches.Add(new Coach("Erik", new[] { "Maandag 08:00", "Woensdag 17:00" }));

            return sportschool;
        }

        // ----------------------------------------------------------------
        // US-01: Toegang op basis van abonnementstype
        // ----------------------------------------------------------------

        [Fact] // TC-01: Hoofdscenario - sporter met "1x per week" mag 1x naar binnen
        public void TC01_EenKeerPerWeek_EersteBezoek_GeeftToegang()
        {
            var sportschool = MaakTestSportschool();

            var (toegestaan, melding) = sportschool.VerleenToegang(1); // Anna, EenKeerPerWeek

            Assert.True(toegestaan);
            Assert.Contains("toegang verleend", melding, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact] // TC-02: Alternatief scenario - tweede bezoek in dezelfde week wordt geweigerd
        public void TC02_EenKeerPerWeek_TweedeBezoekZelfdeWeek_WordtGeweigerd()
        {
            var sportschool = MaakTestSportschool();
            sportschool.VerleenToegang(1); // eerste (toegestane) bezoek

            var (toegestaan, melding) = sportschool.VerleenToegang(1); // tweede bezoek

            Assert.False(toegestaan);
            Assert.Contains("weeklimiet", melding, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact] // TC-03: Hoofdscenario - "Onbeperkt" abonnement heeft geen bezoeklimiet
        public void TC03_Onbeperkt_MeerdereBezoeken_GeeftAltijdToegang()
        {
            var sportschool = MaakTestSportschool();

            for (int i = 0; i < 5; i++)
            {
                var (toegestaan, _) = sportschool.VerleenToegang(3); // Chantal, Onbeperkt
                Assert.True(toegestaan);
            }
        }

        [Fact] // TC-04: Alternatief scenario - onbekende sporter-ID wordt geweigerd
        public void TC04_OnbekendeSporter_WordtGeweigerd()
        {
            var sportschool = MaakTestSportschool();

            var (toegestaan, melding) = sportschool.VerleenToegang(999);

            Assert.False(toegestaan);
            Assert.Contains("Onbekende sporter", melding);
        }

        [Fact] // TC-05: Alternatief scenario - geannuleerd abonnement geeft geen toegang
        public void TC05_GeannuleerdAbonnement_GeeftGeenToegang()
        {
            var sportschool = MaakTestSportschool();
            sportschool.AnnuleerAbonnement(1); // Anna annuleert eerst

            var (toegestaan, melding) = sportschool.VerleenToegang(1);

            Assert.False(toegestaan);
            Assert.Contains("niet actief", melding, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact] // TC-06: Acceptatiecriterium - toegangspogingen worden gelogd (geslaagd en geweigerd)
        public void TC06_ToegangspogingenWordenVastgelegdInLog()
        {
            var sportschool = MaakTestSportschool();

            sportschool.VerleenToegang(1); // geslaagd
            sportschool.VerleenToegang(1); // geweigerd (weeklimiet)

            Assert.Equal(2, sportschool.ToegangsLog.Count);
        }

        // ----------------------------------------------------------------
        // US-02: Abonnement annuleren
        // ----------------------------------------------------------------

        [Fact] // TC-07: Hoofdscenario - een actief abonnement kan geannuleerd worden
        public void TC07_ActiefAbonnement_KanGeannuleerdWorden()
        {
            var sportschool = MaakTestSportschool();

            var (succes, melding) = sportschool.AnnuleerAbonnement(1);

            Assert.True(succes);
            Assert.False(sportschool.ZoekSporter(1)!.AbonnementActief);
            Assert.Contains("geannuleerd", melding, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact] // TC-08: Alternatief scenario - een al-geannuleerd abonnement kan niet nogmaals geannuleerd worden
        public void TC08_AlGeannuleerdAbonnement_GeeftFoutmelding()
        {
            var sportschool = MaakTestSportschool();
            sportschool.AnnuleerAbonnement(1);

            var (succes, melding) = sportschool.AnnuleerAbonnement(1);

            Assert.False(succes);
            Assert.Contains("al geen actief abonnement", melding);
        }

        [Fact] // TC-09: Integratie - na annuleren vervalt de toegang direct (samenhang US-01 + US-02)
        public void TC09_NaAnnuleren_VervaltToegangDirect()
        {
            var sportschool = MaakTestSportschool();

            var (toegangVoor, _) = sportschool.VerleenToegang(2); // Bram mag eerst naar binnen
            sportschool.AnnuleerAbonnement(2);
            var (toegangNa, _) = sportschool.VerleenToegang(2);

            Assert.True(toegangVoor);
            Assert.False(toegangNa);
        }

        // ----------------------------------------------------------------
        // US-04 + US-05: Cursusinschrijving (addendum verplicht)
        // ----------------------------------------------------------------

        [Fact] // TC-10: Hoofdscenario - sporter met addendum kan zich inschrijven
        public void TC10_MetAddendum_InschrijvenLukt()
        {
            var sportschool = MaakTestSportschool();

            var (succes, melding) = sportschool.SchrijfInVoorCursus(2, "Yoga"); // Bram heeft addendum

            Assert.True(succes);
            Assert.Contains("Yoga", melding);
        }

        [Fact] // TC-11: Alternatief scenario - sporter zonder addendum kan zich niet inschrijven
        public void TC11_ZonderAddendum_InschrijvenWordtGeweigerd()
        {
            var sportschool = MaakTestSportschool();

            var (succes, melding) = sportschool.SchrijfInVoorCursus(1, "Yoga"); // Anna heeft geen addendum

            Assert.False(succes);
            Assert.Contains("addendum", melding, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact] // TC-12: Alternatief scenario - dubbele inschrijving voor dezelfde cursus wordt voorkomen
        public void TC12_DubbeleInschrijving_WordtVoorkomen()
        {
            var sportschool = MaakTestSportschool();
            sportschool.SchrijfInVoorCursus(2, "Yoga");

            var (succes, melding) = sportschool.SchrijfInVoorCursus(2, "Yoga");

            Assert.False(succes);
            Assert.Contains("al ingeschreven", melding);
        }

        [Fact] // TC-13: Alternatief scenario - volle cursus (max. capaciteit bereikt) wordt geweigerd
        public void TC13_VolleCursus_WordtGeweigerd()
        {
            var sportschool = MaakTestSportschool();
            // Paaldansen heeft maar 1 plek; sporter 2 vult deze op.
            sportschool.SchrijfInVoorCursus(2, "Paaldansen");

            var (succes, melding) = sportschool.SchrijfInVoorCursus(3, "Paaldansen"); // Chantal heeft ook addendum

            Assert.False(succes);
            Assert.Contains("vol", melding, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact] // TC-14: Alternatief scenario - inschrijven voor een niet-bestaande cursus geeft foutmelding
        public void TC14_NietBestaandeCursus_GeeftFoutmelding()
        {
            var sportschool = MaakTestSportschool();

            var (succes, melding) = sportschool.SchrijfInVoorCursus(2, "Kickboksen");

            Assert.False(succes);
            Assert.Contains("bestaat niet", melding);
        }

        // ----------------------------------------------------------------
        // US-06: Cursusinschrijving annuleren
        // ----------------------------------------------------------------

        [Fact] // TC-15: Hoofdscenario - een bestaande inschrijving annuleren maakt de plek weer vrij
        public void TC15_BestaandeInschrijving_KanGeannuleerdWorden()
        {
            var sportschool = MaakTestSportschool();
            sportschool.SchrijfInVoorCursus(2, "Yoga");

            var (succes, melding) = sportschool.AnnuleerCursusInschrijving(2, "Yoga");

            Assert.True(succes);
            Assert.Contains("geannuleerd", melding, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(2, sportschool.Cursussen.Find(c => c.Naam == "Yoga")!.IngeschrevenSporterIds);
        }

        [Fact] // TC-16: Alternatief scenario - annuleren zonder ingeschreven te zijn geeft foutmelding
        public void TC16_NietIngeschreven_AnnulerenGeeftFoutmelding()
        {
            var sportschool = MaakTestSportschool();

            var (succes, melding) = sportschool.AnnuleerCursusInschrijving(2, "Yoga");

            Assert.False(succes);
            Assert.Contains("niet ingeschreven", melding, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact] // TC-17: Integratie - na annuleren kan een andere sporter de vrijgekomen plek innemen
        public void TC17_NaAnnuleren_KomtPlekVrijVoorAndereSporter()
        {
            var sportschool = MaakTestSportschool();
            sportschool.SchrijfInVoorCursus(2, "Paaldansen"); // vult de enige plek
            sportschool.AnnuleerCursusInschrijving(2, "Paaldansen"); // maakt plek weer vrij

            var (succes, _) = sportschool.SchrijfInVoorCursus(3, "Paaldansen"); // Chantal neemt de plek

            Assert.True(succes);
        }

        // ----------------------------------------------------------------
        // US-07: Afspraak met personal coach
        // ----------------------------------------------------------------

        [Fact] // TC-18: Hoofdscenario - een beschikbaar moment kan geboekt worden
        public void TC18_BeschikbaarMoment_KanGeboektWorden()
        {
            var sportschool = MaakTestSportschool();

            var (succes, melding) = sportschool.PlanAfspraakMetCoach(1, "Jan", "Dinsdag 10:00");

            Assert.True(succes);
            Assert.Contains("bevestigd", melding, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact] // TC-19: Alternatief scenario - een reeds geboekt moment kan niet nogmaals geboekt worden
        public void TC19_DubbeleBoeking_WordtVoorkomen()
        {
            var sportschool = MaakTestSportschool();
            sportschool.PlanAfspraakMetCoach(1, "Jan", "Dinsdag 10:00");

            var (succes, melding) = sportschool.PlanAfspraakMetCoach(4, "Jan", "Dinsdag 10:00");

            Assert.False(succes);
            Assert.Contains("niet beschikbaar", melding, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact] // TC-20: Alternatief scenario - boeken bij een niet-bestaande coach geeft foutmelding
        public void TC20_OnbekendeCoach_GeeftFoutmelding()
        {
            var sportschool = MaakTestSportschool();

            var (succes, melding) = sportschool.PlanAfspraakMetCoach(1, "NietBestaandeCoach", "Dinsdag 10:00");

            Assert.False(succes);
            Assert.Contains("bestaat niet", melding);
        }

        [Fact] // TC-21: Alternatief scenario - boeken van een niet-bestaand/reeds vervallen moment geeft foutmelding
        public void TC21_NietBeschikbaarMoment_GeeftFoutmelding()
        {
            var sportschool = MaakTestSportschool();

            var (succes, melding) = sportschool.PlanAfspraakMetCoach(1, "Jan", "Zaterdag 23:00");

            Assert.False(succes);
            Assert.Contains("niet beschikbaar", melding, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
