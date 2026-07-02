# WhatsappSendMessages

API en .NET 8 para enviar plantillas de WhatsApp (Meta Cloud API) y recibir su webhook. Persiste en SQL Server via EF Core y protege sus endpoints con API keys administrables en base de datos.

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server accesible (connection string en `appsettings.{Environment}.json`)
- Cuenta de WhatsApp Business Cloud API (Meta) con Phone Number ID, Business Account ID y un Access Token
- Herramienta `dotnet-ef` (ya versionada en `.config/dotnet-tools.json`, se restaura con `dotnet tool restore`)
- Para desplegar (`deploy_iis.sh`): `sshpass`, `node`/`npm` y acceso SSH a un Windows Server con IIS

## Configuracion inicial

Los archivos `appsettings.Development.json` y `appsettings.Production.json` estan en `.gitignore` (contienen secretos) y no vienen en el repo. Hay que crearlos manualmente dentro de `WhatsappSendMessages/` con esta forma:

```json
{
  "ConnectionStrings": {
    "WhatsAppMessages": "server=TU_SERVIDOR; Database=WhatsAppMessages; User Id=TU_USUARIO; Password=TU_PASSWORD; Trust Server Certificate=true"
  },
  "WhatsAppBusinessCloudApiConfiguration": {
    "WhatsAppBusinessPhoneNumberId": "",
    "WhatsAppBusinessAccountId": "",
    "WhatsAppBusinessId": "",
    "AccessToken": "",
    "AppName": "",
    "Version": "v22.0"
  },
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.MSSqlServer" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.EntityFrameworkCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "MSSqlServer",
        "Args": {
          "connectionString": "WhatsAppMessages",
          "tableName": "Logs",
          "autoCreateSqlTable": true,
          "restrictedToMinimumLevel": "Information",
          "columnOptionsSection": {
            "additionalColumns": [
              { "columnName": "RequestId", "dataType": "nvarchar", "dataLength": 50 }
            ]
          }
        }
      }
    ],
    "Enrich": [ "FromLogContext" ]
  }
}
```

El `AccessToken` de arriba solo se usa una vez: al primer arranque, si la tabla `WhatsAppAccessTokens` esta vacia, la app lo copia a base de datos automaticamente (ver [Rotacion de credenciales](#rotacion-de-credenciales)). De ahi en adelante se ignora y se puede borrar del archivo.

El `connectionString` del sink de `MSSqlServer` es el **nombre** de la entrada en `ConnectionStrings` (no la cadena completa) — Serilog la resuelve sola desde ahi.

## Base de datos

```bash
cd WhatsappSendMessages
dotnet tool restore
dotnet ef database update
```

Esto crea el esquema completo (`MessagesTemplate`, `ApiKeys`, `WhatsAppAccessTokens`, `Logs`) contra la connection string configurada.

## Ejecutar

```bash
cd WhatsappSendMessages
dotnet run
```

Por defecto levanta en `http://localhost:5238` (perfil `http`, ver `Properties/launchSettings.json`) con `ASPNETCORE_ENVIRONMENT=Development`. Swagger UI queda en `/swagger`.

## Autenticacion

Los endpoints de negocio requieren el header `X-API-KEY`. El modelo es opt-in por controlador/accion via `[Authorize(AuthenticationSchemes = "ApiKey")]` (igual que el `Authorize` nativo de .NET): sin el atributo, el endpoint queda publico.

- `POST /api/v1/SendTemplateMessage` — requiere cualquier API key valida.
- `GET /api/v1/WebHookMessages` — publico (se verifica solo con el `hub.verify_token` de Meta).
- `/swagger*` — publico.
- `GET|POST|DELETE /api/v1/ApiKeys` y `PUT /api/v1/WhatsAppAccessToken` — requieren una API key con `IsAdmin = true`.

### Primer arranque

Si no existe ninguna API key admin activa en `ApiKeys`, la app genera una automaticamente y la imprime **una sola vez** en el log al iniciar:

```
No habia ninguna API key admin activa. Se genero una nueva (id 1): <la key aqui>. Guardela ahora, no se volvera a mostrar.
```

Guardala: es la unica forma de gestionar el resto de las keys.

### Gestionar API keys

Con una key admin en el header `X-API-KEY`:

```bash
# Crear una key para un cliente
curl -X POST http://localhost:5238/api/v1/ApiKeys \
  -H "X-API-KEY: <admin-key>" -H "Content-Type: application/json" \
  -d '{"name":"cliente-x","isAdmin":false,"expiresAt":null}'

# Listar keys (no expone el valor real, solo metadata)
curl http://localhost:5238/api/v1/ApiKeys -H "X-API-KEY: <admin-key>"

# Revocar una key comprometida (efecto inmediato, sin redeploy)
curl -X DELETE http://localhost:5238/api/v1/ApiKeys/{id} -H "X-API-KEY: <admin-key>"
```

## Rotacion de credenciales

El `AccessToken` de WhatsApp vive en la tabla `WhatsAppAccessTokens` (no en config), porque Meta lo puede expirar o revocar. Se rota sin redeploy:

```bash
curl -X PUT http://localhost:5238/api/v1/WhatsAppAccessToken \
  -H "X-API-KEY: <admin-key>" -H "Content-Type: application/json" \
  -d '{"accessToken":"<nuevo-token>"}'
```

## Resiliencia del cliente HTTP hacia WhatsApp

El `HttpClient` tipado que usa `WhatsappBusiness.CloudApi` se reconfigura en `Configurations/Extensions/ServicesGroup.cs` para que una caida del API de Meta no deje solicitudes colgadas indefinidamente:

- **Timeout de 30s** por request (la libreria trae 10 minutos por default).
- **Timeout de Polly de 20s** por intento, para cortar antes de llegar al techo del `HttpClient`.
- **Circuit breaker** (5 fallos seguidos → abre 30s): si el API de WhatsApp esta caido, las siguientes solicitudes fallan al instante en vez de intentar conectar y acumularse.
- **`UseProxy = false`** en el `HttpClientHandler`: por default depende de la auto-deteccion de proxy de Windows (WinHTTP), que se cuelga si el servicio `WinHttpAutoProxySvc` falla en el servidor, bloqueando toda salida hacia `graph.facebook.com`. Se desactiva porque el servidor sale directo a internet sin proxy corporativo.
- El `CancellationToken` del request HTTP entrante se propaga hasta la llamada al API de WhatsApp y al `SaveChangesAsync`, para no seguir trabajando si el cliente ya se desconecto.

## Logging

Serilog se configura enteramente desde la seccion `Serilog` de `appsettings` (niveles, sinks, columnas extra) — nada queda hardcodeado en `ServicesGroup.cs`. Se llama `loggingBuilder.ClearProviders()` antes de registrar el provider de Serilog para quitar los providers default de ASP.NET Core (Console/Debug), que de lo contrario siguen imprimiendo en paralelo leyendo de `Logging:LogLevel` en vez de `Serilog:MinimumLevel`, duplicando salida e ignorando los overrides configurados (ej. bajar el ruido de EF Core a `Warning`).

## Despliegue

`deploy_iis.sh` publica el proyecto, empaqueta el output y lo despliega via SSH a un IIS remoto (detiene el App Pool, sube y extrae el paquete, lo reinicia, verifica la URL).

```bash
cp .env.deploy.example .env.deploy
# Editar .env.deploy con los valores reales
./deploy_iis.sh
```

`.env.deploy` se carga con `source` (es bash real): si algun valor (ej. `DEPLOY_PASSWORD`) tiene caracteres especiales de shell (`( ) * ? ! @ $`), va entre comillas simples para que no rompa la sintaxis ni dispare expansion de glob. El archivo real esta en `.gitignore`; solo se versiona `.env.deploy.example`.

## Estructura del proyecto

```
deploy_iis.sh                        Script de deploy manual via SSH a IIS
.env.deploy.example                  Plantilla de variables para el deploy (el real no se versiona)

WhatsappSendMessages/
  Authentication/     Scheme de autenticacion por API key (AuthenticationHandler)
  Configurations/      Extensiones de arranque (ConfigGroup, ServicesGroup, ApplicationGroup)
  Context/              DbContext + configuraciones EF por entidad (Context/Configurations)
  Controllers/           Endpoints (SendTemplateMessage, WebHookMessages, ApiKeys, WhatsAppAccessToken)
  Entities/               Modelos de dominio, request y response
  Migrations/            Migraciones EF Core
  Services/               ApiKeyService, WhatsAppCloudApiConfigProvider
```
