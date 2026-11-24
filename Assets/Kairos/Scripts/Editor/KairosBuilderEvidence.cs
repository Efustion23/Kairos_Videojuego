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
                    HideBox();
                    var pageMat = Lit("M_Ev_PassportPages", new Color(.84f, .81f, .74f), .18f, 0f);
                    var goldEmblem = Lit("M_Ev_PassportGold", new Color(.85f, .68f, .24f), .55f, .75f);
                    void Passport(Vector3 pPos, float yaw, string tag, Color coverCol)
                    {
                        var group = new GameObject("Pasaporte_" + tag).transform;
                        group.SetParent(holder, false);
                        group.localPosition = pPos;
                        group.localRotation = Quaternion.Euler(0, yaw, 0);
                        var cov = EvidenceGlowMat("pasaporte_" + tag, coverCol, cyanGlowCol);
                        Part(group, PrimitiveType.Cube, pageMat, new Vector3(.003f, 0, .002f), new Vector3(.126f, .012f, .186f));
                        Part(group, PrimitiveType.Cube, cov, new Vector3(0, .009f, 0), new Vector3(.134f, .005f, .194f));
                        Part(group, PrimitiveType.Cube, cov, new Vector3(0, -.009f, 0), new Vector3(.134f, .005f, .194f));
                        Part(group, PrimitiveType.Cube, cov, new Vector3(-.065f, 0, 0), new Vector3(.006f, .021f, .194f));
                        Part(group, PrimitiveType.Cube, goldEmblem, new Vector3(.006f, .013f, .018f), new Vector3(.036f, .002f, .036f));
                    }
                    Passport(new Vector3(-.018f, -.112f, -.01f), -12f, "burdeos", new Color(.28f, .06f, .09f));
                    Passport(new Vector3(.012f, -.086f, .015f), 8f, "marino", new Color(.06f, .12f, .26f));
                    Passport(new Vector3(-.004f, -.060f, -.005f), 24f, "verde", new Color(.07f, .20f, .14f));
                    Part(holder, PrimitiveType.Cube, darkMetal, new Vector3(0, -.086f, 0), new Vector3(.148f, .078f, .022f)).localRotation = Quaternion.Euler(0, 8f, 0);
                    break;
                case "e_candados":
                    HideBox();
                    var brassLock = EvidenceGlowMat("candados", new Color(.70f, .54f, .22f), cyanGlowCol);
                    Part(holder, PrimitiveType.Cube, brassLock, new Vector3(0, -.03f, 0), new Vector3(.074f, .136f, .126f));
                    Part(holder, PrimitiveType.Cube, darkMetal, new Vector3(.038f, -.04f, 0), new Vector3(.006f, .05f, .03f));
                    Part(holder, PrimitiveType.Cube, darkMetal, new Vector3(-.038f, -.04f, 0), new Vector3(.006f, .05f, .03f));
                    Part(holder, PrimitiveType.Cylinder, steel, new Vector3(0, .075f, -.038f), new Vector3(.020f, .055f, .020f));
                    Part(holder, PrimitiveType.Cylinder, steel, new Vector3(0, .075f, .038f), new Vector3(.020f, .055f, .020f));
                    Part(holder, PrimitiveType.Cube, steel, new Vector3(0, .126f, 0), new Vector3(.020f, .020f, .096f));
                    Part(holder, PrimitiveType.Cube, darkMetal, new Vector3(0, .144f, .075f), new Vector3(.022f, .032f, .064f)).localRotation = Quaternion.Euler(22f, 0, 0);
                    Part(holder, PrimitiveType.Cube, steel, new Vector3(0, .112f, .108f), new Vector3(.032f, .022f, .064f)).localRotation = Quaternion.Euler(-30f, 0, 0);
                    Part(holder, PrimitiveType.Cube, darkMetal, new Vector3(0, .076f, .138f), new Vector3(.022f, .032f, .064f)).localRotation = Quaternion.Euler(40f, 0, 0);
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
