// OmniNative.cs -- Enlace P/Invoke con OmniBridge.dll (puente C++ hacia la
// API HD de OpenHaptics del Geomagic Touch).
//
// Actividad 4 - Ambientes virtuales simples.
// Toda la haptica se calcula en el servo loop a 1 kHz dentro del DLL nativo;
// Unity solo LEE el estado y ESCRIBE parametros. Nunca al reves: si las
// fuerzas se calcularan en Update() a 60 fps se sentirian escalones y
// vibraciones.
//
// IMPORTANTE: el orden y el tipo de cada campo de OmniState y OmniParams
// tienen que coincidir exactamente con OmniBridge.h. HapticDevice lo comprueba
// al arrancar contra Omni_SizeOfState/Omni_SizeOfParams, para que un cambio en
// el C++ que se olvide aqui salga como un error claro y no como fuerzas raras.
using System;
using System.Runtime.InteropServices;

namespace HapticOmni
{
    /// <summary>Estado del dispositivo leido por el servo loop.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct OmniState
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]  public double[] position;      // mm
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]  public double[] velocity;      // mm/s
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public double[] transform;     // 4x4 column-major
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]  public double[] force;         // N
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]  public double[] jointAngles;   // rad
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]  public double[] gimbalAngles;  // rad
        public double servoRateHz;
        public double timeSec;
        public double autoGain;      // 1.0 normal; baja sola si el lazo oscila
        public double contactForce;  // magnitud de la fuerza de contacto, N
        public double railS;         // avance sobre el riel, mm
        public double railForce;     // resistencia del riel = fuerza del usuario, N
        public int    touchedMask;   // bit i = la primitiva i esta en contacto
        public int    railSliding;   // 1 = deslizando; 0 = adherido
        public int    buttons;       // bit0 = boton 1, bit1 = boton 2
        public int    inkwell;       // 1 = stylus en el soporte
        public int    servoTicks;
        public int    errorCode;
        public int    running;
        public int    forceEnabled;
        public int    unstable;      // 1 = el lazo esta en ciclo limite
    }

    /// <summary>Parametros de los efectos hapticos que Unity envia al servo loop.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct OmniParams
    {
        public double forceScale;                                                        // ganancia global 0..1
        public double maxForce;                                                          // saturacion N
        public double damping;                                                           // N/(mm/s)
        public double contactDamping;                                                    // N/(mm/s)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]  public double[] anchor;     // mm
        public double anchorK;                                                           // N/mm
        public double anchorRadius;                                                      // mm
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)] public double[] sphereCenter;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]  public double[] sphereRadius;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]  public double[] sphereK;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]  public double[] planePoint;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]  public double[] planeNormal;
        public double planeK;

        // --- Actividad 4: primitivas genericas en arreglos paralelos ----------
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]  public double[] shapeCenter;    // 16 x (x,y,z)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]  public double[] shapeHalf;      // 16 x semilados
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 144)] public double[] shapeAxis;      // 16 x 3 ejes
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]  public double[] shapeK;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]  public double[] shapeFriction;
        public double probeRadius;                                                       // mm

        // --- acarreo del objeto agarrado (ambiente 1) -------------------------
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]  public double[] constForce; // N
        public double carryDamping;                                                      // N/(mm/s)

        // --- riel con resistencia programable (ambiente 3) --------------------
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]  public double[] railOrigin; // mm
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]  public double[] railAxis;   // unitario
        public double railLength;                                                        // mm
        public double railK;                                                             // N/mm
        public double railB;                                                             // N/(mm/s)
        public double railFs;                                                            // N, friccion estatica
        public double railFk;                                                            // N, friccion dinamica
        public double railStickK;                                                        // N/mm
        public double railLateralK;                                                      // N/mm
        public double railEndK;                                                          // N/mm

        public int mode;                                                                 // bitmask OmniMode
        public int userTag;                                                              // marca de ensayo para el CSV
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]  public int[] sphereActive;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public int[] shapeType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public int[] shapeId;

        /// <summary>Crea una estructura con todos los arreglos ya reservados.</summary>
        public static OmniParams Create()
        {
            return new OmniParams
            {
                forceScale   = 1.0,
                maxForce     = 3.0,
                damping        = 0.0025,
                contactDamping = 0.0015,
                anchor       = new double[3],
                anchorK      = 0.05,
                anchorRadius = 0.0,
                sphereCenter = new double[24],
                sphereRadius = new double[8],
                sphereK      = new double[8],
                planePoint   = new double[3],
                planeNormal  = new double[3] { 0, 1, 0 },
                planeK       = 0.15,

                shapeCenter   = new double[OmniNative.MaxShapes * 3],
                shapeHalf     = new double[OmniNative.MaxShapes * 3],
                shapeAxis     = new double[OmniNative.MaxShapes * 9],
                shapeK        = new double[OmniNative.MaxShapes],
                shapeFriction = new double[OmniNative.MaxShapes],
                probeRadius   = 0.0,

                constForce   = new double[3],
                carryDamping = 0.0,

                railOrigin   = new double[3],
                railAxis     = new double[3] { 1, 0, 0 },
                railLength   = 60.0,
                railStickK   = 0.12,
                railLateralK = 0.18,
                railEndK     = 0.20,

                mode         = OmniMode.Free,
                userTag      = 0,
                sphereActive = new int[8],
                shapeType    = new int[OmniNative.MaxShapes],
                shapeId      = new int[OmniNative.MaxShapes],
            };
        }
    }

    /// <summary>Modos de fuerza; se pueden combinar con OR.</summary>
    public static class OmniMode
    {
        public const int Free    = 0;   // movimiento libre
        public const int Viscous = 1;   // resistencia virtual
        public const int Attract = 2;   // atraccion hacia un punto
        public const int Spheres = 4;   // esferas heredadas de la Actividad 3
        public const int Plane   = 8;   // colision con un plano
        public const int Shapes  = 16;  // lista generica de primitivas
        public const int Carry   = 32;  // peso e inercia del objeto agarrado
        public const int Rail    = 64;  // riel con resistencia programable
    }

    /// <summary>Tipos de primitiva haptica (coinciden con OMNI_SHAPE_* del C++).</summary>
    public static class OmniShapeType
    {
        public const int None      = 0;
        public const int Sphere    = 1;
        public const int Box       = 2;
        public const int HalfSpace = 3;
    }

    /// <summary>Funciones exportadas por OmniBridge.dll.</summary>
    public static class OmniNative
    {
        public const int MaxSpheres = 8;
        public const int MaxShapes  = 16;
        private const string DLL = "OmniBridge";

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Omni_Init();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Omni_Shutdown();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Omni_IsRunning();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Omni_GetLastError();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Omni_GetDeviceModel();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Omni_GetState(out OmniState state);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Omni_SetParams(ref OmniParams p);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Omni_GetParams(out OmniParams p);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Omni_SetForceEnabled(int enabled);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Omni_StartRecording(int maxSamples);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Omni_StopRecording();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Omni_GetRecordedCount();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Omni_SaveRecordingCsv([MarshalAs(UnmanagedType.LPStr)] string path);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Omni_SizeOfState();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Omni_SizeOfParams();

        public static string LastError  { get { return Marshal.PtrToStringAnsi(Omni_GetLastError())  ?? ""; } }
        public static string DeviceInfo { get { return Marshal.PtrToStringAnsi(Omni_GetDeviceModel()) ?? ""; } }
    }
}
