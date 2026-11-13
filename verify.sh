set -eu

cd "$(dirname "$0")"
base="http://127.0.0.1:${PORT:-8080}"
set -x
docker compose up -d --build --wait
docker compose ps
curl --retry 12 --retry-all-errors --retry-delay 1 -fsS "$base/health"
code="LAB-DEMO-$(date +%s)"
equipment=$(curl -fsS -H 'Content-Type: application/json' -d "{\"code\":\"$code\",\"name\":\"Мультиметр\"}" "$base/equipment")
printf '%s\n' "$equipment" | jq -e --arg code "$code" 'select(.code == $code)'
equipment_id=$(printf '%s\n' "$equipment" | jq -er .id)
request="{\"equipmentId\":$equipment_id,\"borrower\":\"Тестовый студент\"}"
loan=$(curl -fsS -H 'Content-Type: application/json' -d "$request" "$base/loans")
printf '%s\n' "$loan" | jq -e 'select(.returnedAt == null)'
loan_id=$(printf '%s\n' "$loan" | jq -er .id)
test "$(curl -sS -o /dev/null -w '%{http_code}' -H 'Content-Type: application/json' -d "$request" "$base/loans")" = 409
curl -fsS -X POST "$base/loans/$loan_id/return" | jq -e 'select(.returnedAt != null)'
report=$(curl -fsS -X POST "$base/reports")
printf '%s\n' "$report" | jq -e 'select(.returned >= 1 and .total == (.active + .returned))'
report_id=$(printf '%s\n' "$report" | jq -er .id)
docker compose down
docker compose up -d --wait
docker compose ps
saved=$(curl --retry 12 --retry-all-errors --retry-delay 1 -fsS "$base/loans/$loan_id")
printf '%s\n' "$saved" | jq -e --argjson id "$equipment_id" 'select(.equipmentId == $id and .returnedAt != null)'
test "$(curl -fsS "$base/reports/$report_id" | jq -cS .)" = "$(printf '%s\n' "$report" | jq -cS .)"
docker compose logs --no-color --tail 16 gateway catalog loans reports db
