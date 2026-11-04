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
    }
}
