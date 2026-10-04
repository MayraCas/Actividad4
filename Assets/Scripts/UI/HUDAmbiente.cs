// HUDAmbiente.cs -- Interfaz en pantalla, comun a los tres ambientes.
//
// Panel izquierdo: el estado del dispositivo -- coordenadas de la punta,
// fuerza que esta devolviendo, frecuencia real del servo loop y avisos de
// seguridad. Panel derecho: las metricas que pide la actividad para el
// ambiente que este cargado, que cada Ambiente devuelve en LineasHUD().
//
// Se dibuja con IMGUI para que las escenas no dependan de prefabs, fuentes ni
// canvas: basta anadir el componente y funciona.
using UnityEngine;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/HUD de ambiente")]
    public class HUDAmbiente : MonoBehaviour
    {
        public HapticDevice dispositivo;
        public AmbienteBase ambiente;

        [Header("Apariencia")]
        public int anchoPanel = 330;
        [Tooltip("Semi-rango de cada eje que abarcan las barras, en mm, medido " +
                 "alrededor del centro del espacio de trabajo del dispositivo.")]
        public Vector3 rangoMm = new Vector3(80f, 60f, 45f);

        private GUIStyle titulo, texto, mono, chico, grande;
        private Texture2D texFondo, texBarra, texMarco;

        void Reset()
        {
            dispositivo = FindFirstObjectByType<HapticDevice>();
            ambiente    = FindFirstObjectByType<AmbienteBase>();
        }

        void Awake()
        {
            if (dispositivo == null) dispositivo = FindFirstObjectByType<HapticDevice>();
            if (ambiente    == null) ambiente    = FindFirstObjectByType<AmbienteBase>();
            texFondo = Solido(new Color(0.04f, 0.06f, 0.11f, 0.88f));
            texBarra = Solido(Color.white);
            texMarco = Solido(new Color(1f, 1f, 1f, 0.16f));
        }

        void Update() { SelectorAmbientes.LeerAtajosDeEscena(); }

        private static Texture2D Solido(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            t.hideFlags = HideFlags.HideAndDontSave;
            return t;
        }

        private void PrepararEstilos()
        {
            if (titulo != null) return;
            titulo = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            titulo.normal.textColor = new Color(0.55f, 0.85f, 1f);
            texto  = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            texto.normal.textColor = new Color(0.90f, 0.93f, 0.98f);
            mono   = new GUIStyle(texto) { fontSize = 14, fontStyle = FontStyle.Bold };
            chico  = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
            chico.normal.textColor = new Color(0.65f, 0.70f, 0.80f);
            grande = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
            grande.normal.textColor = new Color(0.95f, 0.97f, 1f);
        }

        void OnGUI()
        {
            PrepararEstilos();
            if (dispositivo == null)
            {
                GUI.Label(new Rect(12, 12, 460, 24), "Falta el componente HapticDevice en la escena.", titulo);
                return;
            }
            PanelDispositivo();
            PanelAmbiente();
        }

        // ------------------------------------------------------------ izquierda --
        private void PanelDispositivo()
        {
            int x = 12, y = 12, w = anchoPanel, alto = 292;

            GUI.DrawTexture(new Rect(x, y, w, alto), texFondo);
            var r = new Rect(x + 14, y + 12, w - 28, 20);

            GUI.Label(r, ambiente != null ? ambiente.Nombre : "DISPOSITIVO", titulo); r.y += 22;

            bool ok = dispositivo.Connected;
            var estilo = new GUIStyle(texto);
            estilo.normal.textColor = ok ? new Color(0.45f, 0.95f, 0.55f) : new Color(1f, 0.75f, 0.3f);
            GUI.Label(r, (ok ? "● " : "○ ") + dispositivo.DeviceInfo, estilo); r.y += 20;

            if (!ok && !string.IsNullOrEmpty(dispositivo.LastError))
            {
                GUI.Label(new Rect(r.x, r.y, r.width, 28), dispositivo.LastError, chico);
                r.y += 28;
            }

            r.y += 4;
            GUI.Label(r, "POSICION DE LA PUNTA (mm)", texto); r.y += 20;

            Vector3 p = dispositivo.PositionMm;
            Vector3 c = dispositivo.deviceCenterMm;
            Barra(ref r, "X", p.x, c.x, rangoMm.x, new Color(1.0f, 0.35f, 0.38f));
            Barra(ref r, "Y", p.y, c.y, rangoMm.y, new Color(0.45f, 0.95f, 0.45f));
            Barra(ref r, "Z", p.z, c.z, rangoMm.z, new Color(0.40f, 0.65f, 1.00f));

            r.y += 4;
            Vector3 f = dispositivo.ForceN;
            float mag = f.magnitude;
            GUI.Label(r, "FUERZA APLICADA (N)", texto); r.y += 20;
            GUI.Label(r, string.Format("Fx {0,6:F2}   Fy {1,6:F2}   Fz {2,6:F2}", f.x, f.y, f.z), mono); r.y += 20;
            BarraSimple(ref r, "|F|", mag, dispositivo.maxForceNewtons,
                        Color.Lerp(new Color(0.4f, 0.9f, 1f), new Color(1f, 0.4f, 0.3f),
                                   Mathf.Clamp01(mag / dispositivo.maxForceNewtons)));

            r.y += 4;
            GUI.Label(r, string.Format("Servo {0,5:F0} Hz · {1,3:F0} fps · formas {2}/{3} · botones {4}",
                                       dispositivo.ServoRateHz,
                                       1f / Mathf.Max(1e-5f, Time.smoothDeltaTime),
                                       dispositivo.ActiveShapeCount, dispositivo.TotalShapeCount,
                                       (dispositivo.Button1 ? "[1]" : " 1 ") + (dispositivo.Button2 ? "[2]" : " 2 ")), chico);
            r.y += 18;

            if (!dispositivo.forcesEnabled)      Aviso(r, "FUERZAS DESACTIVADAS (F para reactivar)", new Color(1f, 0.6f, 0.3f));
            else if (dispositivo.AutoGain < 0.99) Aviso(r, string.Format("CONTACTO INESTABLE - fuerza reducida al {0:F0} %",
                                                                        dispositivo.AutoGain * 100.0), new Color(1f, 0.55f, 0.35f));
            else if (dispositivo.Recording)       Aviso(r, "● GRABANDO a 1 kHz", new Color(1f, 0.45f, 0.45f));
        }

        private void Aviso(Rect r, string txt, Color c)
        {
            var s = new GUIStyle(texto);
            s.normal.textColor = c;
            GUI.Label(r, txt, s);
        }

        // ------------------------------------------------------------- derecha --
        private void PanelAmbiente()
        {
            if (ambiente == null) return;

            string[] lineas = ambiente.LineasHUD();
            string[] ayuda  = ambiente.LineasAyuda();
            int w = 320;
            int alto = 96 + lineas.Length * 19 + 12 + ayuda.Length * 16;
            int x = Screen.width - w - 12, y = 12;

            GUI.DrawTexture(new Rect(x, y, w, alto), texFondo);
            var r = new Rect(x + 14, y + 10, w - 28, 20);

            GUI.Label(r, "ENSAYO", titulo); r.y += 24;
            GUI.Label(r, ambiente.TiempoTexto, grande); r.y += 38;

            string estado = ambiente.Estado == EstadoEnsayo.Preparado ? "listo - haz la primera accion"
                          : ambiente.Estado == EstadoEnsayo.EnCurso   ? "en curso"
                                                                      : "TERMINADO - datos guardados";
            var se = new GUIStyle(texto);
            se.normal.textColor = ambiente.Estado == EstadoEnsayo.Terminado
                                ? new Color(0.5f, 0.95f, 0.6f) : new Color(0.85f, 0.88f, 0.95f);
            GUI.Label(r, estado, se); r.y += 24;

            for (int i = 0; i < lineas.Length; i++) { GUI.Label(r, lineas[i], texto); r.y += 19; }

            r.y += 8;
            for (int i = 0; i < ayuda.Length; i++) { GUI.Label(r, ayuda[i], chico); r.y += 16; }
        }

        // ---- barras -----------------------------------------------------------
        private void Barra(ref Rect r, string eje, float valor, float centro, float rango, Color color)
        {
            float t = Mathf.Clamp01((valor - centro + rango) / (2f * rango));
            GUI.Label(new Rect(r.x, r.y, 16, 18), eje, mono);
            GUI.Label(new Rect(r.x + 18, r.y, 62, 18), valor.ToString("F2"), mono);

            var pista = new Rect(r.x + 86, r.y + 5, r.width - 86, 9);
            GUI.DrawTexture(pista, texMarco);
            GUI.DrawTexture(new Rect(pista.x + pista.width * 0.5f - 1, pista.y - 2, 2, 13), texMarco);
            var col = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(pista.x + pista.width * t - 3, pista.y - 2, 6, 13), texBarra);
            GUI.color = col;
            r.y += 21;
        }

        private void BarraSimple(ref Rect r, string etiqueta, float valor, float maximo, Color color)
        {
            float t = Mathf.Clamp01(valor / Mathf.Max(1e-5f, maximo));
            GUI.Label(new Rect(r.x, r.y, 30, 18), etiqueta, mono);
            GUI.Label(new Rect(r.x + 32, r.y, 56, 18), valor.ToString("F2"), mono);
            var pista = new Rect(r.x + 92, r.y + 5, r.width - 92, 9);
            GUI.DrawTexture(pista, texMarco);
            var col = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(pista.x, pista.y, pista.width * t, 9), texBarra);
            GUI.color = col;
            r.y += 21;
        }
    }
}
