using UnityEditor;
using UnityEngine;

namespace Kairos.EditorTools
{
    public static partial class KairosBuilder
    {
        static Material EvidenceGlowMat(string name, Color baseCol, Color glowCol)
        {
            var resMat = Resources.Load<Material>("KInspectableOutline");
            var sh = resMat != null ? resMat.shader : Shader.Find("Kairos/Pruebas/InspectableOutlineGlow");
            if (sh == null) return Lit("M_Ev_" + name, baseCol, .35f, 0);
            string path = $"{GenDir}/M_EvGlow_{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            else m.shader = sh;
            m.SetColor("_BaseColor", baseCol);
            m.SetColor("_GlowColor", glowCol);
            m.SetFloat("_HighlightStrength", 1.8f);
            m.SetFloat("_ActiveGlow", 1.0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// Cada prueba se reconoce por lo que es: una carpeta, unos pasaportes, un candado, radio VHF o plotter GPS.
        /// Se aplica el shader de outline glow forense y metadatos de inspección OIT.
        static void DressEvidence(EvidenceProp e, Vector3 scale)
        {
            var holder = new GameObject("Modelo").transform;
            holder.SetParent(e.transform, false);
            holder.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
            void HideBox() => e.GetComponent<MeshRenderer>().enabled = false;
            Color cyanGlowCol = new Color(0.18f, 0.96f, 1.0f, 1.0f);

            switch (e.evidenceId)
            {
                case "e_deudas":
                case "e_registro":
                    HideBox();
                    var binder = (GameObject)PrefabUtility.InstantiatePrefab(PropPrefab("binder_notebook"), holder);
                    binder.transform.localPosition = Vector3.down * .03f;
                    binder.transform.localRotation = Quaternion.Euler(0, e.evidenceId == "e_deudas" ? 20 : -35, 0);
                    binder.transform.localScale = Vector3.one * 1.7f;
                    break;
                case "e_pasaportes":
                    var cover = EvidenceGlowMat("pasaportes", new Color(.2f, .05f, .07f), cyanGlowCol);
                    e.GetComponent<MeshRenderer>().sharedMaterial = cover;
                    Part(holder, PrimitiveType.Cube, cover, new Vector3(.02f, .13f, -.03f), new Vector3(.28f, .02f, .2f)).localRotation = Quaternion.Euler(0, 12, 0);
                    break;
                case "e_candados":
                    HideBox();
                    Part(holder, PrimitiveType.Cube, steel, Vector3.zero, new Vector3(.05f, .14f, .13f));
                    Part(holder, PrimitiveType.Cylinder, darkMetal, new Vector3(0, .1f, -.04f), new Vector3(.018f, .05f, .018f));
                    Part(holder, PrimitiveType.Cylinder, darkMetal, new Vector3(0, .1f, .04f), new Vector3(.018f, .05f, .018f));
                    Part(holder, PrimitiveType.Cube, darkMetal, new Vector3(0, .15f, 0), new Vector3(.018f, .02f, .1f));
                    break;
                case "e_radio":
                    HideBox();
                    var radioChassis = EvidenceGlowMat("radio_chassis", new Color(.14f, .15f, .16f), new Color(1f, .55f, .12f, 1f));
                    var radioDial = EvidenceGlowMat("radio_dial", new Color(.35f, .38f, .40f), cyanGlowCol);
                    Part(holder, PrimitiveType.Cube, radioChassis, Vector3.zero, new Vector3(.28f, .14f, .22f));
                    Part(holder, PrimitiveType.Cube, screenMat, new Vector3(-.05f, .02f, .112f), new Vector3(.12f, .05f, .01f));
                    var dial = Part(holder, PrimitiveType.Cylinder, radioDial, new Vector3(.08f, .01f, .115f), new Vector3(.045f, .015f, .045f));
                    dial.localRotation = Quaternion.Euler(90, 0, 0);
                    Part(holder, PrimitiveType.Cylinder, radioDial, new Vector3(-.10f, .18f, -.07f), new Vector3(.015f, .16f, .015f));
                    Part(holder, PrimitiveType.Cube, rubber, new Vector3(.16f, 0f, .02f), new Vector3(.04f, .05f, .13f));
                    break;
                case "e_plotter":
                    HideBox();
                    var plotterBody = EvidenceGlowMat("plotter_body", new Color(.18f, .20f, .22f), cyanGlowCol);
                    var plotterScreen = EvidenceGlowMat("plotter_screen", new Color(.05f, .38f, .34f), new Color(.2f, .98f, .8f, 1f));
                    var housing = Part(holder, PrimitiveType.Cube, plotterBody, new Vector3(0, .04f, 0), new Vector3(.32f, .22f, .08f));
                    housing.localRotation = Quaternion.Euler(-15f, 0, 0);
                    var lcd = Part(holder, PrimitiveType.Cube, plotterScreen, new Vector3(-.02f, .045f, .044f), new Vector3(.23f, .16f, .01f));
                    lcd.localRotation = Quaternion.Euler(-15f, 0, 0);
                    Part(holder, PrimitiveType.Cube, darkMetal, new Vector3(0, -.08f, -.02f), new Vector3(.22f, .04f, .14f));
                    break;
                default:
                    var mr = e.GetComponent<MeshRenderer>();
                    if (mr != null && mr.sharedMaterial != null)
                        mr.sharedMaterial = EvidenceGlowMat(e.evidenceId, mr.sharedMaterial.color, cyanGlowCol);
                    break;
            }

            var item = e.gameObject.AddComponent<Kairos.Pruebas.InspectablePropItem>();
            item.evidenceType = e.evidenceId == "e_radio" ? Kairos.Pruebas.EvidenceType.RadioVHFSatelital :
                               e.evidenceId == "e_plotter" ? Kairos.Pruebas.EvidenceType.PlotterGPSNautico :
                               e.evidenceId.Contains("pasaporte") ? Kairos.Pruebas.EvidenceType.PasaportesRetenidos :
                               Kairos.Pruebas.EvidenceType.BitacoraReal;
            var def = CaseData.Evidence(e.evidenceId);
            item.evidenceTitle = def != null ? def.title : e.evidenceId;
            item.oitViolationCode = "OIT C188 / Forense Marítimo";
            item.targetRenderers.AddRange(e.GetComponentsInChildren<Renderer>());
        }
    }
}
