using UnityEditor;
using UnityEngine;

namespace Kairos.EditorTools
{
    public static partial class KairosBuilder
    {
        /// Cada prueba se reconoce por lo que es: una carpeta, unos pasaportes, un candado.
        /// Se aplica el shader de outline glow forense y metadatos de inspección OIT.
        static void DressEvidence(EvidenceProp e, Vector3 scale)
        {
            var holder = new GameObject("Modelo").transform;
            holder.SetParent(e.transform, false);
            holder.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
            void HideBox() => e.GetComponent<MeshRenderer>().enabled = false;

            var glowShader = Shader.Find("Kairos/Pruebas/InspectableOutlineGlow");
            Material glowMat = null;
            if (glowShader != null)
            {
                glowMat = new Material(glowShader);
                glowMat.SetColor("_GlowColor", new Color(0.18f, 0.96f, 1.0f, 1.0f));
                glowMat.SetFloat("_HighlightStrength", 1.8f);
                glowMat.SetFloat("_ActiveGlow", 1.0f);
            }

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
                    var cover = glowMat != null ? glowMat : Lit("M_Passport", new Color(.2f, .05f, .07f), .3f, 0);
                    if (glowMat != null) glowMat.SetColor("_BaseColor", new Color(.2f, .05f, .07f));
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
                    if (glowMat != null)
                    {
                        var mr = e.GetComponent<MeshRenderer>();
                        if (mr != null && mr.sharedMaterial != null)
                        {
                            glowMat.SetColor("_BaseColor", mr.sharedMaterial.color);
                            mr.sharedMaterial = glowMat;
                        }
                    }
                    break;
            }

            var item = e.gameObject.AddComponent<Kairos.Pruebas.InspectablePropItem>();
            item.evidenceType = e.evidenceId.Contains("pasaporte") ? Kairos.Pruebas.EvidenceType.PasaportesRetenidos :
                               e.evidenceId.Contains("bitacora") || e.evidenceId.Contains("registro") ? Kairos.Pruebas.EvidenceType.BitacoraReal :
                               Kairos.Pruebas.EvidenceType.BitacoraReal;
            item.evidenceTitle = e.evidenceId;
            item.oitViolationCode = "OIT C188 / Forense Marítimo";
            item.targetRenderers.AddRange(e.GetComponentsInChildren<Renderer>());
        }
    }
}
