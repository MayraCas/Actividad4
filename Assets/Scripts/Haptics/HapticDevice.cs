// HapticDevice.cs -- Gestor del Geomagic Touch dentro de Unity.
//
// Responsabilidades:
//   1. Abrir / cerrar el dispositivo (Omni_Init / Omni_Shutdown).
//   2. Leer el estado cada frame y convertir mm del dispositivo a unidades
//      de Unity (y al reves).
//   3. Reunir los efectos hapticos activos de la escena y enviarlos al
//      servo loop de 1 kHz una vez por frame.
//
// Novedad de la Actividad 4: la escena puede tener muchisimas mas primitivas
// tocables (un laberinto son ~60 muros) que las 16 ranuras del dispositivo.
// Cada frame se ordenan por distancia a la punta y se envian las 16 mas
// cercanas. Como la mano se mueve despacio comparada con los 60 fps, la
// primitiva que estas a punto de tocar siempre entra en la lista.
//
// Si no hay hardware disponible entra en modo simulacion (raton + rueda), y
// ademas resuelve las colisiones geometricamente para que los tres ambientes
// se puedan recorrer y evaluar sin el equipo conectado.
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HapticOmni
{
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("Haptics/Haptic Device")]
    public class HapticDevice : MonoBehaviour
    {
        public static HapticDevice Instance { get; private set; }

        [Header("Mapeo dispositivo -> Unity")]
        [Tooltip("Unidades de Unity por milimetro del dispositivo.")]
        public float unitsPerMillimeter = 0.025f;
        [Tooltip("Punto del espacio de trabajo (mm) que queda en el origen de este objeto. " +
                 "El Touch declara su espacio util como (-80,-60,-35)..(80,60,35) mm, " +
                 "es decir centrado en el origen.")]
        public Vector3 deviceCenterMm = Vector3.zero;

        [Header("Seguridad")]
        [Range(0f, 1f)] public float forceScale = 1f;
        [Range(0.5f, 3.3f)] public float maxForceNewtons = 3.0f;
        [Tooltip("Desactiva toda salida de fuerza sin cerrar el dispositivo.")]
        public bool forcesEnabled = true;

        [Header("Punta")]
        [Tooltip("Radio de la punta en unidades de Unity. Con 0 la punta es un punto; " +
                 "con un radio se comporta como una bola, que es lo que hace jugable " +
                 "el laberinto del ambiente 2.")]
        public float probeRadius = 0f;

        [Header("Efectos globales")]
        public bool viscousEnabled = false;
        [Tooltip("Resistencia viscosa en N por cada mm/s de velocidad. Por encima de " +
                 "0.005 el lazo zumba; el puente lo limita ahi de todos modos.")]
        [Range(0f, 0.005f)] public float damping = 0.0025f;
        [Tooltip("Amortiguamiento del contacto: quita el rebote al chocar y permite " +
                 "usar superficies mas rigidas sin que vibren.")]
        [Range(0f, 0.005f)] public float contactDamping = 0.0015f;
        public bool shapesEnabled = true;
        public bool attractorsEnabled = true;
        public bool floorEnabled = false;
        [Tooltip("Rigidez del suelo virtual en N/mm.")]
        public float floorStiffness = 0.15f;
        [Tooltip("Altura del suelo virtual, en unidades de Unity y relativa a este objeto.")]
        public float floorHeight = -1.5f;

        [Header("Simulacion sin hardware")]
        [Tooltip("Sin dispositivo, empuja el cursor fuera de las formas solidas para " +
                 "que los ambientes sigan siendo jugables con el raton.")]
        public bool colisionSimulada = true;

        // ---- acarreo (ambiente 1) -------------------------------------------
        /// <summary>Fuerza constante en el mundo (el peso del objeto agarrado), en N.</summary>
        public Vector3 CarryForceWorld { get; set; }
        /// <summary>Amortiguamiento extra que simula la inercia del objeto, N/(mm/s).</summary>
        public float CarryDamping { get; set; }
        public bool CarryEnabled { get; set; }

        // ---- riel (ambiente 3) ----------------------------------------------
        public bool    RailEnabled { get; set; }
        public Vector3 RailOriginWorld { get; set; }
        public Vector3 RailAxisWorld { get; set; } = Vector3.right;
        /// <summary>Recorrido util del riel, en unidades de Unity.</summary>
        public float RailLength { get; set; } = 1.5f;
        public float RailK { get; set; }
        public float RailB { get; set; }
        public float RailFs { get; set; }
        public float RailFk { get; set; }
        public float RailStickK { get; set; } = 0.12f;
        public float RailLateralK { get; set; } = 0.18f;
        public float RailEndK { get; set; } = 0.20f;

        /// <summary>Avance del riel en unidades de Unity.</summary>
        public float RailS { get { return (float)State.railS * unitsPerMillimeter; } }
        /// <summary>Resistencia del riel: la fuerza que el usuario esta aplicando, N.</summary>
        public float RailForceN { get { return (float)State.railForce; } }
        public bool  RailSliding { get { return State.railSliding != 0; } }

        /// <summary>Marca de ensayo que se guarda en cada muestra del CSV de 1 kHz.</summary>
        public int UserTag { get; set; }

        // ---- estado publico -------------------------------------------------
        /// <summary>Ganancia que el puente aplica solo: baja si detecta vibracion.</summary>
        public double AutoGain   { get { return State.autoGain; } }
        /// <summary>True mientras el lazo esta oscilando por un contacto demasiado rigido.</summary>
        public bool   Unstable   { get { return State.unstable != 0; } }
        /// <summary>Magnitud de la fuerza de contacto con las primitivas, N.</summary>
        public float  ContactForceN { get { return (float)State.contactForce; } }

        public bool   Connected  { get; private set; }
        public bool   Simulated  { get; private set; }
        public string DeviceInfo { get; private set; } = "sin inicializar";
        public string LastError  { get; private set; } = "";
        public OmniState State   { get; private set; }

        /// <summary>Posicion de la punta del stylus en mm (marco del dispositivo).</summary>
        public Vector3 PositionMm { get; private set; }
        /// <summary>Velocidad de la punta en mm/s.</summary>
        public Vector3 VelocityMm { get; private set; }
        /// <summary>Fuerza que el servo loop esta aplicando, en newtons.</summary>
        public Vector3 ForceN { get; private set; }
        /// <summary>Posicion de la punta en coordenadas de mundo de Unity.</summary>
        public Vector3 WorldPosition { get; private set; }
        /// <summary>Orientacion del stylus.</summary>
        public Quaternion WorldRotation { get; private set; } = Quaternion.identity;

        public bool Button1 { get; private set; }
        public bool Button2 { get; private set; }
        public bool Button1Down { get; private set; }
        public bool Button2Down { get; private set; }
        public double ServoRateHz { get { return State.servoRateHz; } }

        // ---- registro de efectos de la escena -------------------------------
        private static readonly List<HapticShape>     shapes     = new List<HapticShape>();
        private static readonly List<HapticAttractor> attractors = new List<HapticAttractor>();

        internal static void Register(HapticShape s)       { if (!shapes.Contains(s)) shapes.Add(s); }
        internal static void Unregister(HapticShape s)     { shapes.Remove(s); }
        internal static void Register(HapticAttractor a)   { if (!attractors.Contains(a)) attractors.Add(a); }
        internal static void Unregister(HapticAttractor a) { attractors.Remove(a); }

        /// <summary>Primitivas realmente enviadas al dispositivo este frame.</summary>
        public int ActiveShapeCount { get; private set; }
        /// <summary>Total de primitivas registradas en la escena.</summary>
        public int TotalShapeCount { get { return shapes.Count; } }
        public HapticAttractor ActiveAttractor { get; private set; }

        /// <summary>Formas que el servo loop reporta en contacto este frame.</summary>
        public IReadOnlyList<HapticShape> EnContacto { get { return contacto; } }

        private OmniParams parametros;
        private bool prevB1, prevB2;
        private Vector3 posSimuladaMm;
        private readonly HapticShape[] enviadas = new HapticShape[OmniNative.MaxShapes];
        private readonly List<HapticShape> contacto = new List<HapticShape>();
        private readonly List<HapticShape> candidatas = new List<HapticShape>();

        // ---------------------------------------------------------------------
        void OnEnable()
        {
            Instance   = this;
            parametros = OmniParams.Create();

            try
            {
                // Antes de nada: si el layout de C# y el de C++ no coinciden, todo
                // lo demas seria basura. Mejor un error legible aqui.
                int nS = OmniNative.Omni_SizeOfState();
                int nP = OmniNative.Omni_SizeOfParams();
                int mS = Marshal.SizeOf(typeof(OmniState));
                int mP = Marshal.SizeOf(typeof(OmniParams));
                if (nS != mS || nP != mP)
                {
                    LastError = string.Format(
                        "El layout de las estructuras no coincide con OmniBridge.dll " +
                        "(state {0} vs {1}, params {2} vs {3}). Recompila el puente con " +
                        "Native/OmniBridge/build.bat o revisa OmniNative.cs.", mS, nS, mP, nP);
                    Debug.LogError("[Haptics] " + LastError);
                    Simulated = true;
                    DeviceInfo = "LAYOUT INCOMPATIBLE";
                    return;
                }

                int rc = OmniNative.Omni_Init();
                if (rc == 0)
                {
                    Connected  = true;
                    DeviceInfo = OmniNative.DeviceInfo;
                    Debug.Log("[Haptics] Dispositivo listo: " + DeviceInfo);
                }
                else
                {
                    LastError = OmniNative.LastError;
                    Debug.LogWarning("[Haptics] No se pudo abrir el dispositivo (" + rc + "): " + LastError +
                                     "\nSe usara el modo simulacion con el raton.");
                }
            }
            catch (System.DllNotFoundException e)
            {
                LastError = "No se encontro OmniBridge.dll en Assets/Plugins/x86_64. " + e.Message;
                Debug.LogWarning("[Haptics] " + LastError);
            }
            catch (System.Exception e)
            {
                LastError = e.Message;
                Debug.LogWarning("[Haptics] " + e);
            }

            Simulated = !Connected;
            if (Simulated && DeviceInfo != "LAYOUT INCOMPATIBLE") DeviceInfo = "SIMULADO (sin hardware)";
        }

        void OnDisable()
        {
            if (Connected)
            {
                try { OmniNative.Omni_Shutdown(); }
                catch (System.Exception e) { Debug.LogWarning("[Haptics] " + e.Message); }
                Debug.Log("[Haptics] Dispositivo liberado.");
            }
            Connected = false;
            if (Instance == this) Instance = null;
        }

        void OnApplicationQuit() { OnDisable(); }

        // ---------------------------------------------------------------------
        void Update()
        {
            if (Connected) LeerDispositivo();
            else           SimularConRaton();

            WorldPosition = DeviceToWorld(PositionMm);
            if (!Connected && colisionSimulada) ResolverColisionSimulada();

            ActualizarContactos();

            Button1Down = Button1 && !prevB1;
            Button2Down = Button2 && !prevB2;
            prevB1 = Button1;
            prevB2 = Button2;
        }

        private void LeerDispositivo()
        {
            OmniState s;
            OmniNative.Omni_GetState(out s);
            State = s;

            if (s.position != null && s.position.Length == 3)
                PositionMm = new Vector3((float)s.position[0], (float)s.position[1], (float)s.position[2]);
            if (s.velocity != null && s.velocity.Length == 3)
                VelocityMm = new Vector3((float)s.velocity[0], (float)s.velocity[1], (float)s.velocity[2]);
            if (s.force != null && s.force.Length == 3)
                ForceN = new Vector3((float)s.force[0], (float)s.force[1], (float)s.force[2]);

            // La matriz 4x4 del dispositivo es column-major; sus columnas 0..2
            // son los ejes del stylus.
            if (s.transform != null && s.transform.Length == 16)
            {
                Vector3 right   = new Vector3((float)s.transform[0], (float)s.transform[1], -(float)s.transform[2]);
                Vector3 up      = new Vector3((float)s.transform[4], (float)s.transform[5], -(float)s.transform[6]);
                if (right.sqrMagnitude > 1e-6f && up.sqrMagnitude > 1e-6f)
                    WorldRotation = transform.rotation * Quaternion.LookRotation(Vector3.Cross(right, up), up);
            }

            Button1 = (s.buttons & 1) != 0;
            Button2 = (s.buttons & 2) != 0;
        }

        /// <summary>Modo sin hardware: el raton mueve XY y la rueda mueve Z.</summary>
        private void SimularConRaton()
        {
            var mouse = Mouse.current;
            if (mouse != null && Camera.main != null)
            {
                Vector2 m = mouse.position.ReadValue();
                float nx = Mathf.Clamp01(m.x / Mathf.Max(1, Screen.width))  * 2f - 1f;
                float ny = Mathf.Clamp01(m.y / Mathf.Max(1, Screen.height)) * 2f - 1f;
                posSimuladaMm.x = nx * 80f;
                posSimuladaMm.y = ny * 60f;
                posSimuladaMm.z = Mathf.Clamp(posSimuladaMm.z + mouse.scroll.ReadValue().y * 0.05f, -90f, 40f);
                Button1 = mouse.leftButton.isPressed;
                Button2 = mouse.rightButton.isPressed;
            }
            Vector3 anterior = PositionMm;
            PositionMm = posSimuladaMm;
            VelocityMm = Time.deltaTime > 0f ? (PositionMm - anterior) / Time.deltaTime : Vector3.zero;
            ForceN     = Vector3.zero;
        }

        /// <summary>Sin dispositivo no hay fuerzas, asi que la punta atravesaria los
        /// muros. Se empuja geometricamente fuera de las formas para que el
        /// laberinto y el banco de trabajo sigan teniendo sentido con el raton.</summary>
        private void ResolverColisionSimulada()
        {
            for (int pasada = 0; pasada < 3; ++pasada)
            {
                bool movido = false;
                for (int i = 0; i < shapes.Count; ++i)
                {
                    var sh = shapes[i];
                    if (sh == null || !sh.isActiveAndEnabled) continue;
                    float d = sh.DistanciaA(WorldPosition) - probeRadius;
                    if (d >= 0f) continue;

                    // Normal numerica por diferencias finitas: sirve igual para las
                    // tres formas sin repetir su geometria aqui.
                    const float h = 0.002f;
                    Vector3 p = WorldPosition;
                    Vector3 n = new Vector3(
                        sh.DistanciaA(p + Vector3.right   * h) - sh.DistanciaA(p - Vector3.right   * h),
                        sh.DistanciaA(p + Vector3.up      * h) - sh.DistanciaA(p - Vector3.up      * h),
                        sh.DistanciaA(p + Vector3.forward * h) - sh.DistanciaA(p - Vector3.forward * h));
                    if (n.sqrMagnitude < 1e-12f) continue;
                    WorldPosition = p + n.normalized * (-d);
                    movido = true;
                }
                if (!movido) break;
            }
            PositionMm = WorldToDevice(WorldPosition);
        }

        /// <summary>Marca que formas estan en contacto, segun el servo loop o, en
        /// simulacion, por geometria.</summary>
        private void ActualizarContactos()
        {
            for (int i = 0; i < contacto.Count; ++i)
                if (contacto[i] != null) contacto[i].EnContacto = false;
            contacto.Clear();

            if (Connected)
            {
                int mask = State.touchedMask;
                for (int i = 0; i < enviadas.Length; ++i)
                {
                    if ((mask & (1 << i)) == 0) continue;
                    var sh = enviadas[i];
                    if (sh == null) continue;
                    sh.EnContacto = true;
                    contacto.Add(sh);
                }
            }
            else
            {
                for (int i = 0; i < shapes.Count; ++i)
                {
                    var sh = shapes[i];
                    if (sh == null || !sh.isActiveAndEnabled) continue;
                    if (sh.DistanciaA(WorldPosition) - probeRadius > 0.004f) continue;
                    sh.EnContacto = true;
                    contacto.Add(sh);
                }
            }
        }

        // ---------------------------------------------------------------------
        void LateUpdate()
        {
            SeleccionarFormas();          // tambien en simulacion: alimenta ActiveShapeCount
            if (!Connected) return;
            ArmarParametros();
            OmniNative.Omni_SetParams(ref parametros);
            OmniNative.Omni_SetForceEnabled(forcesEnabled ? 1 : 0);
        }

        /// <summary>Elige las 16 primitivas mas cercanas a la punta. Las marcadas como
        /// prioritarias (suelo, mesa) van siempre primero.</summary>
        private void SeleccionarFormas()
        {
            candidatas.Clear();
            if (shapesEnabled)
            {
                for (int i = 0; i < shapes.Count; ++i)
                {
                    var s = shapes[i];
                    if (s != null && s.isActiveAndEnabled) candidatas.Add(s);
                }
                Vector3 punta = WorldPosition;
                candidatas.Sort((a, b) =>
                {
                    if (a.prioritaria != b.prioritaria) return a.prioritaria ? -1 : 1;
                    return a.DistanciaA(punta).CompareTo(b.DistanciaA(punta));
                });
            }
            ActiveShapeCount = Mathf.Min(candidatas.Count, OmniNative.MaxShapes);
        }

        private void ArmarParametros()
        {
            int modo = OmniMode.Free;

            parametros.forceScale  = forceScale;
            parametros.maxForce    = maxForceNewtons;
            parametros.probeRadius = probeRadius / Mathf.Max(1e-5f, unitsPerMillimeter);
            parametros.userTag     = UserTag;

            // Resistencia virtual
            parametros.contactDamping = contactDamping;
            if (viscousEnabled) { modo |= OmniMode.Viscous; parametros.damping = damping; }

            // Atraccion: se usa el atractor habilitado mas cercano a la punta. En el
            // ambiente 1 es el resorte que une la punta con el objeto agarrado.
            ActiveAttractor = null;
            if (attractorsEnabled)
            {
                float mejor = float.MaxValue;
                for (int i = 0; i < attractors.Count; i++)
                {
                    var a = attractors[i];
                    if (a == null || !a.isActiveAndEnabled) continue;
                    float d = Vector3.Distance(a.transform.position, WorldPosition);
                    if (d < mejor) { mejor = d; ActiveAttractor = a; }
                }
                if (ActiveAttractor != null)
                {
                    Vector3 mm = WorldToDevice(ActiveAttractor.transform.position);
                    parametros.anchor[0] = mm.x;
                    parametros.anchor[1] = mm.y;
                    parametros.anchor[2] = mm.z;
                    parametros.anchorK      = ActiveAttractor.stiffness;
                    parametros.anchorRadius = ActiveAttractor.radius / Mathf.Max(1e-5f, unitsPerMillimeter);
                    modo |= OmniMode.Attract;
                }
            }

            // Primitivas solidas
            int n = 0;
            for (int i = 0; i < OmniNative.MaxShapes; i++)
            {
                enviadas[i] = null;
                parametros.shapeType[i] = OmniShapeType.None;
                parametros.shapeId[i]   = 0;
            }
            for (int i = 0; i < candidatas.Count && n < OmniNative.MaxShapes; i++)
                EscribirForma(candidatas[i], n++);
            if (n > 0) modo |= OmniMode.Shapes;
            ActiveShapeCount = n;

            // Suelo virtual (plano infinito heredado de la Actividad 3)
            if (floorEnabled)
            {
                Vector3 mm = WorldToDevice(transform.position + Vector3.up * floorHeight);
                parametros.planePoint[0]  = mm.x;
                parametros.planePoint[1]  = mm.y;
                parametros.planePoint[2]  = mm.z;
                parametros.planeNormal[0] = 0; parametros.planeNormal[1] = 1; parametros.planeNormal[2] = 0;
                parametros.planeK = floorStiffness;
                modo |= OmniMode.Plane;
            }

            // Acarreo: peso e inercia del objeto que se lleva en la mano.
            if (CarryEnabled)
            {
                Vector3 f = WorldToDeviceDirection(CarryForceWorld);
                parametros.constForce[0] = f.x;
                parametros.constForce[1] = f.y;
                parametros.constForce[2] = f.z;
                parametros.carryDamping  = CarryDamping;
                modo |= OmniMode.Carry;
            }

            // Riel con resistencia programable.
            if (RailEnabled)
            {
                float upm = Mathf.Max(1e-5f, unitsPerMillimeter);
                Vector3 o = WorldToDevice(RailOriginWorld);
                Vector3 a = WorldToDeviceDirection(RailAxisWorld.normalized);
                parametros.railOrigin[0] = o.x; parametros.railOrigin[1] = o.y; parametros.railOrigin[2] = o.z;
                parametros.railAxis[0]   = a.x; parametros.railAxis[1]   = a.y; parametros.railAxis[2]   = a.z;
                parametros.railLength    = RailLength / upm;
                parametros.railK         = RailK;
                parametros.railB         = RailB;
                parametros.railFs        = RailFs;
                parametros.railFk        = RailFk;
                parametros.railStickK    = RailStickK;
                parametros.railLateralK  = RailLateralK;
                parametros.railEndK      = RailEndK;
                modo |= OmniMode.Rail;
            }

            parametros.mode = modo;
        }

        /// <summary>Vuelca una forma de la escena en la ranura n de los parametros.</summary>
        private void EscribirForma(HapticShape s, int n)
        {
            float upm = Mathf.Max(1e-5f, unitsPerMillimeter);
            Vector3 c = WorldToDevice(s.transform.position);
            parametros.shapeCenter[n * 3 + 0] = c.x;
            parametros.shapeCenter[n * 3 + 1] = c.y;
            parametros.shapeCenter[n * 3 + 2] = c.z;

            switch (s.tipo)
            {
                case TipoForma.Esfera:
                    parametros.shapeHalf[n * 3 + 0] = s.RadioMundo / upm;
                    parametros.shapeHalf[n * 3 + 1] = 0;
                    parametros.shapeHalf[n * 3 + 2] = 0;
                    EscribirEjes(n, Vector3.right, Vector3.up, Vector3.forward);
                    break;

                case TipoForma.Caja:
                {
                    Vector3 h = s.SemiladosMundo / upm;
                    parametros.shapeHalf[n * 3 + 0] = h.x;
                    parametros.shapeHalf[n * 3 + 1] = h.y;
                    parametros.shapeHalf[n * 3 + 2] = h.z;
                    EscribirEjes(n, s.transform.right, s.transform.up, s.transform.forward);
                    break;
                }

                default:
                {
                    Vector3 nrm = s.transform.TransformDirection(s.normalLocal).normalized;
                    EscribirEjes(n, nrm, Vector3.up, Vector3.forward);
                    parametros.shapeHalf[n * 3 + 0] = 0;
                    parametros.shapeHalf[n * 3 + 1] = 0;
                    parametros.shapeHalf[n * 3 + 2] = 0;
                    break;
                }
            }

            parametros.shapeK[n]        = s.rigidez;
            parametros.shapeFriction[n] = s.friccion;
            parametros.shapeType[n]     = (int)s.tipo;
            parametros.shapeId[n]       = s.Id;
            enviadas[n] = s;
        }

        private void EscribirEjes(int n, Vector3 ejeX, Vector3 ejeY, Vector3 ejeZ)
        {
            Vector3 x = WorldToDeviceDirection(ejeX);
            Vector3 y = WorldToDeviceDirection(ejeY);
            Vector3 z = WorldToDeviceDirection(ejeZ);
            int b = n * 9;
            parametros.shapeAxis[b + 0] = x.x; parametros.shapeAxis[b + 1] = x.y; parametros.shapeAxis[b + 2] = x.z;
            parametros.shapeAxis[b + 3] = y.x; parametros.shapeAxis[b + 4] = y.y; parametros.shapeAxis[b + 5] = y.z;
            parametros.shapeAxis[b + 6] = z.x; parametros.shapeAxis[b + 7] = z.y; parametros.shapeAxis[b + 8] = z.z;
        }

        // ---- conversiones ----------------------------------------------------
        /// <summary>Convierte milimetros del dispositivo a un punto del mundo.</summary>
        public Vector3 DeviceToWorld(Vector3 mm)
        {
            Vector3 local = new Vector3(mm.x - deviceCenterMm.x,
                                        mm.y - deviceCenterMm.y,
                                      -(mm.z - deviceCenterMm.z)) * unitsPerMillimeter;
            return transform.TransformPoint(local);
        }

        /// <summary>Convierte un punto del mundo a milimetros del dispositivo.</summary>
        public Vector3 WorldToDevice(Vector3 world)
        {
            Vector3 local = transform.InverseTransformPoint(world) / Mathf.Max(1e-5f, unitsPerMillimeter);
            return new Vector3(local.x + deviceCenterMm.x,
                               local.y + deviceCenterMm.y,
                              -local.z + deviceCenterMm.z);
        }

        /// <summary>Lleva una direccion del mundo al marco del dispositivo. Es la misma
        /// transformacion que para los puntos pero sin el desplazamiento; la Z va
        /// invertida porque el marco del Touch es diestro y el de Unity zurdo.</summary>
        public Vector3 WorldToDeviceDirection(Vector3 world)
        {
            Vector3 local = transform.InverseTransformDirection(world);
            return new Vector3(local.x, local.y, -local.z);
        }

        /// <summary>Lleva una direccion del dispositivo al mundo.</summary>
        public Vector3 DeviceToWorldDirection(Vector3 dev)
        {
            return transform.TransformDirection(new Vector3(dev.x, dev.y, -dev.z));
        }

        // ---- grabacion para el analisis en Python ---------------------------
        public bool Recording { get; private set; }
        public string UltimaGrabacion { get; private set; }

        public void StartRecording(int segundos = 120)
        {
            if (!Connected) { Debug.LogWarning("[Haptics] Sin dispositivo: no hay nada que grabar."); return; }
            OmniNative.Omni_StartRecording(Mathf.Max(1, segundos) * 1000);
            Recording = true;
            Debug.Log("[Haptics] Grabando a 1 kHz...");
        }

        public string StopRecording(string nombre = "omni_log.csv")
        {
            if (!Connected || !Recording) return null;
            string ruta = System.IO.Path.Combine(Application.dataPath, "../Analysis/datos/" + nombre);
            ruta = System.IO.Path.GetFullPath(ruta);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ruta));
            int n = OmniNative.Omni_SaveRecordingCsv(ruta);
            Recording = false;
            UltimaGrabacion = ruta;
            Debug.Log("[Haptics] " + n + " muestras guardadas en " + ruta);
            return ruta;
        }

        // ---- ayuda visual en el editor --------------------------------------
        void OnDrawGizmos()
        {
            // Caja aproximada del espacio de trabajo util del Touch (160x120x70 mm)
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.35f);
            Vector3 c = DeviceToWorld(deviceCenterMm);
            Vector3 size = new Vector3(160f, 120f, 70f) * unitsPerMillimeter;
            Gizmos.matrix = Matrix4x4.TRS(c, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.matrix = Matrix4x4.identity;

            if (floorEnabled)
            {
                Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.5f);
                Vector3 f = transform.position + Vector3.up * floorHeight;
                Gizmos.DrawWireCube(f, new Vector3(size.x, 0.01f, size.z));
            }
        }
    }
}
