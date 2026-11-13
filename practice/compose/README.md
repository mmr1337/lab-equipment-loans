# Контрольная точка «Compose»

В Compose два сервиса. `api` предоставляет CRUD для оборудования, `db` запускает PostgreSQL 16. API подключается по имени `db`. `depends_on` ждёт готовности базы, а `db_data` сохраняет данные после `docker compose down`.

Из корня репозитория:

```sh
bash practice/compose/verify.sh
```

Сценарий запускает Compose на порту 8081, создаёт прибор, читает запись, обновляет название, перезапускает Compose, читает сохранённую запись и удаляет её. В конце проверяет ответ `404`. Для ручной проверки из папки `practice/compose`:

```sh
PORT=8081 docker compose up -d --build --wait
docker compose ps
curl -fsS http://127.0.0.1:8081/equipment
docker compose logs api db
docker compose down
```

[Конфигурация Compose](docker-compose.yml) · [Сценарий с CRUD-запросами](verify.sh) · [Журнал проверки](run.log)
