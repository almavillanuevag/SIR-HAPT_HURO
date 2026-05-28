# SIR-HAPT — Sistema Inmersivo de Rehabilitación con Retroalimentación Háptica

> Serious game en Realidad Aumentada para rehabilitación del miembro superior, desarrollado en Unity para Meta Quest 3 con integración de dispositivos hápticos bHaptics.

Versión desarrollada para pruebas en 5 grupos: sin retroalimentación (SR), con retroalimentación visual (RV), con retroalimentación háptica (RH), con retroalimentación visual y háptica (RVH), únicamente háptica (RUH).

## Instalación y configuración

### 1. Clonar el repositorio

```bash
git clone https://github.com/almavillanuevag/SIR-HAPT_HURO.git
```

O bien, desde la interfaz de GitHub:

1. Hacer clic en el botón **Code**.
2. Seleccionar **Download ZIP** y descomprimir en la ubicación deseada.


### 2. Abrir el proyecto en Unity

1. Abrir **Unity Hub**.
2. Hacer clic en **Open > Add project from disk**.
3. Navegar hasta la carpeta raíz del repositorio clonado (la que contiene las carpetas `Assets/`, `Packages/` y `ProjectSettings/`).
4. Seleccionar la carpeta y confirmar.
6. Esperar a que Unity importe todos los assets y resuelva los paquetes.

### 3. Configurar el Build Target para Android / Meta Quest 3

1. En Unity, ir a **File > Build Settings**.
2. En la lista de plataformas, seleccionar **Android**.
3. Hacer clic en **Switch Platform** y esperar a que Unity recompile el proyecto para Android.

### 4. Conectar las Meta Quest 3 en modo desarrollador

Las gafas deben estar en modo desarrollador y autorizar la conexión con la computadora.

- Activar el modo desarrollador en las gafas
- Conectar las gafas a la computadora
- Ponerse las gafas y debería aparecer  una notificación solicitando autorización de acceso USB.
- Seleccionar **Permitir** en la notificación para habilitar la transferencia de datos.

> Si la notificación no aparece, verificar que el cable tenga soporte de datos y que el modo desarrollador esté activado. 

### 5. Build and Run en las gafas

1. En Unity, ir a **File > Build Settings**.
2. En la sección **Run Device**, hacer clic en el ícono de actualización y seleccionar las Meta Quest 3 en la lista desplegable.
3. Asegurarse de que ambas escenas estén incluidas en **Scenes In Build**. Si no aparece, hacer clic en **Add Open Scenes**.
4. Hacer clic en **Build and Run**.
5. Unity compilará el proyecto, generará el `.apk` y lo instalará automáticamente en las gafas.
6. La aplicación debería de iniciar directamente en las Meta Quest 3.


## Configuración de bHaptics

Para que la retroalimentación háptica funcione correctamente:

- Los dispositivos deberán estar conectados vía Bluetooth directamente a las gafas
- Al iniciar el juego (Run) en las gafas solicitará permiso de acceder al Bluetooth, darle permitir / aceptar a todo.

