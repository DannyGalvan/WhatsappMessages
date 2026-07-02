#!/usr/bin/env bash
set -euo pipefail

# ─── Colors ───────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; NC='\033[0m'
log()   { echo -e "${GREEN}▶${NC} $1"; }
info()  { echo -e "  ${CYAN}$1${NC}"; }
warn()  { echo -e "${YELLOW}⚠${NC}  $1"; }
error() { echo -e "${RED}✖${NC}  $1"; exit 1; }
ok()    { echo -e "${GREEN}✔${NC}  $1"; }

# ─── Load .env.deploy ─────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="${1:-$SCRIPT_DIR/.env.deploy}"
[[ -f "$ENV_FILE" ]] || error "No se encontró '$ENV_FILE'.\n   Ejecuta: cp .env.deploy.example .env.deploy"
# shellcheck source=/dev/null
source "$ENV_FILE"

# ─── Validate required vars ───────────────────────────────────────────────────
MISSING=""
for var in DEPLOY_SERVER DEPLOY_SSH_PORT DEPLOY_SITE DEPLOY_PATH \
           DEPLOY_USER DEPLOY_PASSWORD DEPLOY_PROJECT; do
  [[ -n "${!var:-}" ]] || MISSING="$MISSING $var"
done
[[ -z "$MISSING" ]] || error "Variables faltantes en $ENV_FILE:$MISSING"

# ─── Check dependencies ───────────────────────────────────────────────────────
command -v dotnet  >/dev/null 2>&1 || error "dotnet no instalado"
command -v node    >/dev/null 2>&1 || error "node no instalado"
command -v sshpass >/dev/null 2>&1 || error "sshpass no instalado.\n   Ejecuta: brew install hudochenkov/sshpass/sshpass"

# ─── SSH helpers ──────────────────────────────────────────────────────────────
SSH_OPTS="-o StrictHostKeyChecking=no -o LogLevel=ERROR -p $DEPLOY_SSH_PORT"
SCP_OPTS="-o StrictHostKeyChecking=no -o LogLevel=ERROR -P $DEPLOY_SSH_PORT"

run_ssh() {
  sshpass -p "$DEPLOY_PASSWORD" ssh $SSH_OPTS "$DEPLOY_USER@$DEPLOY_SERVER" "$1"
}

run_ps() {
  run_ssh "powershell -NoProfile -NonInteractive -Command \"$1\""
}

PUBLISH_DIR="$SCRIPT_DIR/publish"
TAR_FILE="/tmp/deploy-package.tar.gz"
REMOTE_TEMP="C:/temp/deploy-package.tar.gz"

echo ""
echo -e "${CYAN}════════════════════════════════════════${NC}"
echo -e "${CYAN}  Deploy → $DEPLOY_USER@$DEPLOY_SERVER${NC}"
echo -e "${CYAN}  App Pool: $DEPLOY_SITE${NC}"
echo -e "${CYAN}════════════════════════════════════════${NC}"
echo ""

# ─── Step 1: Test SSH ─────────────────────────────────────────────────────────
log "Verificando conexión SSH..."
run_ssh "echo ok" >/dev/null 2>&1 || error "No se puede conectar a $DEPLOY_SERVER:$DEPLOY_SSH_PORT. Verifica credenciales y OpenSSH."
ok "Conexión SSH establecida"

# ─── Step 2: Frontend deps ────────────────────────────────────────────────────
if [[ -n "${DEPLOY_CLIENT_DIR:-}" ]]; then
  log "Instalando dependencias frontend..."
  npm ci --prefix "$SCRIPT_DIR/$DEPLOY_CLIENT_DIR" --silent
  ok "npm ci completado"
else
  info "DEPLOY_CLIENT_DIR no definido — omitiendo frontend"
fi

# ─── Step 3: dotnet publish ───────────────────────────────────────────────────
log "Publicando aplicación .NET..."
rm -rf "$PUBLISH_DIR"
dotnet publish "$SCRIPT_DIR/$DEPLOY_PROJECT" \
  -c Release \
  -o "$PUBLISH_DIR" \
  /p:EnvironmentName=Production \
  --nologo \
  -v quiet
find "$PUBLISH_DIR" -name "*.pdb" -delete
FILE_COUNT=$(find "$PUBLISH_DIR" -type f | wc -l | tr -d ' ')
ok "Build completado ($FILE_COUNT archivos)"

# ─── Step 4: Remove preserved files from local publish ────────────────────────
if [[ -n "${DEPLOY_PRESERVE:-}" ]]; then
  IFS=',' read -ra PRESERVE_LIST <<< "$DEPLOY_PRESERVE"
  for item in "${PRESERVE_LIST[@]}"; do
    item="$(echo "$item" | tr -d ' ')"
    [[ -z "$item" ]] && continue
    [[ -f "$PUBLISH_DIR/$item" ]] && { rm -f "$PUBLISH_DIR/$item"; info "Preservando en servidor: $item"; }
    [[ -d "$PUBLISH_DIR/$item" ]] && { rm -rf "$PUBLISH_DIR/$item"; info "Preservando en servidor: $item/"; }
  done
fi

# ─── Step 5: Package ──────────────────────────────────────────────────────────
log "Comprimiendo paquete..."
# COPYFILE_DISABLE evita que el tar de macOS genere archivos AppleDouble
# (._nombre) por cada archivo con atributos extendidos; sin esto terminan
# empaquetados junto al build real y aparecen sueltos en el servidor.
COPYFILE_DISABLE=1 tar -czf "$TAR_FILE" --exclude='.DS_Store' --exclude='._*' -C "$PUBLISH_DIR" .
TAR_SIZE=$(du -sh "$TAR_FILE" | cut -f1)
ok "Paquete listo ($TAR_SIZE)"

# ─── Step 6: Stop App Pool ────────────────────────────────────────────────────
log "Deteniendo App Pool '$DEPLOY_SITE'..."
run_ps "Import-Module WebAdministration; Stop-WebAppPool -Name '$DEPLOY_SITE' -ErrorAction SilentlyContinue; Start-Sleep 3" 2>/dev/null || true
ok "App Pool detenido"

# ─── Step 7: Upload ───────────────────────────────────────────────────────────
log "Subiendo archivos al servidor..."
run_ps "if (-not (Test-Path 'C:\\temp')) { New-Item -ItemType Directory -Path 'C:\\temp' -Force | Out-Null }" 2>/dev/null || true
sshpass -p "$DEPLOY_PASSWORD" scp $SCP_OPTS "$TAR_FILE" "$DEPLOY_USER@$DEPLOY_SERVER:$REMOTE_TEMP"
ok "Paquete subido"

# ─── Step 8: Extract on server ────────────────────────────────────────────────
log "Extrayendo en el servidor..."

# Build PowerShell preserve list for Where-Object filter
PS_PRESERVE_LIST=""
if [[ -n "${DEPLOY_PRESERVE:-}" ]]; then
  IFS=',' read -ra PRESERVE_LIST <<< "$DEPLOY_PRESERVE"
  for item in "${PRESERVE_LIST[@]}"; do
    item="$(echo "$item" | tr -d ' ')"
    [[ -z "$item" ]] && continue
    PS_PRESERVE_LIST="\$_.Name -ne '$item' -and $PS_PRESERVE_LIST"
  done
  # Remove trailing " -and "
  PS_PRESERVE_LIST="${PS_PRESERVE_LIST%" -and "}"
fi

if [[ -n "$PS_PRESERVE_LIST" ]]; then
  CLEAN_CMD="Get-ChildItem -Path '$DEPLOY_PATH' -ErrorAction SilentlyContinue | Where-Object { $PS_PRESERVE_LIST } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue"
else
  CLEAN_CMD="Get-ChildItem -Path '$DEPLOY_PATH' -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue"
fi

run_ps "
  $CLEAN_CMD
  tar -xzf '$REMOTE_TEMP' -C '$DEPLOY_PATH'
  Remove-Item '$REMOTE_TEMP' -Force -ErrorAction SilentlyContinue
  Write-Host 'OK'
" 2>/dev/null
ok "Archivos desplegados en $DEPLOY_PATH"

# ─── Step 9: Start App Pool ───────────────────────────────────────────────────
log "Iniciando App Pool '$DEPLOY_SITE'..."
run_ps "Import-Module WebAdministration; Start-WebAppPool -Name '$DEPLOY_SITE'" 2>/dev/null
ok "App Pool iniciado"

# ─── Cleanup ──────────────────────────────────────────────────────────────────
rm -rf "$PUBLISH_DIR"
rm -f "$TAR_FILE"

# ─── Step 10: Verify URL (optional) ───────────────────────────────────────────
if [[ -n "${DEPLOY_URL:-}" ]]; then
  log "Verificando URL: $DEPLOY_URL"
  sleep 3
  HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 "$DEPLOY_URL" 2>/dev/null || echo "000")
  if [[ "$HTTP_CODE" =~ ^(200|301|302)$ ]]; then
    ok "Sitio responde con HTTP $HTTP_CODE"
  else
    warn "Sitio respondió con HTTP $HTTP_CODE — verifica manualmente"
  fi
fi

echo ""
echo -e "${GREEN}════════════════════════════════════════${NC}"
echo -e "${GREEN}  Deploy completado exitosamente ✔${NC}"
[[ -n "${DEPLOY_URL:-}" ]] && echo -e "${GREEN}  $DEPLOY_URL${NC}"
echo -e "${GREEN}════════════════════════════════════════${NC}"
echo ""
