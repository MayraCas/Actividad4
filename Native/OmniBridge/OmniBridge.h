// OmniBridge.h -- API C que expone el Geomagic Touch (Omni) a Unity / C#.
//
// El bucle haptico (servo loop) corre a 1 kHz dentro de hd.dll; Unity solo
// lee el estado y escribe parametros, nunca calcula fuerzas. Eso mantiene la
// haptica estable aunque el render vaya a 60 fps.
//
// Actividad 4 -- Ambientes virtuales simples.
// Respecto a la version de la Actividad 3 se anaden tres cosas, una por
// ambiente, sin tocar nada de lo que ya funcionaba:
//
//   * Lista generica de primitivas (esfera / caja OBB / semiespacio) con radio
//     de sonda. Da las paredes rectas del laberinto y la mesa del banco de
//     trabajo, que con solo esferas no se podian representar.
//   * Fuerza constante + amortiguamiento de acarreo: el peso y la inercia del
//     objeto que el usuario tiene agarrado.
//   * Riel de un grado de libertad con resorte, viscosidad y friccion seca con
//     adherencia (stick-slip): las "resistencias" del tercer ambiente.
#pragma once

#ifdef OMNIBRIDGE_EXPORTS
#define OMNI_API extern "C" __declspec(dllexport)
#else
#define OMNI_API extern "C" __declspec(dllimport)
#endif

#define OMNI_MAX_SPHERES 8    /* esferas heredadas de la Actividad 3 */
#define OMNI_MAX_SHAPES  16   /* primitivas genericas de la Actividad 4 */

// Limites de estabilidad del lazo a 1 kHz. Por encima de estos valores el
// Touch entra en ciclo limite y vibra en la mano, asi que el puente los
// aplica siempre, aunque Unity pida mas.
#define OMNI_MAX_STIFFNESS 0.25   /* N/mm    */
#define OMNI_MAX_DAMPING   0.005  /* N/(mm/s) */

// Modos de fuerza (bitmask, se pueden combinar)
#define OMNI_MODE_FREE     0   // movimiento libre: fuerza cero
#define OMNI_MODE_VISCOUS  1   // resistencia virtual (amortiguamiento)
#define OMNI_MODE_ATTRACT  2   // atraccion hacia un punto (resorte)
#define OMNI_MODE_SPHERES  4   // colision con las esferas heredadas
#define OMNI_MODE_PLANE    8   // colision con un plano (suelo/pared)
#define OMNI_MODE_SHAPES   16  // colision con la lista de primitivas
#define OMNI_MODE_CARRY    32  // peso e inercia del objeto agarrado
#define OMNI_MODE_RAIL     64  // riel 1-DOF con resistencia programable

// Tipos de primitiva
#define OMNI_SHAPE_NONE      0
#define OMNI_SHAPE_SPHERE    1  // half[0] = radio
#define OMNI_SHAPE_BOX       2  // caja orientada: half = semilados, axis = ejes
#define OMNI_SHAPE_HALFSPACE 3  // semiespacio: axis[0..2] = normal del lado libre

#pragma pack(push, 8)

// Una primitiva tocable, ya reunida desde los arreglos de OmniParams. No forma
// parte de la estructura que cruza a C#: dentro de OmniParams las primitivas
// viven en arreglos paralelos (igual que las esferas de la Actividad 3), que es
// lo que el marshalling de C# sabe copiar sin sorpresas.
struct OmniShape {
    int    type;        // OMNI_SHAPE_*
    int    id;          // identificador estable; si cambia se reinicia el contacto
    double center[3];   // centro, mm (semiespacio: un punto del plano)
    double half[3];     // semilados, mm (esfera: half[0] = radio)
    double axis[9];     // ejes locales por filas: X = axis[0..2], Y = [3..5], Z = [6..8]
    double k;           // rigidez del contacto, N/mm
    double friction;    // friccion tangencial de Coulomb (0 = superficie lisa)
};

// Estado leido del dispositivo (unidades del device frame: mm, mm/s, N).
struct OmniState {
    double position[3];      // punta del stylus, mm
    double velocity[3];      // mm/s
    double transform[16];    // matriz 4x4 column-major (pos + orientacion)
    double force[3];         // fuerza que se esta comandando, N
    double jointAngles[3];   // rad
    double gimbalAngles[3];  // rad
    double servoRateHz;      // frecuencia real medida del servo loop
    double timeSec;          // tiempo desde Omni_Init()
    double autoGain;         // 1.0 normal; baja solo si se detecta inestabilidad
    double contactForce;     // magnitud de la fuerza de contacto con las primitivas, N
    double railS;            // posicion a lo largo del riel, mm
    double railForce;        // resistencia del riel = fuerza que ejerce el usuario, N
    int    touchedMask;      // bit i = la primitiva i esta en contacto
    int    railSliding;      // 1 si el riel esta deslizando; 0 si esta adherido
    int    buttons;          // bit0 = boton 1, bit1 = boton 2
    int    inkwell;          // 1 si el stylus esta en el soporte
    int    servoTicks;       // numero de iteraciones del servo loop
    int    errorCode;        // ultimo codigo de error HD (0 = OK)
    int    running;          // 1 si el scheduler esta activo
    int    forceEnabled;     // 1 si la salida de fuerza esta habilitada
    int    unstable;         // 1 si el lazo esta oscilando (contacto demasiado rigido)
};

// Parametros de los efectos hapticos (los escribe Unity).
struct OmniParams {
    double forceScale;                     // ganancia global 0..1
    double maxForce;                       // saturacion de seguridad, N
    double damping;                        // viscosidad, N/(mm/s)
    double contactDamping;                 // amortiguamiento de contacto, N/(mm/s)
    double anchor[3];                      // punto de atraccion, mm
    double anchorK;                        // rigidez del resorte, N/mm
    double anchorRadius;                   // radio de accion, mm (0 = infinito)
    double sphereCenter[OMNI_MAX_SPHERES * 3];
    double sphereRadius[OMNI_MAX_SPHERES];
    double sphereK[OMNI_MAX_SPHERES];      // N/mm
    double planePoint[3];
    double planeNormal[3];                 // normal unitaria (lado libre)
    double planeK;                         // N/mm

    // --- Actividad 4 ------------------------------------------------------
    // Primitivas en arreglos paralelos. Unity reescribe la lista entera cada
    // frame con las OMNI_MAX_SHAPES primitivas mas cercanas a la punta, asi que
    // un laberinto de sesenta muros cabe sin problema: el servo loop solo ve
    // las que estan al alcance de la mano.
    double shapeCenter[OMNI_MAX_SHAPES * 3];   // mm
    double shapeHalf[OMNI_MAX_SHAPES * 3];     // mm (esfera: solo el primero)
    double shapeAxis[OMNI_MAX_SHAPES * 9];     // ejes locales por filas
    double shapeK[OMNI_MAX_SHAPES];            // N/mm
    double shapeFriction[OMNI_MAX_SHAPES];     // coeficiente de Coulomb
    double probeRadius;                    // radio de la punta, mm (0 = punto)

    double constForce[3];                  // peso del objeto agarrado, N
    double carryDamping;                   // inercia del objeto agarrado, N/(mm/s)

    double railOrigin[3];                  // inicio del riel, mm
    double railAxis[3];                    // direccion unitaria del riel
    double railLength;                     // recorrido util, mm
    double railK;                          // resorte de retorno, N/mm
    double railB;                          // viscosidad a lo largo del riel, N/(mm/s)
    double railFs;                         // friccion estatica (hay que vencerla), N
    double railFk;                         // friccion dinamica (mientras desliza), N
    double railStickK;                     // rigidez de la adherencia, N/mm
    double railLateralK;                   // rigidez que mantiene la punta sobre el riel, N/mm
    double railEndK;                       // rigidez de los topes de los extremos, N/mm

    int    mode;                           // bitmask OMNI_MODE_*
    int    userTag;                        // marca de ensayo, se guarda en el CSV
    int    sphereActive[OMNI_MAX_SPHERES];
    int    shapeType[OMNI_MAX_SHAPES];     // OMNI_SHAPE_*
    int    shapeId[OMNI_MAX_SHAPES];       // identificador estable de cada primitiva
};

#pragma pack(pop)

// --- ciclo de vida ---------------------------------------------------------
OMNI_API int  Omni_Init(void);          // 0 = OK, <0 = error (ver Omni_GetLastError)
OMNI_API void Omni_Shutdown(void);
OMNI_API int  Omni_IsRunning(void);
OMNI_API const char *Omni_GetLastError(void);
OMNI_API const char *Omni_GetDeviceModel(void);

// --- estado y parametros ---------------------------------------------------
OMNI_API void Omni_GetState(struct OmniState *out);
OMNI_API void Omni_SetParams(const struct OmniParams *in);
OMNI_API void Omni_GetParams(struct OmniParams *out);
OMNI_API void Omni_SetForceEnabled(int enabled);

// --- grabacion a 1 kHz para analisis en Python ------------------------------
OMNI_API int  Omni_StartRecording(int maxSamples);
OMNI_API void Omni_StopRecording(void);
OMNI_API int  Omni_GetRecordedCount(void);
OMNI_API int  Omni_SaveRecordingCsv(const char *path);

// --- comprobacion de layout (Unity la usa para validar el marshalling) ------
OMNI_API int  Omni_SizeOfState(void);
OMNI_API int  Omni_SizeOfParams(void);
