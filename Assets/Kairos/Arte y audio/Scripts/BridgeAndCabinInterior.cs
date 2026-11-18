using UnityEngine;

namespace Kairos.Pruebas
{
    /// <summary>
    /// Sistema de ambientación interior modular para el puente de mando y camarote del contramaestre (Semana 13 / S13-3).
    /// Genera la disposición física, consolas de gobierno náutico, mesa de derrota y literas navales.
    /// </summary>
    public class BridgeAndCabinInterior : MonoBehaviour
    {
        [Header("Configuración de Puente de Mando")]
        public Color instrumentGlow = new Color(0.18f, 0.85f, 0.72f); // Fósforo verde/cian naval
        public Color nightCabinLight = new Color(0.95f, 0.42f, 0.15f); // Luz roja/ámbar de baja visión

        public GameObject DecorateBridge(Transform parent)
        {
            var bridgeRoot = new GameObject("PuenteDeMando_Decor");
            bridgeRoot.transform.SetParent(parent, false);

            // 1. Consola principal de derrota y timonería
            var consoleObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            consoleObj.name = "ConsolaTimoneria";
            consoleObj.transform.SetParent(bridgeRoot.transform, false);
            consoleObj.transform.localPosition = new Vector3(0f, 0.5f, 2.5f);
            consoleObj.transform.localScale = new Vector3(3.2f, 1.0f, 0.8f);

            var crtRadar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crtRadar.name = "RadarPantallaCircular";
            crtRadar.transform.SetParent(consoleObj.transform, false);
            crtRadar.transform.localPosition = new Vector3(-0.35f, 0.65f, 0f);
            crtRadar.transform.localScale = new Vector3(0.35f, 0.12f, 0.35f);
            crtRadar.transform.localRotation = Quaternion.Euler(30, 0, 0);

            // 2. Mesa de cartas náuticas
            var chartTable = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chartTable.name = "MesaCartasNauticas";
            chartTable.transform.SetParent(bridgeRoot.transform, false);
            chartTable.transform.localPosition = new Vector3(-2.2f, 0.45f, 0.5f);
            chartTable.transform.localScale = new Vector3(1.6f, 0.9f, 2.2f);

            // Flexo de iluminación sobre mesa de derrota
            var lampGo = new GameObject("FlexoMesaDerrota");
            lampGo.transform.SetParent(chartTable.transform, false);
            lampGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            var lampLight = lampGo.AddComponent<Light>();
            lampLight.type = LightType.Spot;
            lampLight.color = nightCabinLight;
            lampLight.intensity = 1.6f;
            lampLight.range = 3.5f;
            lampLight.spotAngle = 65f;

            return bridgeRoot;
        }

        public GameObject DecorateCabin(Transform parent)
        {
            var cabinRoot = new GameObject("CamaroteContramaestre_Decor");
            cabinRoot.transform.SetParent(parent, false);

            // 1. Litera naval doble empotrada al mamparo
            var bunkBed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bunkBed.name = "LiteraNaval";
            bunkBed.transform.SetParent(cabinRoot.transform, false);
            bunkBed.transform.localPosition = new Vector3(1.8f, 0.9f, 0f);
            bunkBed.transform.localScale = new Vector3(0.9f, 1.8f, 2.1f);

            // 2. Taquilla metálica de tripulación
            var locker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            locker.name = "TaquillaAcero";
            locker.transform.SetParent(cabinRoot.transform, false);
            locker.transform.localPosition = new Vector3(1.8f, 1.0f, -1.5f);
            locker.transform.localScale = new Vector3(0.7f, 2.0f, 0.8f);

            // 3. Aplique de mamparo tenue
            var wallLight = new GameObject("ApliqueMamparo");
            wallLight.transform.SetParent(cabinRoot.transform, false);
            wallLight.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            var l = wallLight.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = nightCabinLight;
            l.intensity = 0.85f;
            l.range = 4.2f;

            return cabinRoot;
        }
    }
}
