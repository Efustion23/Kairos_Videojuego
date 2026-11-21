using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Kairos.Pruebas;

namespace Kairos.Pruebas.Tests
{
    [TestFixture]
    public class Semana15MasterDeliveryTests
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
                    UnityEngine.Object.DestroyImmediate(spawnedObjects[i]);
                }
            }
            spawnedObjects.Clear();
        }

        [Test]
        public void CrearCalibracionAudio_ConfiguraNivelesNominales()
        {
            var go = MasterAudioMixerCalibration.CreateMasterAudioCalibration(null, null);
            spawnedObjects.Add(go);

            var cal = go.GetComponent<MasterAudioMixerCalibration>();
            Assert.IsNotNull(cal, "Debe instanciarse el componente MasterAudioMixerCalibration.");

            Assert.AreEqual(0.0f, cal.masterVolumeDb, "El volumen maestro debe ser nominal a 0 dB.");
            Assert.Greater(cal.dialogueVolumeDb, cal.musicVolumeDb, "Las voces de los testigos deben tener prioridad (+2 dB) sobre la música (-6 dB).");

            cal.TransitionToState(AudioMixerSnapshotState.DialogoTestigo);
            Assert.AreEqual(AudioMixerSnapshotState.DialogoTestigo, cal.currentState);
        }

        [Test]
        public void CrearSecuenciadorCinematico_CargaCuatroTomasPorDefecto()
        {
            var go = TrailerCinematicSequencer.CreateCinematicSequencer(null, null);
            spawnedObjects.Add(go);

            var seq = go.GetComponent<TrailerCinematicSequencer>();
            Assert.IsNotNull(seq, "Debe contener el componente TrailerCinematicSequencer.");
            Assert.IsNotNull(seq.targetCamera, "Debe tener una cámara asignada.");
            Assert.AreEqual(4, seq.sequenceShots.Count, "Debe contener las 4 tomas cinematográficas del tráiler.");

            Assert.AreEqual("Toma1_BuqueEntreNiebla", seq.sequenceShots[0].shotName);
            Assert.Greater(seq.sequenceShots[0].duration, 0f);
        }

        [Test]
        public void PlaySequence_PosicionaCamaraEnPrimeraToma()
        {
            var go = TrailerCinematicSequencer.CreateCinematicSequencer(null, null);
            spawnedObjects.Add(go);

            var seq = go.GetComponent<TrailerCinematicSequencer>();
            seq.PlaySequence();

            Assert.IsTrue(seq.isPlaying);
            Assert.AreEqual(seq.sequenceShots[0].cameraPosition, seq.targetCamera.transform.position);
            Assert.AreEqual(seq.sequenceShots[0].fieldOfView, seq.targetCamera.fieldOfView);
        }

        [Test]
        public void VerificarSuiteCompletaChristopher_Semana12a15()
        {
            // Valida que todos los componentes de las semanas 12 a 15 de Christopher Veliz existan y sean instanciables
            Type[] christopherTypes = new Type[]
            {
                typeof(ThermalVisorBoatController),     // S12: Visor térmico de lancha
                typeof(SubmarineAudioSuite),            // S12: SFX submarinos y ping acústico
                typeof(SonarWaveRadialSweep),           // S12: Onda cian radial
                typeof(BridgeAndCabinInterior),         // S13: Interior del puente y camarote
                typeof(GuardLanternPatrolRig),          // S13: Rig de linterna de guardia
                typeof(InteriorSpatialAudio),           // S13: Pasos metálicos y compresor 3D
                typeof(InspectablePropItem),             // S14: Evidencias forenses
                typeof(AdaptiveTensionMusic),           // S14: Música adaptativa
                typeof(MaritimeFogAtmosphereController),// S15: Niebla volumétrica y reflectores
                typeof(MasterAudioMixerCalibration),    // S15: Balance maestro de audio
                typeof(TrailerCinematicSequencer)       // S15: Secuenciador cinematográfico de tráiler
            };

            for (int i = 0; i < christopherTypes.Length; i++)
            {
                Assert.IsNotNull(christopherTypes[i], $"El tipo {christopherTypes[i]} debe existir.");
                var testGo = new GameObject($"TypeTest_{christopherTypes[i].Name}");
                spawnedObjects.Add(testGo);
                var comp = testGo.AddComponent(christopherTypes[i]);
                Assert.IsNotNull(comp, $"No se pudo agregar el componente {christopherTypes[i].Name}.");
            }
        }

        [Test]
        public void SecuenciadorCinematico_CargaTomasParaLosTresSectores()
        {
            var go = TrailerCinematicSequencer.CreateCinematicSequencer(null, null);
            spawnedObjects.Add(go);
            var seq = go.GetComponent<TrailerCinematicSequencer>();

            seq.LoadSectorShots(0);
            Assert.AreEqual(4, seq.sequenceShots.Count);
            Assert.AreEqual("Toma1_BuqueEntreNiebla", seq.CurrentShot.shotName);

            seq.LoadSectorShots(1);
            Assert.AreEqual(4, seq.sequenceShots.Count);
            StringAssert.StartsWith("S2_", seq.CurrentShot.shotName);
            Assert.Greater(seq.TotalDuration, 10f);

            seq.LoadSectorShots(2);
            Assert.AreEqual(4, seq.sequenceShots.Count);
            StringAssert.StartsWith("S3_", seq.CurrentShot.shotName);
            Assert.Greater(seq.TotalDuration, 10f);
        }

        [Test]
        public void MasterAudioCalibration_SoportaPulsoSonarYEstados()
        {
            var go = MasterAudioMixerCalibration.CreateMasterAudioCalibration(null, null);
            spawnedObjects.Add(go);
            var cal = go.GetComponent<MasterAudioMixerCalibration>();

            cal.PulseSonarSnapshot(1.5f);
            Assert.AreEqual(AudioMixerSnapshotState.SonarActivo, cal.currentState);

            cal.TransitionToState(AudioMixerSnapshotState.AlertaMaxima);
            Assert.AreEqual(AudioMixerSnapshotState.AlertaMaxima, cal.currentState);
        }
    }
}
