# Laurel

*Logros ganados, nunca regalados.*

**Otros idiomas:** [English](leeme/README.en.md) · [Português](leeme/README.pt-BR.md) · [Français](leeme/README.fr.md) · [Deutsch](leeme/README.de.md) · [Русский](leeme/README.ru.md) · [简体中文](leeme/README.zh-CN.md) · [日本語](leeme/README.ja.md)

Prepara tus juegos para que **Hydra Launcher registre los logros cuando los consigues jugando**.
La app nunca desbloquea logros por su cuenta.

## Cómo usarla
1. Abre `App\Laurel.exe` y **elige tu idioma**. Hay 8: español, inglés, portugués, francés, alemán, ruso, chino y japonés. Si marcas "No volver a preguntar", puedes cambiarlo luego con el botón **Idioma**.
2. La primera vez, la app busca sola la carpeta de descargas de Hydra (solo lee sus datos, no los cambia). Si tus juegos están en otro sitio, pulsa **Añadir…**.
3. Escribe tu **clave de API de Steam**. Es gratis y la consigues con el enlace "Obtener clave". Sin ella, la app puede revisar tus juegos pero no prepararlos. **La clave no se guarda nunca:** vive solo en memoria y tendrás que escribirla cada vez que abras la app.
4. Pulsa **Preparar juegos reparables**.
5. Déjala abierta o minimízala (se queda junto al reloj). Cuando instales un juego, esperará a que termine la instalación y lo preparará sola.

**Consejo:** las tarjetas de colores sobre la lista cuentan los juegos de cada estado; haz clic en una para ver solo esos juegos. El buscador filtra por nombre, emulador o AppID.

## Qué significa cada estado
| Estado | Significado |
|---|---|
| OK | Hydra ya encuentra el archivo de logros de ese juego |
| LISTO | Configurado; el archivo aparecerá con tu primer logro |
| STEAM | `steam_api.dll` es la original de Valve (firma verificada): Hydra lee los logros de tu cuenta de Steam |
| REPARABLE | Usa Goldberg pero le falta la lista de logros; la app puede crearla |
| SIN APPID | Haz clic derecho en el juego → "Asignar AppID" |
| NO SOPORTADO | La DLL no es de Valve y no contiene firmas de emuladores conocidos: su emulador no guarda logros donde Hydra los busca |
| SIN STEAM | No usa Steam. La app indica qué plataforma usa: GOG (con o sin logros), Epic, Ubisoft, Microsoft Store o ninguna |

## Cómo protege tus archivos
- **Nunca sobrescribe** un archivo que ya existe: solo crea los que faltan.
- **Nunca borra** nada por su cuenta.
- Anota cada archivo que crea en `cambios.json`.
- **Deshacer cambios** manda a la papelera solo los archivos creados por la app que nadie ha tocado después. Si tú o el juego los has modificado, se conservan.
- El **modo simulación** muestra lo que haría sin escribir nada.

## Identificador de DLL
Cuando un juego tiene `steam_api.dll` pero ningún archivo de configuración conocido, la app examina la DLL **en modo solo lectura**:
1. Verifica su firma digital con Windows, sin conectarse a internet. Si la firma es de Valve y es válida, el juego es una copia de Steam.
2. Busca dentro del archivo firmas de emuladores conocidos: Goldberg, CODEX, RUNE, EMPRESS, TENOKE, OnlineFix, CreamAPI, SmartSteamEmu, 3DM, ALI213, SKIDROW, RLD! y Razor1911.

El resultado aparece en "Ver detalle" (clic derecho sobre el juego).

## Puentes de logros
Algunos juegos sí registran logros, pero en un sitio que Hydra no lee. Con la opción **Puentes de logros** activada, la app copia esos logros **ya conseguidos** al formato que Hydra entiende (`%AppData%\GSE Saves\<AppID>\achievements.json`). Nunca inventa logros.
- **GOG Galaxy → Hydra:** para juegos de GOG con logros. Asígnales el AppID de Steam (clic derecho → "Asignar AppID"). La app lee una *copia temporal* de la base de datos de GOG Galaxy, nunca la original. Si escribiste tu clave de API, solo copia los logros cuyo nombre coincide con los de Steam.
- **RLE → Hydra:** Hydra conoce la carpeta de RLE (`%AppData%\RLE`) pero no la revisa; el puente la cubre.
- Si el archivo de destino ya existe y no lo creó la app (por ejemplo, porque el juego usa Goldberg), **no se toca**.
- Lo que crea un puente queda en `cambios.json` y se puede deshacer.

## Guía para juegos NO SOPORTADO
La app **no instala emuladores**. Para estos juegos, clic derecho → **"Guía para seguir sus logros…"** abre una guía que te acompaña:
1. **Copia de seguridad** de la DLL actual (como `.hydralogros-original`, sin sobrescribir nada).
2. Enlace al repositorio oficial de gbe_fork.
3. Abre la carpeta de la DLL para que **tú** la sustituyas.
4. **Comprobar:** si detecta gbe_fork, prepara el juego automáticamente.

Solo se ofrece para copias cuya DLL ya no es la original de Valve.

## Privacidad y uso de la API de Steam
- **Qué se guarda en disco:** solo las bibliotecas, las opciones (incluido el idioma), los AppID que asignes y la lista de archivos creados (para poder deshacer). Nunca la clave de API. Si un `config.json` antiguo la tenía, la app la borra al arrancar.
- **El registro no guarda datos personales:** en `registro.txt` y en pantalla, la carpeta de usuario aparece como `%USERPROFILE%`, el nombre de usuario como `<usuario>`, y los correos y la clave se ocultan.
- **La app consulta Steam con prudencia** para que el uso de tu clave sea normal:
  - una sola consulta cada vez, con al menos 3 segundos entre consultas;
  - como máximo 150 consultas por sesión;
  - cada juego se consulta una sola vez por sesión;
  - si Steam rechaza la clave, no se vuelve a usar hasta que escribas otra;
  - si Steam pide bajar el ritmo, las consultas se pausan 15 minutos.
- La lista de logros se descarga de Steam en el idioma que elegiste.

## Seguridad y escaneo
**Qué escanea:** solo las subcarpetas de tus bibliotecas, buscando únicamente los archivos que identifican emuladores y plataformas. No entra en carpetas de audio, vídeo, recursos ni instaladores, y oculta las carpetas que no contienen archivos de juego. Revisa varios juegos a la vez, con un tope de tamaño y de tiempo por juego.

**Candados:**
- **Bibliotecas:** no se pueden usar como biblioteca la raíz de un disco, Windows, Archivos de programa, ProgramData, Usuarios, tu perfil, AppData ni la carpeta temporal. Tampoco enlaces simbólicos. Si `config.json` las contiene, se ignoran.
- **Enlaces:** nunca sigue enlaces simbólicos ni *junctions*, ni lee archivos de OneDrive que solo están en la nube (no los descarga).
- **Escritura:** solo puede crear `steam_settings\achievements.json`, `steam_settings\steam_appid.txt`, la copia `.hydralogros-original` y `GSE Saves\<AppID>\achievements.json`. Cualquier otra ruta se bloquea.
- **Deshacer:** aunque alguien edite `cambios.json`, solo actúa sobre esos mismos archivos.
- **Red:** solo se conecta a `api.steampowered.com` por HTTPS y no sigue redirecciones, así tu clave no puede acabar en otro servidor. Solo abre dos webs de confianza.
- **Nada se ejecuta:** nunca lanza programas de las carpetas de juegos. Las carpetas se abren con el Explorador y las DLL de Windows solo se cargan desde `System32`.
- **Datos de entrada:** los AppID solo pueden ser números, los archivos de texto de más de 1 MB se ignoran y las búsquedas de texto tienen un tiempo máximo.
- **Registro:** `registro.txt` rota al llegar a 2 MB.
- **Aviso** si abres la app como administrador (no hace falta).

## Archivos de la carpeta
- `App\Laurel.exe`: la aplicación. Necesita .NET 10 Desktop Runtime.
- `App\config.json`, `cambios.json`, `estado.json` y `registro.txt`: los crea la app al usarla.
- `leeme\`: este documento en otros idiomas.
- `src\`: código fuente. Para recompilar: `dotnet publish src -c Release -o App`.
- `tools\IconGen\`: genera el icono (`src\app.ico`). Para regenerarlo: `dotnet run --project tools/IconGen -- src/app.ico`.
- `Portable\Laurel.exe`: versión portable. Lleva .NET dentro (no hay que instalar nada) y guarda sus datos en su propia carpeta. Para recompilarla: `dotnet publish src -c Release -p:Portable=true -o Portable`.
- El botón **Leeme** de la app muestra este documento en tu idioma. Usa una copia incluida dentro del `.exe`: editar estos `.md` no cambia lo que muestra la app (hay que recompilar).

## Licencia
Laurel es software libre: puedes usarlo, estudiarlo, modificarlo y compartirlo bajo los términos de la [Licencia Pública General de GNU v3.0](LICENSE) (GPL-3.0). Si distribuyes una versión modificada, también debe ser libre y con su código fuente.

*** ESTA HERRAMIENTA ES DE USO LIBRE ***
