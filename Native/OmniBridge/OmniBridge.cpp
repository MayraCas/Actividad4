// OmniBridge.cpp -- implementacion del puente Unity <-> Geomagic Touch.
//
// Actividad 4: al motor de la Actividad 3 se le anaden las primitivas
// genericas (esfera / caja / semiespacio), el acarreo de objetos y el riel con
// resistencia programable. Las reglas de estabilidad que costaron un ciclo de
// correccion se mantienen intactas y ahora aplican tambien a lo nuevo:
// filtro solo en la entrada, limitador de pendiente en la salida, normal de
// contacto persistente y saturacion dura de k y b.
#define OMNIBRIDGE_EXPORTS
#include "OmniBridge.h"
#include "hd_dyn.h"

#include <atomic>
#include <cmath>
#include <cstdarg>
#include <cstdio>
#include <cstring>
#include <vector>

// ---------------------------------------------------------------- globales --
static HdApi              g_hd;
static HHD                g_device      = HD_INVALID_HANDLE;
static HDSchedulerHandle  g_callback    = 0;
static std::atomic<int>   g_running{0};
static std::atomic<int>   g_forceEnabled{1};
static char               g_error[512]  = {0};
static char               g_model[128]  = "desconocido";
static HMODULE            g_selfModule  = NULL;

// Estado compartido servo -> Unity, protegido con un seqlock (sin bloqueos:
// el servo loop nunca se queda esperando al hilo de Unity).
static OmniState             g_state = {};
static std::atomic<unsigned> g_stateSeq{0};

// Parametros Unity -> servo con doble buffer e indice atomico.
static OmniParams        g_params[2] = {};
static std::atomic<int>  g_paramsIdx{0};

// Cronometro de alta resolucion
static double g_autoGain = 1.0;   // reduccion automatica si el lazo se vuelve inestable

static LARGE_INTEGER g_freq, g_t0;
static double        g_lastTime = 0.0;

// Grabacion a frecuencia del servo
struct Sample { double t, p[3], v[3], f[3], railS, railF; int buttons, mode, tag; };
static std::vector<Sample>  g_rec;
static std::atomic<int>     g_recCount{0};
static std::atomic<int>     g_recActive{0};
static int                  g_recMax = 0;

// -------------------------------------------------------------- utilidades --
static double nowSec()
{
    LARGE_INTEGER t; QueryPerformanceCounter(&t);
    return double(t.QuadPart - g_t0.QuadPart) / double(g_freq.QuadPart);
}

static void setError(const char *fmt, ...)
{
    va_list ap; va_start(ap, fmt);
    _vsnprintf_s(g_error, sizeof(g_error), _TRUNCATE, fmt, ap);
    va_end(ap);
}

static bool checkHdError(const char *ctx)
{
    HDErrorInfo e = g_hd.hdGetError();
    if (e.errorCode == HD_SUCCESS) return false;
    const char *msg = g_hd.hdGetErrorString(e.errorCode);
    setError("%s: %s (codigo HD 0x%04X, interno %d)",
             ctx, msg ? msg : "error desconocido", e.errorCode, e.internalErrorCode);
    return true;
}

static inline double limitar(double x, double lo, double hi)
{
    return x < lo ? lo : (x > hi ? hi : x);
}

/// Transicion suave de 0 a 1 entre a y b (evita escalones de fuerza).
static inline double transicion(double x, double a, double b)
{
    if (x <= a) return 0.0;
    if (x >= b) return 1.0;
    double t = (x - a) / (b - a);
    return t * t * (3.0 - 2.0 * t);
}

static inline double punto(const double a[3], const double b[3])
{
    return a[0]*b[0] + a[1]*b[1] + a[2]*b[2];
}

static inline void normalizar(double n[3])
{
    double L = sqrt(n[0]*n[0] + n[1]*n[1] + n[2]*n[2]);
    if (L > 1e-9) { n[0] /= L; n[1] /= L; n[2] /= L; }
}

static void defaultParams(OmniParams &p)
{
    memset(&p, 0, sizeof(p));
    p.mode           = OMNI_MODE_FREE;
    p.forceScale     = 1.0;
    p.maxForce       = 3.0;    // N (el Touch entrega ~3.3 N de pico)
    p.damping        = 0.0025; // N/(mm/s) -- por encima de ~0.005 el lazo zumba
    p.contactDamping = 0.0015; // N/(mm/s) -- amortigua el contacto y permite mas rigidez
    p.anchorK        = 0.05;   // N/mm
    p.anchorRadius   = 0.0;
    p.planeNormal[1] = 1.0;
    p.planeK         = 0.15;
    for (int i = 0; i < OMNI_MAX_SPHERES; ++i) p.sphereK[i] = 0.15;

    p.probeRadius  = 0.0;
    p.carryDamping = 0.0;
    p.railAxis[0]  = 1.0;
    p.railLength   = 60.0;
    p.railStickK   = 0.12;
    p.railLateralK = 0.18;
    p.railEndK     = 0.20;
}

// ------------------------------------------------- contacto con primitivas --
// Memoria de contacto: una entrada por ranura de la lista. Guarda si la punta
// estaba dentro, la normal con la que entro y, para las cajas, que cara se
// eligio. Se indexa por ranura, pero se valida contra el id que manda Unity:
// si Unity reordena la lista (manda las 16 primitivas mas cercanas, y esas
// cambian al moverse la mano) el contacto de esa ranura se reinicia en vez de
// heredar la normal de otro objeto.
struct MemoriaContacto {
    int    id;
    int    dentro;
    int    eje;      // cara elegida en la caja: 0,1,2
    double signo;    // +1 / -1
    double n[3];     // normal memorizada, en el marco del dispositivo
};

/// Reune la primitiva i desde los arreglos paralelos de OmniParams.
static void leerForma(const OmniParams &prm, int i, OmniShape &s)
{
    s.type = prm.shapeType[i];
    s.id   = prm.shapeId[i];
    memcpy(s.center, &prm.shapeCenter[i * 3], 3 * sizeof(double));
    memcpy(s.half,   &prm.shapeHalf[i * 3],   3 * sizeof(double));
    memcpy(s.axis,   &prm.shapeAxis[i * 9],   9 * sizeof(double));
    s.k        = prm.shapeK[i];
    s.friction = prm.shapeFriction[i];
}

/// Fuerza de contacto de UNA primitiva. Devuelve la magnitud normal (0 si no
/// hay contacto) y acumula la fuerza en fout.
static double contactoForma(const OmniShape &s, const double p[3], const double vf[3],
                            double probeR, double bc, MemoriaContacto &m, double fout[3])
{
    if (s.id != m.id) { m.id = s.id; m.dentro = 0; }
    if (s.type == OMNI_SHAPE_NONE) { m.dentro = 0; return 0.0; }

    double d[3] = { p[0] - s.center[0], p[1] - s.center[1], p[2] - s.center[2] };
    double n[3] = { 0, 1, 0 };
    double prof = 0.0, profMax = 0.0;

    if (s.type == OMNI_SHAPE_SPHERE) {
        double R = s.half[0] + probeR;
        double L = sqrt(d[0]*d[0] + d[1]*d[1] + d[2]*d[2]);
        if (R <= 0.0 || L >= R) { m.dentro = 0; return 0.0; }

        double nr[3];
        if (L > 1e-4)     { nr[0] = d[0]/L; nr[1] = d[1]/L; nr[2] = d[2]/L; }
        else if (m.dentro){ nr[0] = m.n[0]; nr[1] = m.n[1]; nr[2] = m.n[2]; }
        else              { nr[0] = 0.0; nr[1] = 1.0; nr[2] = 0.0; }

        if (!m.dentro) {
            m.dentro = 1;
            m.n[0] = nr[0]; m.n[1] = nr[1]; m.n[2] = nr[2];
        } else if (punto(nr, m.n) > 0.2) {
            // Deslizamiento normal sobre la superficie: la normal sigue a la punta.
            for (int i = 0; i < 3; ++i) m.n[i] += 0.05 * (nr[i] - m.n[i]);
            normalizar(m.n);
        }
        // Si el producto punto es <= 0.2 se conserva la normal de entrada: sin
        // salto de +3 N a -3 N al pasar cerca del centro, y sin atravesar.
        n[0] = m.n[0]; n[1] = m.n[1]; n[2] = m.n[2];
        prof    = R - punto(d, n);
        profMax = R;
    }
    else if (s.type == OMNI_SHAPE_BOX) {
        // Coordenadas locales de la punta y semilados inflados por el radio de
        // la sonda: asi la punta se comporta como una esfera rodando contra la
        // caja, que es justo la bola del laberinto.
        const double *ax = &s.axis[0], *ay = &s.axis[3], *az = &s.axis[6];
        double q[3] = { punto(d, ax), punto(d, ay), punto(d, az) };
        double h[3] = { s.half[0] + probeR, s.half[1] + probeR, s.half[2] + probeR };

        double pen[3];
        for (int i = 0; i < 3; ++i) {
            pen[i] = h[i] - fabs(q[i]);
            if (pen[i] <= 0.0) { m.dentro = 0; return 0.0; }   // fuera de la caja
        }

        // Cara de salida: la de menor penetracion.
        int eje = 0;
        if (pen[1] < pen[eje]) eje = 1;
        if (pen[2] < pen[eje]) eje = 2;
        double signo = (q[eje] >= 0.0) ? 1.0 : -1.0;

        if (!m.dentro) { m.dentro = 1; m.eje = eje; m.signo = signo; }
        // Ya dentro: solo se cambia de cara si la nueva es la misma o vecina en
        // el mismo sentido; si el usuario empuja hasta pasar el centro se
        // conserva la cara de entrada y la caja sigue empujando hacia afuera.
        else if (eje == m.eje && signo == m.signo) { /* misma cara */ }
        else if (pen[m.eje] <= 0.0) { m.eje = eje; m.signo = signo; }

        const double *e = (m.eje == 0) ? ax : (m.eje == 1) ? ay : az;
        n[0] = m.signo * e[0]; n[1] = m.signo * e[1]; n[2] = m.signo * e[2];
        prof    = h[m.eje] - m.signo * q[m.eje];
        profMax = 2.0 * h[m.eje];
        if (prof > h[m.eje]) prof = h[m.eje];   // acota la fuerza si se atraveso el centro
    }
    else if (s.type == OMNI_SHAPE_HALFSPACE) {
        n[0] = s.axis[0]; n[1] = s.axis[1]; n[2] = s.axis[2];
        double dist = punto(d, n) - probeR;
        if (dist >= 0.0) { m.dentro = 0; return 0.0; }
        m.dentro = 1;
        prof    = -dist;
        profMax = 1e9;
    }
    else { m.dentro = 0; return 0.0; }

    if (prof < 0.0)       prof = 0.0;
    if (prof > profMax)   prof = profMax;

    double k  = limitar(s.k, 0.0, OMNI_MAX_STIFFNESS);
    double vn = punto(vf, n);
    double fn = k * prof - bc * vn;      // resorte de contacto + amortiguamiento
    if (fn < 0.0) fn = 0.0;              // una superficie empuja, no pega
    for (int i = 0; i < 3; ++i) fout[i] += fn * n[i];

    // Friccion tangencial de Coulomb, regularizada con la velocidad para que no
    // zumbe con la mano casi quieta. Es lo que da la textura aspera de las
    // paredes del laberinto y de la mesa del banco de trabajo.
    double mu = limitar(s.friction, 0.0, 1.0);
    if (mu > 0.0 && fn > 0.0) {
        double vt[3] = { vf[0] - vn*n[0], vf[1] - vn*n[1], vf[2] - vn*n[2] };
        double vtL = sqrt(vt[0]*vt[0] + vt[1]*vt[1] + vt[2]*vt[2]);
        if (vtL > 1e-6) {
            double ft = mu * fn * transicion(vtL, 0.5, 3.0);
            for (int i = 0; i < 3; ++i) fout[i] -= ft * vt[i] / vtL;
        }
    }
    return fn;
}

// --------------------------------------------------------- bucle haptico ----
// Se ejecuta ~1000 veces por segundo dentro del hilo servo de OpenHaptics.
// Debe ser corto: sin asignaciones de memoria, sin bloqueos, sin I/O.
static HDenum __cdecl servoLoop(void * /*userData*/)
{
    HHD hHD = g_hd.hdGetCurrentDevice();
    g_hd.hdBeginFrame(hHD);

    double p[3] = {0}, v[3] = {0}, T[16] = {0}, ja[3] = {0}, ga[3] = {0};
    HDint     btn = 0;
    HDboolean ink = 0;

    g_hd.hdGetDoublev(HD_CURRENT_POSITION, p);
    g_hd.hdGetDoublev(HD_CURRENT_VELOCITY, v);
    g_hd.hdGetDoublev(HD_CURRENT_TRANSFORM, T);
    g_hd.hdGetDoublev(HD_CURRENT_JOINT_ANGLES, ja);
    g_hd.hdGetDoublev(HD_CURRENT_GIMBAL_ANGLES, ga);
    g_hd.hdGetIntegerv(HD_CURRENT_BUTTONS, &btn);
    g_hd.hdGetBooleanv(HD_CURRENT_INKWELL_SWITCH, &ink);   // es booleano, no int

    const OmniParams &prm = g_params[g_paramsIdx.load(std::memory_order_acquire)];

    // --- la velocidad que reporta el equipo es ruidosa: se filtra ----------
    // El filtro va sobre la ENTRADA, nunca sobre la fuerza de salida. Filtrar
    // la salida mete retardo de fase dentro del lazo cerrado, y ese retardo es
    // justo lo que pone a vibrar la viscosidad y los contactos rigidos.
    static double vf[3] = {0, 0, 0};
    const double alphaV = 0.20;                     // ~30 Hz de corte
    for (int i = 0; i < 3; ++i) vf[i] += alphaV * (v[i] - vf[i]);
    double vfMag = sqrt(vf[0]*vf[0] + vf[1]*vf[1] + vf[2]*vf[2]);

    double f[3] = {0, 0, 0};
    double bc = limitar(prm.contactDamping, 0.0, OMNI_MAX_DAMPING);
    double lim = (prm.maxForce > 0.0 && prm.maxForce < 3.3) ? prm.maxForce : 3.0;

    int    tocadas    = 0;
    double fContacto  = 0.0;
    double railS      = 0.0;
    double railFuerza = 0.0;
    int    railDesliza = 0;

    // 1) Resistencia virtual: F = -b*v.
    //    La rampa evita el zumbido con la mano casi quieta, donde lo unico que
    //    queda en la senal de velocidad es el ruido de cuantizacion del encoder.
    if (prm.mode & OMNI_MODE_VISCOUS) {
        double b = limitar(prm.damping, 0.0, OMNI_MAX_DAMPING);
        double g = transicion(vfMag, 3.0, 8.0);     // 0 por debajo de 3 mm/s
        for (int i = 0; i < 3; ++i) f[i] -= b * g * vf[i];
    }

    // 2) Atraccion hacia un punto: resorte con un poco de amortiguamiento,
    //    para que la punta se asiente en el destino en vez de orbitarlo.
    //    En la Actividad 4 es tambien el resorte que une la punta con el objeto
    //    que se lleva agarrado.
    if (prm.mode & OMNI_MODE_ATTRACT) {
        double d[3] = {prm.anchor[0] - p[0], prm.anchor[1] - p[1], prm.anchor[2] - p[2]};
        double L = sqrt(d[0]*d[0] + d[1]*d[1] + d[2]*d[2]);
        if (prm.anchorRadius <= 0.0 || L < prm.anchorRadius) {
            double k = limitar(prm.anchorK, 0.0, OMNI_MAX_STIFFNESS);
            for (int i = 0; i < 3; ++i) f[i] += k * d[i] - 0.0008 * vf[i];
        }
    }

    // 3) Colision con las esferas heredadas de la Actividad 3.
    if (prm.mode & OMNI_MODE_SPHERES) {
        static MemoriaContacto memEsf[OMNI_MAX_SPHERES] = {};
        for (int s = 0; s < OMNI_MAX_SPHERES; ++s) {
            if (!prm.sphereActive[s]) { memEsf[s].dentro = 0; continue; }
            OmniShape sh = {};
            sh.type    = OMNI_SHAPE_SPHERE;
            sh.id      = 1000 + s;
            sh.center[0] = prm.sphereCenter[s*3+0];
            sh.center[1] = prm.sphereCenter[s*3+1];
            sh.center[2] = prm.sphereCenter[s*3+2];
            sh.half[0] = prm.sphereRadius[s];
            sh.k       = prm.sphereK[s];
            contactoForma(sh, p, vf, 0.0, bc, memEsf[s], f);
        }
    }

    // 4) Colision con un plano (suelo o pared virtual).
    if (prm.mode & OMNI_MODE_PLANE) {
        const double *n = prm.planeNormal;
        double d[3] = {p[0] - prm.planePoint[0], p[1] - prm.planePoint[1], p[2] - prm.planePoint[2]};
        double dist = punto(d, n);
        if (dist < 0.0) {
            double k  = limitar(prm.planeK, 0.0, OMNI_MAX_STIFFNESS);
            double vn = punto(vf, n);
            double fn = k * (-dist) - bc * vn;
            if (fn < 0.0) fn = 0.0;
            for (int i = 0; i < 3; ++i) f[i] += fn * n[i];
        }
    }

    // 5) Lista generica de primitivas: la mesa y las piezas del ambiente 1, y
    //    todos los muros del laberinto del ambiente 2.
    if (prm.mode & OMNI_MODE_SHAPES) {
        static MemoriaContacto mem[OMNI_MAX_SHAPES] = {};
        double probeR = prm.probeRadius > 0.0 ? prm.probeRadius : 0.0;
        OmniShape sh;
        for (int i = 0; i < OMNI_MAX_SHAPES; ++i) {
            leerForma(prm, i, sh);
            double fn = contactoForma(sh, p, vf, probeR, bc, mem[i], f);
            if (fn > 0.0) { tocadas |= (1 << i); fContacto += fn; }
        }
    }

    // 6) Acarreo: el peso y la inercia del objeto que el usuario tiene agarrado.
    //    El peso es una fuerza constante (no depende de la posicion), asi que
    //    actualizarla a 60 Hz desde Unity no introduce ningun problema: lo que
    //    exige 1 kHz son los contactos, no la gravedad.
    if (prm.mode & OMNI_MODE_CARRY) {
        double cf[3] = { prm.constForce[0], prm.constForce[1], prm.constForce[2] };
        double m = sqrt(cf[0]*cf[0] + cf[1]*cf[1] + cf[2]*cf[2]);
        if (m > lim) { double e = lim / m; cf[0] *= e; cf[1] *= e; cf[2] *= e; }
        double b = limitar(prm.carryDamping, 0.0, OMNI_MAX_DAMPING);
        for (int i = 0; i < 3; ++i) f[i] += cf[i] - b * vf[i];
    }

    // 7) Riel de un grado de libertad con resistencia programable.
    //    Modelo: resorte + viscosidad + friccion seca con adherencia. La
    //    adherencia es lo que produce el stick-slip -- hay que vencer Fs para
    //    que empiece a moverse, y al hacerlo la resistencia cae a Fk -- que es
    //    exactamente la sensacion de un cajon atascado.
    {
        static int    pegado = 1;
        static double sPeg   = 0.0;
        static int    tagPrev = -1;

        if (prm.mode & OMNI_MODE_RAIL) {
            double eje[3] = { prm.railAxis[0], prm.railAxis[1], prm.railAxis[2] };
            normalizar(eje);
            double d[3] = { p[0] - prm.railOrigin[0], p[1] - prm.railOrigin[1], p[2] - prm.railOrigin[2] };
            double s  = punto(d, eje);
            double vs = punto(vf, eje);

            // Al cambiar de estacion se reinicia la adherencia en la posicion actual.
            if (prm.userTag != tagPrev) { tagPrev = prm.userTag; pegado = 1; sPeg = s; }

            // Canal lateral: mantiene la punta sobre la linea del riel.
            double kLat = limitar(prm.railLateralK, 0.0, OMNI_MAX_STIFFNESS);
            for (int i = 0; i < 3; ++i) {
                double lat  = d[i]  - s  * eje[i];
                double vlat = vf[i] - vs * eje[i];
                f[i] += -kLat * lat - bc * vlat;
            }

            double fAlong = 0.0;
            fAlong -= limitar(prm.railK, 0.0, OMNI_MAX_STIFFNESS) * s;      // resorte de retorno
            fAlong -= limitar(prm.railB, 0.0, OMNI_MAX_DAMPING)  * vs;      // viscosidad

            double kEnd = limitar(prm.railEndK, 0.0, OMNI_MAX_STIFFNESS);   // topes duros
            if (s < 0.0)             fAlong += kEnd * (-s);
            if (s > prm.railLength)  fAlong -= kEnd * (s - prm.railLength);

            double Fs = limitar(prm.railFs, 0.0, lim);
            double Fk = limitar(prm.railFk, 0.0, lim);
            if (pegado) {
                double kSt = limitar(prm.railStickK, 0.0, OMNI_MAX_STIFFNESS);
                double fSt = -kSt * (s - sPeg);
                if (fabs(fSt) > Fs) pegado = 0;      // se rompe la adherencia
                else                fAlong += fSt;
            }
            if (!pegado) {
                double g = transicion(fabs(vs), 0.5, 3.0);
                fAlong -= Fk * (vs >= 0.0 ? 1.0 : -1.0) * g;
                if (fabs(vs) < 0.5) { pegado = 1; sPeg = s; }   // se vuelve a pegar
            }

            for (int i = 0; i < 3; ++i) f[i] += fAlong * eje[i];

            railS       = s;
            railFuerza  = -fAlong;      // lo que el usuario tiene que aplicar
            railDesliza = pegado ? 0 : 1;
        } else {
            pegado = 1; sPeg = 0.0; tagPrev = -1;
        }
    }

    // --- rampa al cambiar de modo ------------------------------------------
    // Evita el tiron si se cambia de modo con la punta ya dentro de un objeto.
    static int    modoPrev = -1;
    static double rampa    = 0.0;
    if (prm.mode != modoPrev) { modoPrev = prm.mode; rampa = 0.0; }
    if (rampa < 1.0) rampa += 1.0 / 250.0;          // 250 ms
    if (rampa > 1.0) rampa = 1.0;

    // --- escala y saturacion -----------------------------------------------
    double scale = limitar(prm.forceScale, 0.0, 1.0) * rampa * g_autoGain;
    for (int i = 0; i < 3; ++i) f[i] *= scale;

    double mag = sqrt(f[0]*f[0] + f[1]*f[1] + f[2]*f[2]);
    if (mag > lim) { double k = lim / mag; f[0] *= k; f[1] *= k; f[2] *= k; }
    if (!(f[0] == f[0]) || !(f[1] == f[1]) || !(f[2] == f[2])) { f[0] = f[1] = f[2] = 0.0; }

    // --- limite de pendiente ------------------------------------------------
    // Suaviza los saltos (cambio de modo, entrada en contacto) sin anadir
    // ningun retardo mientras la fuerza es estable. Esa es la diferencia con
    // un filtro pasa-bajos, y la razon de que los contactos no vibren.
    static double fPrev[3] = {0, 0, 0};
    const double maxSalto = 0.10;                   // N por ciclo -> 100 N/s
    for (int i = 0; i < 3; ++i) {
        double dif = f[i] - fPrev[i];
        if (dif >  maxSalto) dif =  maxSalto;
        if (dif < -maxSalto) dif = -maxSalto;
        fPrev[i] += dif;
        f[i] = fPrev[i];
    }

    // --- deteccion de ciclo limite ------------------------------------------
    // Si la fuerza de salida oscila a alta frecuencia, el lazo se volvio
    // inestable: se baja la ganancia hasta que deje de vibrar, en lugar de
    // dejar el equipo sacudiendose en la mano. Se recupera sola.
    static double fLP[3] = {0, 0, 0};
    static double hfProm = 0.0;
    double hf2 = 0.0;
    for (int i = 0; i < 3; ++i) {
        fLP[i] += 0.02 * (f[i] - fLP[i]);           // ~3 Hz
        double e = f[i] - fLP[i];
        hf2 += e * e;
    }
    hfProm += 0.01 * (sqrt(hf2) - hfProm);          // ventana de ~100 ms
    int inestable = (hfProm > 0.30) ? 1 : 0;
    if (inestable) {
        g_autoGain -= 0.002;                        // baja en ~0.4 s
        if (g_autoGain < 0.20) g_autoGain = 0.20;
    } else if (g_autoGain < 1.0) {
        g_autoGain += 0.0005;                       // recupera en ~2 s
        if (g_autoGain > 1.0) g_autoGain = 1.0;
    }

    if (!g_forceEnabled.load(std::memory_order_relaxed)) {
        f[0] = f[1] = f[2] = 0.0;
        fPrev[0] = fPrev[1] = fPrev[2] = 0.0;
        fLP[0] = fLP[1] = fLP[2] = 0.0;
        hfProm = 0.0;
    }

    g_hd.hdSetDoublev(HD_CURRENT_FORCE, f);
    g_hd.hdEndFrame(hHD);

    // --- publicar estado (seqlock) ----------------------------------------
    double t  = nowSec();
    double dt = t - g_lastTime;
    g_lastTime = t;

    unsigned seq = g_stateSeq.load(std::memory_order_relaxed);
    g_stateSeq.store(seq + 1, std::memory_order_release);        // impar: escribiendo
    std::atomic_thread_fence(std::memory_order_release);

    memcpy(g_state.position, p, sizeof(p));
    memcpy(g_state.velocity, v, sizeof(v));
    memcpy(g_state.transform, T, sizeof(T));
    memcpy(g_state.force, f, sizeof(f));
    memcpy(g_state.jointAngles, ja, sizeof(ja));
    memcpy(g_state.gimbalAngles, ga, sizeof(ga));
    g_state.buttons     = btn;
    g_state.inkwell     = ink ? 1 : 0;
    g_state.servoTicks += 1;
    g_state.timeSec     = t;
    if (dt > 1e-6 && dt < 0.1) {
        double hz = 1.0 / dt;
        g_state.servoRateHz = (g_state.servoRateHz <= 0.0) ? hz
                            : g_state.servoRateHz * 0.99 + hz * 0.01;
    }
    g_state.running      = 1;
    g_state.forceEnabled = g_forceEnabled.load(std::memory_order_relaxed);
    g_state.autoGain     = g_autoGain;
    g_state.unstable     = inestable;
    g_state.touchedMask  = tocadas;
    g_state.contactForce = fContacto;
    g_state.railS        = railS;
    g_state.railForce    = railFuerza;
    g_state.railSliding  = railDesliza;

    std::atomic_thread_fence(std::memory_order_release);
    g_stateSeq.store(seq + 2, std::memory_order_release);        // par: listo

    // --- grabacion opcional a 1 kHz ---------------------------------------
    if (g_recActive.load(std::memory_order_relaxed)) {
        int i = g_recCount.load(std::memory_order_relaxed);
        if (i < g_recMax) {
            Sample &s = g_rec[i];
            s.t = t;
            memcpy(s.p, p, sizeof(p));
            memcpy(s.v, v, sizeof(v));
            memcpy(s.f, f, sizeof(f));
            s.railS   = railS;
            s.railF   = railFuerza;
            s.buttons = btn;
            s.mode    = prm.mode;
            s.tag     = prm.userTag;
            g_recCount.store(i + 1, std::memory_order_release);
        } else {
            g_recActive.store(0, std::memory_order_release);
        }
    }

    // Errores del dispositivo: se registran; solo se corta la fuerza si el
    // fallo es persistente.
    HDErrorInfo e = g_hd.hdGetError();
    if (e.errorCode != HD_SUCCESS) {
        g_state.errorCode = (int)e.errorCode;
        static int consecutivos = 0;
        if (++consecutivos > 500) {
            g_forceEnabled.store(0, std::memory_order_relaxed);
            consecutivos = 0;
        }
    } else {
        g_state.errorCode = 0;
    }

    return HD_CALLBACK_CONTINUE;
}

// ------------------------------------------------------------ ciclo de vida -
OMNI_API int Omni_Init(void)
{
    if (g_running.load()) return 0;             // idempotente

    QueryPerformanceFrequency(&g_freq);
    QueryPerformanceCounter(&g_t0);
    g_lastTime = 0.0;

    defaultParams(g_params[0]);
    defaultParams(g_params[1]);
    g_paramsIdx.store(0);
    memset(&g_state, 0, sizeof(g_state));
    g_autoGain = 1.0;

    if (!g_hd.mod && !hdDynLoad(g_hd, g_selfModule, g_error, sizeof(g_error)))
        return -1;

    g_device = g_hd.hdInitDevice(HD_DEFAULT_DEVICE);
    if (checkHdError("No se pudo inicializar el dispositivo haptico")) {
        g_device = HD_INVALID_HANDLE;
        return -2;
    }

    // Identificacion del dispositivo (constantes verificadas contra hd.dll 3.5):
    //   0x2500 HD_VERSION  0x2501 modelo  0x2502 driver  0x2503 fabricante
    //   0x2504 numero de serie
    const char *ver    = g_hd.hdGetString(0x2500);
    const char *model  = g_hd.hdGetString(0x2501);
    const char *driver = g_hd.hdGetString(0x2502);
    const char *serial = g_hd.hdGetString(0x2504);
    _snprintf_s(g_model, sizeof(g_model), _TRUNCATE, "%s  (S/N %s)  driver %s  HD %s",
                model  ? model  : "?", serial ? serial : "?",
                driver ? driver : "?", ver    ? ver    : "?");
    g_hd.hdGetError();                          // limpiar por si algun query no aplica

    g_hd.hdMakeCurrentDevice(g_device);
    g_hd.hdEnable(HD_FORCE_OUTPUT);
    g_hd.hdEnable(HD_MAX_FORCE_CLAMPING);
    g_hd.hdGetError();

    g_hd.hdSetSchedulerRate(1000.0);
    g_hd.hdGetError();

    g_callback = g_hd.hdScheduleAsynchronous(servoLoop, NULL, HD_MAX_SCHEDULER_PRIORITY);
    if (checkHdError("No se pudo agendar el bucle haptico")) {
        g_hd.hdDisableDevice(g_device);
        g_device = HD_INVALID_HANDLE;
        return -3;
    }

    g_hd.hdStartScheduler();
    if (checkHdError("No se pudo arrancar el scheduler")) {
        g_hd.hdUnschedule(g_callback);
        g_hd.hdDisableDevice(g_device);
        g_device = HD_INVALID_HANDLE;
        return -4;
    }

    g_forceEnabled.store(1);
    g_running.store(1);
    g_error[0] = '\0';
    return 0;
}

OMNI_API void Omni_Shutdown(void)
{
    if (!g_running.load()) return;
    g_running.store(0);
    g_recActive.store(0);

    g_forceEnabled.store(0);
    Sleep(20);                                   // deja que el servo saque F = 0

    g_hd.hdStopScheduler();
    if (g_callback) { g_hd.hdUnschedule(g_callback); g_callback = 0; }
    if (g_device != HD_INVALID_HANDLE) {
        g_hd.hdDisableDevice(g_device);
        g_device = HD_INVALID_HANDLE;
    }
    g_state.running = 0;
}

OMNI_API int  Omni_IsRunning(void)             { return g_running.load(); }
OMNI_API const char *Omni_GetLastError(void)   { return g_error; }
OMNI_API const char *Omni_GetDeviceModel(void) { return g_model; }
OMNI_API void Omni_SetForceEnabled(int on)     { g_forceEnabled.store(on ? 1 : 0); }
OMNI_API int  Omni_SizeOfState(void)           { return (int)sizeof(OmniState); }
OMNI_API int  Omni_SizeOfParams(void)          { return (int)sizeof(OmniParams); }

OMNI_API void Omni_GetState(OmniState *out)
{
    if (!out) return;
    for (int intento = 0; intento < 8; ++intento) {
        unsigned s1 = g_stateSeq.load(std::memory_order_acquire);
        if (s1 & 1u) continue;                   // escritura en curso
        std::atomic_thread_fence(std::memory_order_acquire);
        memcpy(out, &g_state, sizeof(OmniState));
        std::atomic_thread_fence(std::memory_order_acquire);
        if (g_stateSeq.load(std::memory_order_acquire) == s1) return;
    }
    memcpy(out, &g_state, sizeof(OmniState));    // ultimo recurso
}

OMNI_API void Omni_SetParams(const OmniParams *in)
{
    if (!in) return;
    int cur  = g_paramsIdx.load(std::memory_order_relaxed);
    int next = 1 - cur;
    memcpy(&g_params[next], in, sizeof(OmniParams));
    g_paramsIdx.store(next, std::memory_order_release);
}

OMNI_API void Omni_GetParams(OmniParams *out)
{
    if (!out) return;
    memcpy(out, &g_params[g_paramsIdx.load(std::memory_order_acquire)], sizeof(OmniParams));
}

// --------------------------------------------------------------- grabacion --
OMNI_API int Omni_StartRecording(int maxSamples)
{
    if (maxSamples <= 0) return -1;
    if (maxSamples > 2000000) maxSamples = 2000000;   // ~33 min a 1 kHz
    g_recActive.store(0);
    g_rec.assign((size_t)maxSamples, Sample{});
    g_recMax = maxSamples;
    g_recCount.store(0);
    g_recActive.store(1);
    return 0;
}

OMNI_API void Omni_StopRecording(void)    { g_recActive.store(0); }
OMNI_API int  Omni_GetRecordedCount(void) { return g_recCount.load(); }

OMNI_API int Omni_SaveRecordingCsv(const char *path)
{
    g_recActive.store(0);
    int n = g_recCount.load();
    FILE *fp = NULL;
    if (fopen_s(&fp, path, "w") != 0 || !fp) {
        setError("No se pudo escribir %s", path);
        return -1;
    }
    fprintf(fp, "t,px,py,pz,vx,vy,vz,fx,fy,fz,buttons,mode,tag,rail_s,rail_f\n");
    for (int i = 0; i < n; ++i) {
        const Sample &s = g_rec[i];
        fprintf(fp, "%.6f,%.4f,%.4f,%.4f,%.3f,%.3f,%.3f,%.5f,%.5f,%.5f,%d,%d,%d,%.4f,%.5f\n",
                s.t, s.p[0], s.p[1], s.p[2], s.v[0], s.v[1], s.v[2],
                s.f[0], s.f[1], s.f[2], s.buttons, s.mode, s.tag, s.railS, s.railF);
    }
    fclose(fp);
    return n;
}

// ---------------------------------------------------------------- DllMain ---
BOOL APIENTRY DllMain(HMODULE hModule, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH) {
        g_selfModule = hModule;
        DisableThreadLibraryCalls(hModule);
    }
    return TRUE;
}
