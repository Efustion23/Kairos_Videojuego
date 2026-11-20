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

            // Modelo CC0 escalado para que su lado mayor mida 'size' metros; 'wall' lo pone de pie mirando al centro del barco.
            void Model(string id, float size, float yaw = 0, bool wall = false)
            {
                HideBox();
                var go = (GameObject)PrefabUtility.InstantiatePrefab(PropPrefab(id), holder);
                var b = new Bounds(); bool first = true;
                foreach (var r in go.GetComponentsInChildren<Renderer>()) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
                go.transform.localScale = Vector3.one * (size / Mathf.Max(b.size.x, b.size.z, .001f));
                go.transform.localRotation = wall ? Quaternion.Euler(0, 0, Mathf.Sign(e.transform.position.x) * 90) * Quaternion.Euler(0, yaw, 0) : Quaternion.Euler(0, yaw, 0);
                go.transform.localPosition = wall ? Vector3.zero : Vector3.down * scale.y * .5f; // apoyado en la mesa
            }

            switch (e.evidenceId)
            {
                case "e_radio": Model("vintage_radio_transceiver", .5f, 180); break;
                case "e_plotter": Model("classic_laptop", .36f, 180); break;
                case "e_manifiesto": Model("clipboard", .4f, 15); break;
                case "e_contratos": Model("office_notepads", .45f, -20); break;
                case "e_turnos":
                case "e_facturas": Model("clipboard", .42f, 90, true); break;
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
