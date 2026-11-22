using NUnit.Framework;
using UnityEngine;

namespace Kairos.Tests
{
    /// Reglas del clima, del satélite SAR, del oleaje y de la contención volumétrica del sonar.
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

        [TestCase(1.0f, 1.0f, -45f, 45f, 6.2f)]     // Sector 1: Hai Feng 77
        [TestCase(1.32f, 0.96f, -43f, 43f, 9.6f)]   // Sector 2: Kestrel Explorer
        [TestCase(1.24f, 1.27f, -56.9f, 57.5f, 9.6f)] // Sector 3: Oceanic Crown 9 (CargoShip_3D)
        public void PuntosDelSonarQuedanDentroDelCascoSobreElAguaEnLosTresSectores(float beamScale, float lengthScale, float minZ, float maxZ, float deckY)
        {
            const float minY = 1.65f;
            var samples = new[]
            {
                new Vector3(25f, -8f, 70f),
                new Vector3(-25f, -4f, -70f),
                new Vector3(14f, 18f, 35f),
                new Vector3(-12f, 0.1f, 22f),
                new Vector3(6.5f, 2.0f, 40f),
            };

            foreach (var raw in samples)
            {
                var p = SonarController.ClampLocalPointInsideHull(raw, beamScale, lengthScale, minZ, maxZ, deckY, minY, 0.65f);
                Assert.GreaterOrEqual(p.y, minY - 0.01f, "Ningún punto del sonar debe quedar sumergido bajo el agua.");
                Assert.LessOrEqual(p.y, deckY, "Ningún punto del sonar debe sobresalir por encima de cubierta.");
                Assert.Greater(p.z, minZ, "Ningún punto debe sobresalir por popa.");
                Assert.Less(p.z, maxZ, "Ningún punto debe sobresalir por proa.");
                float halfW = SonarController.HullHalfWidthAt(p.z, p.y, beamScale, lengthScale, minZ, maxZ, deckY);
                Assert.LessOrEqual(Mathf.Abs(p.x), halfW, $"El punto {p} excede la semimanga real del casco ({halfW:0.00} m).");
            }
        }

        [TestCase(1.0f, 1.0f, -45f, 45f, 6.2f)]
        [TestCase(1.32f, 0.96f, -43f, 43f, 9.6f)]
        [TestCase(1.24f, 1.27f, -56.9f, 57.5f, 9.6f)]
        public void CompartimentosMapeadosQuedanDentroDelCascoVisible(float beamScale, float lengthScale, float minZ, float maxZ, float deckY)
        {
            const float minY = 1.65f;
            foreach (var k in CaseData.Compartments)
            {
                SonarController.MapCompartmentBounds(k, beamScale, lengthScale, minZ, maxZ, deckY, minY, out var center, out var size);
                Assert.Greater(center.y - size.y * 0.5f, 0.5f, $"{k.name} debe quedar por encima de la línea de flotación.");
                Assert.LessOrEqual(center.y + size.y * 0.5f, deckY, $"{k.name} debe quedar bajo la cubierta.");
                float halfW = SonarController.HullHalfWidthAt(center.z, center.y, beamScale, lengthScale, minZ, maxZ, deckY);
                Assert.Less(Mathf.Abs(center.x), halfW, $"{k.name} debe centrarse dentro de la manga real.");
            }
        }
    }
}
