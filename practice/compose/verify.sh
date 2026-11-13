set -eu

cd "$(dirname "$0")"
export PORT=${PORT:-8081}
base="http://127.0.0.1:$PORT"
set -x
docker compose up -d --build --wait
docker compose ps
curl --retry 12 --retry-connrefused --retry-delay 1 -fsS "$base/health" | jq -e '.service == "catalog"'
code="KT-COMPOSE-$(date +%s)"
created=$(curl -fsS -H 'Content-Type: application/json' -d "{\"code\":\"$code\",\"name\":\"Термометр\"}" "$base/equipment")
id=$(jq -er '.id' <<EOF
$created
EOF
)
curl -fsS "$base/equipment/$id" | jq -e --arg code "$code" 'select(.code == $code)'
curl -fsS -X PUT -H 'Content-Type: application/json' -d "{\"code\":\"$code\",\"name\":\"Цифровой термометр\"}" "$base/equipment/$id" | jq -e 'select(.name == "Цифровой термометр")'
docker compose down
docker compose up -d --wait
docker compose ps
curl --retry 12 --retry-connrefused --retry-delay 1 -fsS "$base/equipment/$id" | jq -e 'select(.name == "Цифровой термометр")'
test "$(curl -sS -o /dev/null -w '%{http_code}' -X DELETE "$base/equipment/$id")" = 204
test "$(curl -sS -o /dev/null -w '%{http_code}' "$base/equipment/$id")" = 404
docker compose logs --no-color --tail 24 api db
docker compose down
