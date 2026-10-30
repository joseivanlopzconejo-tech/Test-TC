#!/usr/bin/env bash
# Se ejecuta una vez al crear el Codespace: restaura herramientas y guarda los secretos de
# desarrollo con "dotnet user-secrets" (fuera del repositorio).
set -euo pipefail

PROYECTO="src/TorreControl.Web"
CARPETA_DEMO="$HOME/.torre-control"

echo "Esperando la contraseña de SQL Server..."
for _ in $(seq 1 60); do
  [ -s /secretos/sa ] && break
  sleep 2
done
SA="$(cat /secretos/sa)"

dotnet tool restore
dotnet restore

dotnet user-secrets set "ConnectionStrings:TorreControl" \
  "Server=db,1433;Database=TorreControl;User Id=sa;Password=${SA};TrustServerCertificate=True" \
  --project "$PROYECTO" > /dev/null

# Contraseña de los usuarios demo: al azar, guardada fuera del repositorio.
mkdir -p "$CARPETA_DEMO"
if [ ! -s "$CARPETA_DEMO/contrasena-demo" ]; then
  echo "Demo$(head -c 6 /dev/urandom | od -An -tx1 | tr -d ' \n')Aa9" > "$CARPETA_DEMO/contrasena-demo"
  chmod 600 "$CARPETA_DEMO/contrasena-demo"
fi
dotnet user-secrets set "DatosEjemplo:Contrasena" "$(cat "$CARPETA_DEMO/contrasena-demo")" \
  --project "$PROYECTO" > /dev/null

# Las pruebas de base de datos usan el mismo SQL Server (crean y borran bases temporales).
echo "export TORRECONTROL_PRUEBAS_SQL='Server=db,1433;User Id=sa;Password=${SA};TrustServerCertificate=True'" \
  > "$CARPETA_DEMO/pruebas.sh"
chmod 600 "$CARPETA_DEMO/pruebas.sh"
grep -q "torre-control/pruebas.sh" "$HOME/.bashrc" || echo 'source "$HOME/.torre-control/pruebas.sh"' >> "$HOME/.bashrc"

echo "Listo. La contraseña de los usuarios demo está en $CARPETA_DEMO/contrasena-demo"
