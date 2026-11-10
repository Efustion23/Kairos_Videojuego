using NUnit.Framework;

namespace Kairos.Tests
{
    /// Reglas del clima, del satélite SAR y del oleaje que usa la Fase 1.
    public class RastreoRulesTests
    {
        const int AllLayers = (1 << 5) - 1;

        [TestCase(2f, true)]
        [TestCase(10f, false)]
        [TestCase(17f, true)]
        [TestCase(26f, true)]
        public void ElCieloSeNublaEnSusFranjas(float hours, bool cloudy) => Assert.AreEqual(cloudy, CaseData.Cloudy(hours));

        [TestCase(0f, true)]
        [TestCase(7.5f, true)]
        [TestCase(8f, false)]
        [TestCase(21f, false)]
        [TestCase(22f, true)]
        public void LaNocheCubreSusHoras(float hours, bool night) => Assert.AreEqual(night, CaseData.Night(hours));

        [Test]
        public void ElSarPasaEnVentanasDeUnaHora()
        {
            Assert.AreEqual(2f, CaseData.NextSarPass(0f));
            Assert.IsTrue(CaseData.SarNow(2.5f));
            Assert.IsFalse(CaseData.SarNow(3f));
            Assert.AreEqual(11f, CaseData.NextSarPass(3f));
            Assert.AreEqual(float.MaxValue, CaseData.NextSarPass(90f));
        }

        [TestCase(35.9f, 3)]
        [TestCase(36f, 6)]
        [TestCase(59.9f, 6)]
        [TestCase(60f, 4)]
        public void ElMarCambiaConLasHoras(float hours, int douglas) => Assert.AreEqual(douglas, CaseData.DouglasAt(hours));

        [Test]
        public void UnSarDegradadoBajaElRiesgo() =>
            Assert.Less(CaseData.ComputeIR(CaseData.Target, AllLayers, 0, sarDegraded: true), CaseData.ComputeIR(CaseData.Target, AllLayers, 0));
    }
}
