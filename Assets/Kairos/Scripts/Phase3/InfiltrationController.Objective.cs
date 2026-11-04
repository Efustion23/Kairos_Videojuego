using UnityEngine;
using UnityEngine.AI;

namespace Kairos
{
    public partial class InfiltrationController
    {
        EvidenceProp Prop(string id) => System.Array.Find(evidenceProps, e => e.evidenceId == id);
        bool Pending(string id) { var e = Prop(id); return e != null && e.gameObject.activeSelf && !Game.HasEvidence(id); }

        /// El camino mínimo para llegar al tribunal con la cadena completa y, al menos, una Orden de Retención.
        void UpdateObjective()
        {
            objTarget = exitZone;
            objective = "Vuelve a popa y baja a la lancha con quienes te siguen";
            if (Pending("e_facturas"))
            {
                if (DoorFirst("maquinas", "Abre la reja de la sala de máquinas, en el pasillo inferior")) return;
                objective = "Fotografía las facturas en la sala de máquinas"; objTarget = Prop("e_facturas").transform; return;
            }
            if (DoorFirst("babor", "Abre la escotilla de babor, en el pasillo inferior")) return;
            if (Pending("e_celda")) { objective = "Fotografía el compartimento oculto"; objTarget = Prop("e_celda").transform; return; }
            foreach (var n in people)
                if (n.personId != "rodel" && !n.State.rescued && !n.State.following) { objective = $"Habla con {n.Def.name} y ofrécele salir contigo"; objTarget = n.transform; return; }
            if (Pending("e_manifiesto"))
            {
                if (DoorFirst("oficial", "Abre la puerta del camarote del oficial, en la habilitación")) return;
                objective = "Fotografía el manifiesto en el camarote del oficial"; objTarget = Prop("e_manifiesto").transform; return;
            }
            var rodel = System.Array.Find(people, n => n.personId == "rodel");
            if (rodel != null && !rodel.State.rescued && !rodel.State.following) { objective = "Habla con Rodel Santos, en el comedor"; objTarget = rodel.transform; }
        }

        /// Lo que hay tras una puerta cerrada no tiene ruta: primero, la puerta.
        bool DoorFirst(string id, string text)
        {
            if (C.openDoors.Contains(id)) return false;
            objective = text;
            objTarget = System.Array.Find(doors, d => d.doorId == id).transform;
            return true;
        }

        public Interactable ObjectiveInteractable => objTarget != null ? objTarget.GetComponentInParent<Interactable>() : null;

        void BuildRoute()
        {
            var go = new GameObject("Ruta");
            routeLine = go.AddComponent<LineRenderer>();
            routeLine.material = Mats.Glow(new Color(Pal.Sonar.r, Pal.Sonar.g, Pal.Sonar.b, .55f));
            routeLine.widthMultiplier = .07f;
            routeLine.alignment = LineAlignment.TransformZ;
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
            routeLine.positionCount = 0;
        }

        void UpdateRoute()
        {
            pathT -= Time.deltaTime;
            if (pathT > 0) return;
            pathT = .3f;
            routeLine.positionCount = 0;
            if (objTarget == null) return;
            if (!NavMesh.SamplePosition(player.transform.position, out var from, 2f, NavMesh.AllAreas)) return;
            // Busca el suelo en la cubierta del objetivo. El radio es menor que la altura entre cubiertas (3 m): así no salta de piso.
            var probe = objTarget.position;
            probe.y = probe.y < 5.9f ? 3.1f : probe.y < 8.9f ? 6.1f : 9.2f;
            if (objTarget.GetComponent<LockedDoor>() != null) probe.x = 0; // las puertas se abren desde el pasillo central
            if (!NavMesh.SamplePosition(probe, out var to, 2.5f, NavMesh.AllAreas)) return;
            if (!NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path)) return;
            routeLine.positionCount = path.corners.Length;
            for (int i = 0; i < path.corners.Length; i++) routeLine.SetPosition(i, path.corners[i] + Vector3.up * .12f);
        }

        float RouteLength()
        {
            float d = 0;
            for (int i = 1; i < routeLine.positionCount; i++) d += Vector3.Distance(routeLine.GetPosition(i - 1), routeLine.GetPosition(i));
            return d;
        }

        /// Coloca un elemento de la interfaz sobre un punto del mundo. Con clamp, si queda fuera se pega al borde.
        bool Project(RectTransform r, Vector3 world, bool clamp)
        {
            var sp = cam.WorldToScreenPoint(world);
            bool behind = sp.z < 0;
            float x = sp.x / UnityEngine.Screen.width, y = sp.y / UnityEngine.Screen.height;
            if (behind) { x = 1 - x; y = 1 - y; }
            bool offscreen = behind || x < 0 || x > 1 || y < 0 || y > 1;
            if (offscreen && !clamp) return false;
            if (behind) y = 0;
            x = Mathf.Clamp(x, .05f, .95f); y = Mathf.Clamp(y, .27f, .82f); // sin tapar paneles ni avisos
            r.anchoredPosition = new Vector2(x * canvasRect.rect.width, y * canvasRect.rect.height);
            return true;
        }
    }
}
