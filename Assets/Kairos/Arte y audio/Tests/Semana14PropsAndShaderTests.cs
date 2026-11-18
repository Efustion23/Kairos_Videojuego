using NUnit.Framework;
using UnityEngine;
using Kairos.Pruebas;
using System.Collections.Generic;

namespace Kairos.Pruebas.Tests
{
    [TestFixture]
    public class Semana14PropsAndShaderTests
    {
        private List<GameObject> spawnedObjects;

        [SetUp]
        public void Setup()
        {
            spawnedObjects = new List<GameObject>();
        }

        [TearDown]
        public void Teardown()
        {
            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                if (spawnedObjects[i] != null)
                {
                    Object.DestroyImmediate(spawnedObjects[i]);
                }
            }
            spawnedObjects.Clear();
        }

        [Test]
        public void CrearBitacora_ConfiguraPropYColisionadorCorrectamente()
        {
            var bitacora = InspectableEvidenceProps.CreateBitacora(Vector3.zero);
            spawnedObjects.Add(bitacora);

            Assert.IsNotNull(bitacora, "El GameObject de la bitácora no debe ser nulo.");
            var col = bitacora.GetComponent<BoxCollider>();
            Assert.IsNotNull(col, "La bitácora debe contener un BoxCollider.");

            var item = bitacora.GetComponent<InspectablePropItem>();
            Assert.IsNotNull(item, "Debe tener el componente InspectablePropItem.");
            Assert.AreEqual(EvidenceType.BitacoraReal, item.evidenceType);
            StringAssert.Contains("OIT C188", item.oitViolationCode);
            Assert.IsTrue(item.targetRenderers.Count >= 2, "La bitácora debe tener renderers de portada y hojas.");
        }

        [Test]
        public void CrearPasaportes_ConfiguraPropYTargetRenderers()
        {
            var pasaportes = InspectableEvidenceProps.CreatePasaportes(new Vector3(1, 0, 0));
            spawnedObjects.Add(pasaportes);

            Assert.IsNotNull(pasaportes);
            var item = pasaportes.GetComponent<InspectablePropItem>();
            Assert.IsNotNull(item);
            Assert.AreEqual(EvidenceType.PasaportesRetenidos, item.evidenceType);
            StringAssert.Contains("OIT C029", item.oitViolationCode);
            Assert.AreEqual(3, item.targetRenderers.Count, "Debe registrar los 3 pasaportes apilados.");
        }

        [Test]
        public void CrearRadioVHFYPlotterGPS_GeneraMetadatosCompletos()
        {
            var radio = InspectableEvidenceProps.CreateRadioVHF(new Vector3(2, 0, 0));
            var plotter = InspectableEvidenceProps.CreatePlotterGPS(new Vector3(3, 0, 0));
            spawnedObjects.Add(radio);
            spawnedObjects.Add(plotter);

            var itemRadio = radio.GetComponent<InspectablePropItem>();
            var itemPlotter = plotter.GetComponent<InspectablePropItem>();

            Assert.AreEqual(EvidenceType.RadioVHFSatelital, itemRadio.evidenceType);
            Assert.AreEqual(EvidenceType.PlotterGPSNautico, itemPlotter.evidenceType);

            string summaryRadio = itemRadio.GetEvidenceSummary();
            string summaryPlotter = itemPlotter.GetEvidenceSummary();

            Assert.IsNotEmpty(summaryRadio);
            Assert.IsNotEmpty(summaryPlotter);
            StringAssert.Contains("Directrices", summaryRadio);
            StringAssert.Contains("AIS", summaryPlotter);
        }

        [Test]
        public void InspectablePropItem_SetHighlightYCollect_CambiaEstado()
        {
            var bitacora = InspectableEvidenceProps.CreateBitacora(Vector3.zero);
            spawnedObjects.Add(bitacora);
            var item = bitacora.GetComponent<InspectablePropItem>();

            item.SetHighlight(false);
            Assert.IsFalse(item.isHighlighted);

            item.SetHighlight(true);
            Assert.IsTrue(item.isHighlighted);

            bool collectSuccess = item.CollectEvidence();
            Assert.IsTrue(collectSuccess, "La primera recolección debe ser exitosa.");
            Assert.IsTrue(item.isCollected);
            Assert.IsFalse(item.isHighlighted, "Al recolectarse, el resplandor de advertencia debe apagarse.");

            bool secondCollect = item.CollectEvidence();
            Assert.IsFalse(secondCollect, "No debe permitir recolectarse más de una vez.");
        }

        [Test]
        public void CreateAllEvidenceCollection_GeneraCuatroEvidenciasDistintas()
        {
            var list = InspectableEvidenceProps.CreateAllEvidenceCollection(Vector3.zero);
            for (int i = 0; i < list.Count; i++)
            {
                spawnedObjects.Add(list[i]);
            }

            Assert.AreEqual(4, list.Count);
            var types = new HashSet<EvidenceType>();
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i].GetComponent<InspectablePropItem>();
                Assert.IsNotNull(item);
                types.Add(item.evidenceType);
            }

            Assert.AreEqual(4, types.Count, "Las 4 evidencias deben tener tipos únicos.");
        }
    }
}
