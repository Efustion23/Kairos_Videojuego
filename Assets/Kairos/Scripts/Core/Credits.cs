namespace Kairos
{
    /// Recursos de terceros que usa el juego y su licencia. Copia con enlaces en Assets/Kairos/LICENCIAS.txt.
    public static class Credits
    {
        public static readonly (string group, string license, string[] lines)[] Groups =
        {
            ("Música", "CC0, vía OpenGameArt", new[]
            {
                "SRG774: Dark Sci-Fi Audio Pack",
                "congusbongus: Abandoned passages",
                "Graciano Choc (Bz.gamedev): GameOver or Underwater",
            }),
            ("Objetos 3D", "CC0, vía Poly Haven", new[]
            {
                "Serhii Khromov: Barrel 03",
                "Fabi_G: Plastic Crate 02",
                "Ulan Cabanilla: Medical Box, Metal Stool 02, Metal Office Desk, Plastic Jerrycan, Metal Jerrycan Green",
                "Aron Łyczek: Dining Table",
                "James Ray Cock: Utility Box 01",
                "Kuutti Siitonen: Industrial Wall Lamp",
                "Hank Kaamura: Lifebuoy",
                "Slinc: Propane Tank",
                "DaDrood: Binder Notebook",
            }),
            ("Personajes y Animación", "Modelos 3D propios / Tripo AI · Rigging y clips de Adobe Mixamo", new[]
            {
                "Modelos humanoid 3D: Inspectora KAIROS, Rodel Santos, Ari Wibowo, Moussa Diallo, Thant Zin, Guardias y Capitán",
                "Animaciones de locomoción, sigilo, interacción y diálogo: Adobe Mixamo (uso libre en proyectos compilados)",
            }),
            ("Audio Procedural y Shaders", "Autoría propia del equipo KAIROS (C# DSP y HLSL URP)", new[]
            {
                "Síntesis acústica de sonar, eco metálico, radio VHF/UHF, lluvia marina y música adaptativa",
                "Shaders URP: Océano nocturno, estela de lancha, barrido radial de sonar y resaltado forense",
            }),
            ("Tipografía", "SIL Open Font License", new[] { "Liberation Sans" }),
            ("Motor", "Unity Technologies", new[] { "Unity 6 LTS (6000.3.24f1)" }),
        };
    }
}
