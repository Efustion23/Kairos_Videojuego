namespace Kairos
{
    /// Recursos de terceros que usa el juego y su licencia. Copia con enlaces en Assets/Kairos/LICENCIAS.txt.
    public static class Credits
    {
        public static readonly (string group, string license, string[] lines)[] Groups =
        {
            ("Equipo de Desarrollo KAIROS", "Proyecto Académico · Unity 6 LTS", new[]
            {
                "Alexander Aiken Benites Fierro — Dirección de Diseño, UI/UX, Narrativa y Escenas",
                "Diego Ali Gadiel Tornero Bermudez — Programación Principal, Mecánicas, Sonar e IA",
                "Christopher Jesus Veliz Cochachi — Dirección de Arte 3D, Entorno Marítimo, Iluminación y Audio",
            }),
            ("Música", "CC0, vía OpenGameArt", new[]
            {
                "SRG774: Dark Sci-Fi Audio Pack",
                "congusbongus: Abandoned passages",
                "Graciano Choc (Bz.gamedev): GameOver or Underwater",
            }),
            ("Efectos de sonido", "CC0, vía Kenney y OpenGameArt", new[]
            {
                "Kenney: Interface Sounds, Impact Sounds, RPG Audio, Sci-Fi Sounds",
                "kresiek-the-furry: AMB Rain Loop 1",
                "sketchman3: wind whoosh loop",
            }),
            ("Objetos 3D", "CC0, vía Poly Haven", new[]
            {
                "Serhii Khromov: Barrel 03",
                "Fabi_G: Plastic Crate 02",
                "Ulan Cabanilla: Medical Box, Metal Stool 02, Metal Office Desk, Plastic Jerrycan, Metal Jerrycan Green, Office Notepads",
                "Aron Łyczek: Dining Table",
                "James Ray Cock: Utility Box 01",
                "Kuutti Siitonen: Industrial Wall Lamp",
                "Hank Kaamura: Lifebuoy",
                "Slinc: Propane Tank",
                "DaDrood: Binder Notebook",
                "Poly Haven PBR Props: Ocean Buoy, Metal Toolbox, Old Military Crate",
                "Mateusz Sadek: Vintage Radio Transceiver",
                "Arrangemonk: Classic Laptop",
                "ProgrammerOnCoffee: Clipboard",
                "Remi Remix: Motorboat (CC BY 4.0, vía Sketchfab)",
                "floor2727: Jensen-class Stern Trawler, 62 m (CC BY 4.0, vía Sketchfab)",
                "martyn169: Oceania Passenger Ship (CC BY 4.0, vía Sketchfab)",
                "hungry_drifter: Cargo Ship (CC BY 4.0, vía Sketchfab)",
            }),
            ("Personajes y Animación", "Modelos 3D propios / Tripo AI · Rigging y clips de Adobe Mixamo", new[]
            {
                "Modelos humanoid 3D: Inspectora KAIROS, Guardias, Capitán y tripulaciones de los 3 sectores (Atlántico Sur, Golfo de Guinea y Océano Índico)",
                "Animaciones de locomoción, sigilo, interacción y diálogo: Adobe Mixamo (uso libre en proyectos compilados)",
            }),
            ("Audio Procedural y Shaders", "Autoría propia del equipo KAIROS (C# DSP y HLSL URP)", new[]
            {
                "Síntesis acústica de sonar, eco metálico, radio VHF/UHF, alarma y ambientes del barco",
                "Shaders URP: Océano nocturno, estela de lancha, barrido radial de sonar y resaltado forense",
            }),
            ("Tipografía", "SIL Open Font License", new[] { "Liberation Sans" }),
            ("Motor", "Unity Technologies", new[] { "Unity 6 LTS (6000.3.24f1)" }),
        };
    }
}
