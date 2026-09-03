# ArkKeeper

![Windows](https://img.shields.io/badge/Windows-0078D6?style=flat&logo=windows&logoColor=white)
![Linux-ready](https://img.shields.io/badge/Linux--ready-FCC624?style=flat&logo=linux&logoColor=black)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?style=flat&logo=dotnet&logoColor=white)
![Avalonia](https://img.shields.io/badge/UI-Avalonia-6D2BA1?style=flat)
![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)
![Status](https://img.shields.io/badge/status-en%20desarrollo-orange)

ArkKeeper es una **modernización y optimización** de [ARK Server Manager](https://arkservermanager.freeforums.net/), la herramienta clásica en WPF/.NET Framework para administrar servidores dedicados de *ARK: Survival Evolved*. No es un proyecto original desde cero: es una reescritura del mismo código base — original de [ChronosWS/ARK-Dedicated-Server-Tool](https://github.com/ChronosWS/ARK-Dedicated-Server-Tool) — sobre .NET 10 y Avalonia, con una interfaz moderna estilo Windows 11 (Mica, esquinas redondeadas, tema claro/oscuro, acento de color) y con soporte multiplataforma habilitado desde el diseño.

> **Aviso legal:** ArkKeeper y sus autores no están afiliados con Studio Wildcard ni sus socios. *ARK: Survival Evolved™* y sus imágenes, marcas y derechos relacionados son propiedad exclusiva de Studio Wildcard y/o sus afiliados. Herramienta gratuita para uso legal.

## Qué hace (en construcción)

ArkKeeper reimplementa, con paridad progresiva, las funciones del proyecto original:

- **Gestión de servidores**: perfiles múltiples, configuración global y por servidor, arranque/parada/reinicio.
- **Consola RCON**: envío de comandos y monitoreo en vivo.
- **Jugadores y tribus**: listados, perfiles, gestión de baneos.
- **Mods**: integración con Steam Workshop.
- **Discord**: notificaciones y comandos desde el servidor.
- **Backups**: guardado y restauración de mundos.
- **Auto-actualización** del servidor y de la propia herramienta.

El estado real de avance se refleja en los commits y en los issues de este repositorio — no en esta lista, que describe el objetivo final.

## Screenshots

_(Se agregan cuando la Fase 2 del roadmap tenga la primera UI navegable — ver [Issues](../../issues))_

## Instalación

_(Pendiente hasta la primera release — por ahora el proyecto solo se compila desde código fuente, ver abajo)_

## Build from Source

Requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

```bash
git clone https://github.com/Muurrcc/ArkKeeper.git
cd ArkKeeper
dotnet build
dotnet run --project src/ArkKeeper.App
```

## Stack tecnológico

| Capa | Tecnología |
|---|---|
| UI | [Avalonia UI](https://avaloniaui.net/) + [FluentAvalonia](https://github.com/amwx/FluentAvalonia) |
| MVVM | [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) |
| DI / Hosting | Microsoft.Extensions.Hosting |
| Runtime | .NET 10 |

## Créditos

Basado en el trabajo original de [ChronosWS](https://github.com/ChronosWS) y la comunidad de [ARK Server Manager](https://arkservermanager.freeforums.net/), publicado bajo GPL-3.0.

## Licencia

[GPL-3.0](LICENSE) — al ser un derivado de un proyecto GPL-3.0, ArkKeeper se distribuye bajo los mismos términos: código fuente siempre disponible, y cualquier fork o modificación debe mantenerse abierto bajo esta misma licencia.
