#!/usr/bin/env bash
# Se ejecuta cada vez que arranca el Codespace: levanta la app en segundo plano.
# Para verla trabajar:  tail -f /tmp/torre-control.log
set -uo pipefail

# Si ya está corriendo (por ejemplo, al reconectarse), no se levanta otra.
if pgrep -f "[T]orreControl.Web" > /dev/null; then
  exit 0
fi

nohup dotnet run --project src/TorreControl.Web --no-launch-profile --urls "http://0.0.0.0:5178" \
  > /tmp/torre-control.log 2>&1 &

echo "La app está arrancando (tarda ~1 minuto la primera vez). Bitácora: /tmp/torre-control.log"
echo "Usuarios demo: 900001 a 900006. Contraseña: $(cat "$HOME/.torre-control/contrasena-demo" 2>/dev/null)"
