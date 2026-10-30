#!/usr/bin/env bash
# Un solo comando para usar la app en Codespaces:
#   bash .devcontainer/probar.sh
# Arranca la app si no está corriendo, espera a que responda y dice dónde abrirla.
# Si algo falla, muestra el error de la bitácora.
set -uo pipefail

PUERTO=5178
BITACORA=/tmp/torre-control.log

# Código HTTP de la página de login: 200 = la app funciona; 000 = nadie responde.
codigo() { curl -s -o /dev/null -w "%{http_code}" --max-time 5 "http://$1:$PUERTO/Cuenta/Login"; }

if [ "$(codigo 127.0.0.1)" != "200" ]; then
  echo "Deteniendo cualquier versión anterior de la app..."
  pkill -f -- "--urls http" 2>/dev/null
  sleep 2
  bash .devcontainer/iniciar.sh > /dev/null
  echo "Arrancando la app (la primera vez tarda 1-2 minutos)..."
  for _ in $(seq 1 90); do
    [ "$(codigo 127.0.0.1)" = "200" ] && break
    # Si la app ya se cayó, no tiene caso seguir esperando.
    pgrep -f -- "--urls http" > /dev/null || break
    sleep 2
  done
fi

IPV4="$(codigo 127.0.0.1)"
IPV6="$(codigo '[::1]')"
echo
if [ "$IPV4" = "200" ]; then
  echo "✅ La app está funcionando (IPv4: $IPV4, IPv6: $IPV6)."
  if [ -n "${CODESPACE_NAME:-}" ]; then
    echo "   Ábrela en una pestaña NORMAL del navegador (no en el Explorador simple):"
    echo "   https://${CODESPACE_NAME}-${PUERTO}.${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN:-app.github.dev}/"
  fi
  echo "   Usuario: 900005   Contraseña: $(cat "$HOME/.torre-control/contrasena-demo" 2>/dev/null)"
  echo "   La app sigue corriendo aunque cierres esta terminal."
else
  echo "❌ La app no responde (IPv4: $IPV4, IPv6: $IPV6). Últimas líneas de la bitácora:"
  echo "------------------------------------------------------------"
  grep -vE "^\s+(SELECT|FROM|WHERE|WHEN|ELSE|END|INSERT|VALUES|OUTPUT|SET|MERGE|INNER|LEFT|ORDER|\[|\(|,)" "$BITACORA" 2>/dev/null | tail -n 25
  echo "------------------------------------------------------------"
  echo "Manda una captura de este mensaje."
fi
