using System.Collections.Generic;
using NUnit.Framework;

namespace Kairos.Tests
{
    public class CaseRulesTests
    {
        const int AllLayers = (1 << 5) - 1;

        [Test] public void HaiFengConTodasLasCapasDa72() => Assert.AreEqual(72, CaseData.ComputeIR(CaseData.Target, AllLayers, 0));
        [Test] public void ElRiesgoCaeCincoPorDia() => Assert.AreEqual(67, CaseData.ComputeIR(CaseData.Target, AllLayers, 24));
        [Test] public void ElAisSoloNoBasta() => Assert.Less(CaseData.ComputeIR(CaseData.Target, 1, 0), CaseData.IrToScan);

        [Test]
        public void NingunOtroBuqueAlcanzaElUmbral()
        {
            foreach (var ship in CaseData.Ships)
                if (!ship.target) Assert.Less(CaseData.ComputeIR(ship, AllLayers, 0), CaseData.IrToScan, ship.name);
        }

        [Test]
        public void LaConfianzaPideMarcarBienYSonar()
        {
            int k4 = CaseData.PersonsCompartment;
            Assert.AreEqual(1, CaseData.Compartments[k4].kind);
            Assert.AreEqual(100f, CaseData.ComputeConfidence(k4, new[] { 7, 7, 7, 7, 7 }));
            Assert.GreaterOrEqual(CaseData.ComputeConfidence(k4, new[] { 0, 0, 0, 1, 0 }), CaseData.ConfidenceToBoard);
            Assert.Less(CaseData.ComputeConfidence(k4, new[] { 0, 0, 0, 6, 0 }), CaseData.ConfidenceToBoard);
            Assert.AreEqual(0f, CaseData.ComputeConfidence(0, new[] { 7, 7, 7, 7, 7 }));
            Assert.AreEqual(0f, CaseData.ComputeConfidence(-1, new[] { 7, 7, 7, 7, 7 }));
        }

        [TestCase(3, 0)]
        [TestCase(4, 1)]
        [TestCase(7, 2)]
        [TestCase(11, 3)]
        public void UmbralesDeSentencia(int indicators, int verdict) => Assert.AreEqual(verdict, CaseData.VerdictFor(indicators));

        [Test]
        public void LaCadenaLlegaAlBeneficiarioPorDosCaminos()
        {
            Assert.IsFalse(CaseData.ChainComplete(new List<string> { "0-3", "3-4" }));
            Assert.IsTrue(CaseData.ChainComplete(new List<string> { "0-3", "3-4", "4-5" }));
            Assert.IsTrue(CaseData.ChainComplete(new List<string> { "0-1", "1-5" }));
        }

        [Test]
        public void TodasLasPruebasCabenYCubrenLosOnceIndicadores()
        {
            var all = new List<string>();
            foreach (var e in CaseData.EvidenceDefs) all.Add(e.id);
            Assert.AreEqual(11, CaseData.CountIndicators(all));
            Assert.LessOrEqual(all.Count, CaseData.MaxEvidence);
            Assert.IsNotNull(CaseData.Evidence("e_radio"));
            Assert.IsNotNull(CaseData.Evidence("e_plotter"));
        }

        [Test]
        public void ElCaminoGuiadoBastaParaUnaSentenciaYNoSePuedePerder()
        {
            var path = new List<string> { "e_facturas", "e_celda", "t_ari", "e_manifiesto" };
            var links = new List<string>();
            foreach (var id in path) links.AddRange(CaseData.Evidence(id).edges);
            Assert.IsTrue(CaseData.ChainComplete(links));
            Assert.GreaterOrEqual(CaseData.VerdictFor(CaseData.CountIndicators(path)), 1);
            foreach (var id in CaseData.BurnOrder) CollectionAssert.DoesNotContain(path, id);
            foreach (var id in CaseData.DumpedWhenDetected) CollectionAssert.DoesNotContain(path, id);
        }

        [Test]
        public void PerderUnaPruebaUnicaDejaFueraLaCondena()
        {
            Assert.AreEqual(11, CaseData.MaxIndicators(new List<string>()));
            Assert.AreEqual(10, CaseData.MaxIndicators(new List<string> { "e_turnos" }));
            Assert.AreEqual(11, CaseData.MaxIndicators(new List<string> { "e_candados" }));
        }

        [Test]
        public void LasIncursionesSalenDeNocheSinCambiarDeDia()
        {
            Assert.AreEqual(4f, Game.NextNight(4f));
            Assert.AreEqual(22f, Game.NextNight(12f));
            Assert.AreEqual(46f, Game.NextNight(40f));
        }

        static PersonState Person(string id, int testimony = 0, params int[] dignified)
        {
            var st = new PersonState { id = id, testimony = testimony };
            foreach (var n in dignified) st.dignified[n] = true;
            return st;
        }

        static string Epilogue(PersonState st, int verdict) => CaseData.Epilogue(CaseData.PersonDefOf(st.id), st, verdict);

        [Test]
        public void ElEpilogoDeAriRespetaSuDecision()
        {
            StringAssert.DoesNotContain("Declaró", Epilogue(Person("ari"), 1));
            StringAssert.Contains("Declaró", Epilogue(Person("ari", 1), 1));
            StringAssert.DoesNotContain("ante el tribunal", Epilogue(Person("ari", 1), 0));
        }

        [Test]
        public void ElEpilogoDeMoussaSigueLoElegido()
        {
            StringAssert.DoesNotContain("costear", Epilogue(Person("moussa", 0, 0), 1));
            StringAssert.Contains("costear", Epilogue(Person("moussa"), 1));
            StringAssert.DoesNotContain("anulada", Epilogue(Person("moussa"), 0));
        }

        [Test]
        public void ElEpilogoDeThantYRodelSigueLoElegido()
        {
            StringAssert.DoesNotContain("sin apoyo", Epilogue(Person("thant", 0, 4), 1));
            StringAssert.DoesNotContain("Eligió", Epilogue(Person("thant"), 1));
            StringAssert.Contains("Eligió", Epilogue(Person("thant", 2), 1));
            StringAssert.DoesNotContain("reclamando", Epilogue(Person("rodel", 0, 3), 1));
        }

        [Test]
        public void SectoresDosYTresTienenFlotaTripulacionYNodosPropios()
        {
            for (int sector = 1; sector <= 2; sector++)
            {
                var ships = CaseData.ShipsFor(sector);
                var target = CaseData.TargetFor(sector);
                Assert.GreaterOrEqual(ships.Length, 3, $"Sector {sector + 1} debe tener flota completa.");
                Assert.IsTrue(target.target);
                Assert.GreaterOrEqual(CaseData.ComputeIR(target, AllLayers, 0), CaseData.IrToScan, target.name);

                foreach (var ship in ships)
                    if (!ship.target) Assert.Less(CaseData.ComputeIR(ship, AllLayers, 0), CaseData.IrToScan, ship.name);

                Assert.AreEqual(4, CaseData.PeopleFor(sector).Length, $"Sector {sector + 1} debe tener 4 tripulantes.");
                var nodes = CaseData.NodesFor(sector);
                Assert.AreEqual(6, nodes.Length, $"Sector {sector + 1} debe tener 6 nodos judiciales.");
                Assert.AreEqual(target.name, nodes[0].name);
                StringAssert.Contains(target.name, CaseData.CaseTitleFor(sector));
                Assert.IsNotEmpty(CaseData.CaseSummaryFor(sector));
            }
        }

        [Test]
        public void EscenasDeInfiltracionCorrespondenACadaSector()
        {
            Assert.AreEqual("03_Infiltracion", Game.InfiltrationScene(0));
            Assert.AreEqual("03_Infiltracion_S2", Game.InfiltrationScene(1));
            Assert.AreEqual("03_Infiltracion_S3", Game.InfiltrationScene(2));
        }

        [Test]
        public void DouglasEscalaConElSectorSinSuperarNueve()
        {
            int d0 = CaseData.DouglasFor(0, 0f);
            int d1 = CaseData.DouglasFor(1, 0f);
            int d2 = CaseData.DouglasFor(2, 0f);
            Assert.Less(d0, d1);
            Assert.Less(d1, d2);
            Assert.LessOrEqual(CaseData.DouglasFor(2, 48f), 9);
        }

        [Test]
        public void FormatoGpsReflejaCoordenadasDeCadaSector()
        {
            Assert.AreEqual("45°24'S 59°12'O", CaseData.FormatGps(0));
            Assert.AreEqual("2°18'N 2°24'E", CaseData.FormatGps(1));
            Assert.AreEqual("10°12'S 58°18'E", CaseData.FormatGps(2));
        }

        [Test]
        public void EvidenciasFisicasYTestimoniosSeAdaptanACadaSector()
        {
            string[] ids = { "e_comms", "e_deudas", "e_contratos", "e_manifiesto", "e_registro", "e_facturas", "e_radio" };
            for (int sector = 0; sector <= 2; sector++)
            {
                var n = CaseData.NodesFor(sector);
                var tRodel = CaseData.Evidence("t_rodel", sector);
                StringAssert.Contains(CaseData.PeopleFor(sector)[0].name, tRodel.title);

                StringAssert.Contains(n[4].name, CaseData.Evidence("e_comms", sector).desc);
                StringAssert.Contains(n[5].name, CaseData.Evidence("e_comms", sector).desc);
                StringAssert.Contains(n[4].name, CaseData.Evidence("e_deudas", sector).desc);
                StringAssert.Contains(n[2].name, CaseData.Evidence("e_contratos", sector).desc);
                StringAssert.Contains(n[1].name, CaseData.Evidence("e_manifiesto", sector).desc);
                StringAssert.Contains(n[3].name, CaseData.Evidence("e_registro", sector).desc);
                StringAssert.Contains(n[4].name, CaseData.Evidence("e_registro", sector).desc);
                StringAssert.Contains(n[1].name, CaseData.Evidence("e_facturas", sector).desc);
                StringAssert.Contains(n[5].name, CaseData.Evidence("e_facturas", sector).desc);
                StringAssert.Contains(n[4].name, CaseData.Evidence("e_radio", sector).desc);

                if (sector > 0)
                {
                    foreach (var id in ids)
                    {
                        var ev = CaseData.Evidence(id, sector);
                        StringAssert.DoesNotContain("Polar Meridian", ev.desc);
                        StringAssert.DoesNotContain("Cormorán Trade", ev.desc);
                        StringAssert.DoesNotContain("Tidewater Austral", ev.desc);
                        Assert.AreEqual(ev.desc, ev.descSafe);
                    }
                }
            }
        }

        [Test]
        public void ExpedienteYEpilogoReflejanElSectorYVeredicto()
        {
            Assert.AreEqual("KAIROS-JUD-2026-S1", CaseData.CaseFileCodeFor(0));
            Assert.AreEqual("KAIROS-JUD-2026-S2", CaseData.CaseFileCodeFor(1));
            Assert.AreEqual("KAIROS-JUD-2026-S3", CaseData.CaseFileCodeFor(2));

            StringAssert.Contains("sigue ahí fuera", CaseData.EpilogueFooterFor(0, 1, false));
            StringAssert.Contains("Golfo de Guinea", CaseData.EpilogueFooterFor(1, 2, false));
            StringAssert.Contains("condena penal", CaseData.EpilogueFooterFor(2, 3, false));
            StringAssert.Contains("Oceanic Crown 9", CaseData.EpilogueFooterFor(2, 1, false));
            StringAssert.Contains("Campaña completada", CaseData.EpilogueFooterFor(2, 2, true));
        }

        [Test]
        public void SubtituloDeIntroIncluyeBuqueSectorYGps()
        {
            for (int sector = 0; sector <= 2; sector++)
            {
                string sub = CaseData.SectorIntroSubtitle(sector);
                StringAssert.Contains(CaseData.TargetFor(sector).name, sub);
                StringAssert.Contains(CaseData.Sectors[sector].name, sub);
                StringAssert.Contains(CaseData.FormatGps(sector), sub);
            }
        }

        [Test]
        public void ProgresoGlobalDeCampanaCuentaSectoresYFormateaEtiqueta()
        {
            Assert.AreEqual(0, CaseData.CountCompletedSectors(null));
            Assert.AreEqual(1, CaseData.CountCompletedSectors(new[] { true }));
            Assert.AreEqual(0, CaseData.CountCompletedSectors(new[] { false, false, false }));
            Assert.AreEqual(2, CaseData.CountCompletedSectors(new[] { true, false, true }));
            Assert.AreEqual(3, CaseData.CountCompletedSectors(new[] { true, true, true }));

            StringAssert.Contains("0/3 SECTORES RESUELTOS", CaseData.CampaignProgressLabel(new[] { false, false, false }));
            StringAssert.Contains("2/3 SECTORES RESUELTOS", CaseData.CampaignProgressLabel(new[] { true, true, false }));
            StringAssert.Contains("CAMPAÑA COMPLETADA", CaseData.CampaignProgressLabel(new[] { true, true, true }));
            StringAssert.Contains("3/3 SECTORES", CaseData.CampaignProgressLabel(new[] { true, true, true }));
        }

        [Test]
        public void MaletinEvaluaCadenaSocietariaYProyeccionDeSentencia()
        {
            var empty = new List<string>();
            Assert.IsFalse(CaseData.BriefcaseCoversChain(empty));
            string emptySummary = CaseData.BriefcaseReadinessSummary(empty, 0);
            StringAssert.Contains("INCOMPLETA", emptySummary);
            StringAssert.Contains("Sin sentencia", emptySummary);
            StringAssert.Contains("0/11", emptySummary);

            var path = new List<string> { "e_facturas", "e_celda", "t_ari", "e_manifiesto" };
            Assert.IsTrue(CaseData.BriefcaseCoversChain(path));
            string readySummary = CaseData.BriefcaseReadinessSummary(path, 1);
            StringAssert.Contains("COMPLETA", readySummary);
            StringAssert.Contains("Gulf Maritime Holdings", readySummary);
            StringAssert.Contains("Orden de Retención", readySummary);
        }

        [Test]
        public void DossierDeCodiceDocumentaLosTresSectores()
        {
            for (int sector = 0; sector <= 2; sector++)
            {
                string open = CaseData.SectorCodexDossier(sector, false);
                string closed = CaseData.SectorCodexDossier(sector, true);
                StringAssert.Contains("[EN INVESTIGACIÓN]", open);
                StringAssert.Contains("[EXPEDIENTE RESUELTO]", closed);
                StringAssert.Contains(CaseData.Sectors[sector].name, open);
                StringAssert.Contains(CaseData.TargetFor(sector).name, open);
                StringAssert.Contains(CaseData.FormatGps(sector), open);
                StringAssert.Contains(CaseData.NodesFor(sector)[5].name, open);
            }
        }

        [Test]
        public void TripulacionDeSectoresDosYTresTieneTintesDeVestimentaDistintos()
        {
            string[] ids = { "rodel", "ari", "moussa", "thant" };
            foreach (var id in ids)
                Assert.AreEqual(UnityEngine.Color.white, CaseData.CrewOutfitTintFor(0, id));

            for (int sector = 1; sector <= 2; sector++)
            {
                var seen = new HashSet<UnityEngine.Color>();
                foreach (var id in ids)
                {
                    var tint = CaseData.CrewOutfitTintFor(sector, id);
                    Assert.AreNotEqual(UnityEngine.Color.white, tint);
                    Assert.IsTrue(seen.Add(tint), $"Sector {sector + 1} repite tinte para {id}");
                }
            }
        }
    }
}
