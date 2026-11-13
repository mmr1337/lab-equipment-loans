set -eu

cd "$(dirname "$0")/../.."
name=lab-catalog-demo
trap 'docker rm -f "$name" >/dev/null 2>&1 || true' EXIT
set -x
docker build --progress plain -t lab-catalog-demo ./services/catalog
docker run -d --name "$name" -p 127.0.0.1:8080:8080 -e Logging__LogLevel__Microsoft.AspNetCore.HttpLogging=Information lab-catalog-demo
docker ps --filter "name=$name"
curl --retry 12 --retry-connrefused --retry-delay 1 -fsS http://127.0.0.1:8080/equipment | jq -e '.[] | select(.code == "LAB-001")'
docker logs "$name"
