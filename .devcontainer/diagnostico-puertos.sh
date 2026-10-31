#!/usr/bin/env bash
# Diagnóstico del error 502 de Codespaces: arranca dos copias de prueba de la app, cada una con
# UNA diferencia respecto a la que falla (5178), para saber cuál es la causa:
#   3000 -> en segundo plano, solo IPv4 (0.0.0.0), sin configuración especial de puerto
#   3001 -> en segundo plano, IPv4+IPv6 (*),      sin configuración especial de puerto
# Abre ambos enlaces y anota cuál carga. Para apagarlas: pkill -f -- "--urls http.*:300"
set -uo pipefail

arrancar() {
  local puerto=$1 direccion=$2
  ASPNETCORE_ENVIRONMENT=Development Proxy__DetrasDeProxyHttps=true \
  setsid nohup dotnet run --project src/TorreControl.Web --no-launch-profile --no-build \
    --urls "http://$direccion:$puerto" > "/tmp/diagnostico-$puerto.log" 2>&1 < /dev/null &
  for _ in $(seq 1 60); do
    [ "$(curl -s -o /dev/null -w '%{http_code}' "http://127.0.0.1:$puerto/Cuenta/Login")" = "200" ] && return 0
    sleep 2
  done
  return 1
}

# Sin compilar: usa lo que ya compiló probar.sh (recompilar mientras la app del 5178 corre puede
# fallar en Linux porque sus archivos están en uso).

probar() {
  local puerto=$1 direccion=$2
  pkill -f -- "--urls http.*:$puerto" 2>/dev/null
  if arrancar "$puerto" "$direccion"; then
    echo "✅ Prueba $puerto arrancó ($direccion)"
  else
    echo "❌ Prueba $puerto no arrancó: tail /tmp/diagnostico-$puerto.log"
  fi
}

# Comillas en "*": sin ellas, bash lo cambiaría por la lista de archivos de la carpeta.
probar 3000 "0.0.0.0"
probar 3001 "*"

base="https://${CODESPACE_NAME:-tu-codespace}"
dominio="${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN:-app.github.dev}"
echo
echo "Abre cada enlace en una pestaña normal y dime cuáles cargan (aunque sea la pantalla de login):"
echo "  A) ${base}-3000.${dominio}/"
echo "  B) ${base}-3001.${dominio}/"
echo "  C) ${base}-5178.${dominio}/   (la de siempre)"
echo "Si GitHub pregunta si quieres abrir el puerto, acepta."
