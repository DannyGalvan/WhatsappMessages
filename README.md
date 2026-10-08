# WhatsappSendMessages

API en .NET 10 para enviar plantillas de WhatsApp (Meta Cloud API) y recibir su webhook. Persiste en SQL Server via EF Core y protege sus endpoints con API keys administrables en base de datos.

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
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

El `connectionString` del sink de `MSSqlServer` es el **nombre** de la entrada en `ConnectionStrings` (no la cadena completa) - Serilog la resuelve sola desde ahi.

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

- `POST /api/v1/SendTemplateMessage` - requiere cualquier API key valida.
- `GET /api/v1/WebHookMessages` - publico (se verifica solo con el `hub.verify_token` de Meta).
- `/swagger*` - publico.
- `GET|POST|DELETE /api/v1/ApiKeys` y `PUT /api/v1/WhatsAppAccessToken` - requieren una API key con `IsAdmin = true`.

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
- **Circuit breaker** (5 fallos seguidos, abre 30s): si el API de WhatsApp esta caido, las siguientes solicitudes fallan al instante en vez de intentar conectar y acumularse.
- **`UseProxy = false`** en el `HttpClientHandler`: por default depende de la auto-deteccion de proxy de Windows (WinHTTP), que se cuelga si el servicio `WinHttpAutoProxySvc` falla en el servidor, bloqueando toda salida hacia `graph.facebook.com`. Se desactiva porque el servidor sale directo a internet sin proxy corporativo.
- El `CancellationToken` del request HTTP entrante se propaga hasta la llamada al API de WhatsApp y al `SaveChangesAsync`, para no seguir trabajando si el cliente ya se desconecto.

## Logging

Serilog se configura enteramente desde la seccion `Serilog` de `appsettings` (niveles, sinks, columnas extra) - nada queda hardcodeado en `ServicesGroup.cs`. Se llama `loggingBuilder.ClearProviders()` antes de registrar el provider de Serilog para quitar los providers default de ASP.NET Core (Console/Debug), que de lo contrario siguen imprimiendo en paralelo leyendo de `Logging:LogLevel` en vez de `Serilog:MinimumLevel`, duplicando salida e ignorando los overrides configurados (ej. bajar el ruido de EF Core a `Warning`).

El provider se registra con `dispose: true` para que el sink de `MSSqlServer` haga flush al detener la aplicacion. Esto es importante porque el `OutOfMemoryRecoveryMiddleware` puede parar el proceso en cualquier momento; sin flush explicito, los ultimos logs en lote se perderian.

## Auditoria

`MessagesTemplate`, `ApiKeys` y `WhatsAppAccessTokens` exponen cuatro columnas de auditoria que se llenan automaticamente al guardar cambios:

- `CreatedAt` / `CreatedBy` - sellados al insertar.
- `UpdatedAt` / `UpdatedBy` - se refrescan en cada update. `Created*` se protege contra escritura accidental via `IsModified = false` en el `AuditSaveChangesInterceptor`.

El actor se forma como `apikey:{id}:{name}` cuando el cambio ocurre dentro de un request autenticado, o `system` cuando viene de un inicializador/background. Las migraciones `AddAuditColumns` backfilean las filas existentes: las fechas reales no existen, asi que los registros historicos quedan con `CreatedBy = 'legacy'` y la fecha de deploy como aproximacion. Para `ApiKeys` se usa `COALESCE(RevokedAt, CreatedAt)` como `UpdatedAt` y para `WhatsAppAccessTokens` se copia `UpdatedAt` en `CreatedAt`.

## Health check

`GET /health` (publico, sin auth) responde 200 mientras la BD responda. Util para que el monitor del IIS o el balanceador detecten caidas como la del 30-sep-2026 desde fuera. Implementado via `AddHealthChecks().AddDbContextCheck<WhatsappMessagesContext>()` (paquete `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`).

## Configuracion opcional

Ademas de las secciones obligatorias, `appsettings.{Environment}.json` admite:

```json
{
  "Swagger": { "Enabled": false },
  "WhatsAppWebhook": { "VerifyToken": "el-que-meta-manda" }
}
```

- `Swagger:Enabled` (default `true`): si se pone `false`, ni `UseSwagger()` ni `UseSwaggerUI()` se registran, y `/swagger/v1/swagger.json` devuelve 404.
- `WhatsAppWebhook:VerifyToken` (default `"12345"`): el token que Meta manda en `hub.verify_token` al suscribir el webhook. Movilo a `appsettings.Production.json` para no recompilar cuando Meta pida rotarlo.

## Despliegue

`deploy_iis.sh` publica el proyecto, empaqueta el output y lo despliega via SSH a un IIS remoto (detiene el App Pool, sube y extrae el paquete, lo reinicia, verifica la URL).

```bash
cp .env.deploy.example .env.deploy
# Editar .env.deploy con los valores reales
./deploy_iis.sh
```

`.env.deploy` se carga con `source` (es bash real): si algun valor (ej. `DEPLOY_PASSWORD`) tiene caracteres especiales de shell (`( ) * ? ! @ $`), va entre comillas simples para que no rompa la sintaxis ni dispare expansion de glob. El archivo real esta en `.gitignore`; solo se versiona `.env.deploy.example`.

### Recomendaciones para IIS

El escenario del 30-sep-2026 (cache envenenado de `System.Text.Json` + `OutOfMemoryException` no reciclable por IIS) se mitiga con:

- **App Pool en 64-bit**: el default de 32-bit limita la memoria virtual a 4 GB y dispara OOM bajo carga real.
- **Private Memory Limit (KB)**: poner un techo (ej. 1.5x el consumo estable observado) para que IIS recicle el worker cuando se acerque, en vez de esperar al crash.
- **Disable overlapped recycle**: si no, dos procesos compiten por el puerto durante el recycle y se pierden requests.
- **Periodic recycle time** a una hora valle (no a las 12 AM que es cuando arrancan los jobs de Meta).
- **Limite `max server memory` de SQL Server**: si la API y la BD comparten host, fijar `max server memory` (en MB) en `sp_configure` para que SQL no le robe toda la RAM al worker de IIS.

## Estructura del proyecto

```
deploy_iis.sh                        Script de deploy manual via SSH a IIS
.env.deploy.example                  Plantilla de variables para el deploy (el real no se versiona)

WhatsappSendMessages/
  Authentication/                     Scheme de autenticacion por API key
  Configurations/
    Extensions/                       Componentes del builder (Add* por responsabilidad)
      PersistenceServiceExtensions     DbContext + interceptor de auditoria
      ApiKeyAuthenticationServiceExtensions  IMemoryCache, IApiKeyService, scheme, policy admin
      SerilogServiceExtensions        Serilog desde appsettings
      WhatsAppCloudApiServiceExtensions  libreria Meta + HttpClient + IWhatsAppCloudApiConfigProvider
      TemplateServiceExtensions        factory + recorder + sender
      StartupServiceExtensions         IStartupInitializer
      SwaggerServiceExtensions         AddSwaggerGen con toggle Enabled
      OptionsServiceExtensions         bind a IOptions<>
      ServicesGroup                    fachada que llama a todos los anteriores
      ApplicationGroup                 RunStartupInitializersAsync
    Models/
    Options/                            SwaggerOptions, WhatsAppWebhookOptions
  Context/                              DbContext + AuditSaveChangesInterceptor
    Interceptors/
  Controllers/                          Endpoints HTTP
  Entities/                             Modelos de dominio
    Auditing/                            IAuditable
    Request/                             DTOs de entrada
    Response/                            DTOs de salida
  Middleware/                            OutOfMemoryRecoveryMiddleware
  Migrations/                            Migraciones EF Core
  Services/                              ApiKeyService, WhatsAppCloudApiConfigProvider
    Auditing/                            IClock, ICurrentActorProvider
    Templates/                           factory + recorder + sender
  Startup/                               IStartupInitializer + implementaciones

WhatsappSendMessages.Tests/            xUnit + Mvc.Testing + EF.Sqlite (contract) + EF.InMemory (unit)
  Infrastructure/                       CustomWebApplicationFactory + FakeWhatsAppBusinessClient
  Contracts/                             golden-JSON de cada endpoint
  Unit/                                  AuditSaveChangesInterceptor, factory
```
