# Учёт выдачи оборудования в учебной лаборатории

Система помогает лаборанту вести каталог приборов и регистрировать выдачу и возврат. Основной сценарий — добавить прибор в каталог, выдать его студенту и отметить возврат.

Колосов Иван Максимович, группа 3П3.24 (.NET).

## Запуск

Нужны Docker Engine или Docker Desktop с Compose, `curl` и `jq` для проверки. Команды рассчитаны на Bash. В Linux, где Docker доступен через `sudo`, запустите проверку через `sudo bash verify.sh`.

```sh
docker compose up -d --build --wait
docker compose ps
curl -fsS http://127.0.0.1:8080/health
```

Gateway слушает `127.0.0.1:8080`. Для локального запуска параметры задаются через `PORT`, `LISTEN_HOST` и `POSTGRES_PASSWORD`. Значение пароля по умолчанию предназначено для учебного запуска. Чтобы остановить контейнеры и сохранить данные, используйте `docker compose down`.

## Сервисы

| Сервис | API | Ответственность |
|---|---|---|
| Catalog API | `GET /equipment`, `GET /equipment/{id}`, `POST /equipment`, `PUT /equipment/{id}`, `DELETE /equipment/{id}` | Каталог приборов в PostgreSQL. |
| Loans API | `GET /loans`, `GET /loans/{id}`, `POST /loans`, `POST /loans/{id}/return` | Перед выдачей проверяет прибор через Catalog API. Хранит журнал выдач и возвратов в PostgreSQL. |
| Reports API | `POST /reports`, `GET /reports/{id}` | Читает журнал через Loans API, сохраняет сводку JSON в `report_data`. |
| API Gateway | `/equipment`, `/loans`, `/reports`, `/health` | Nginx принимает запросы клиента и маршрутизирует их. |

Стек — .NET 10, ASP.NET Core, Npgsql, PostgreSQL 16, Nginx и Docker Compose. Compose поднимает отдельный `db`, а том `db_data` сохраняет таблицы `equipment` и `loans`. Структура создаётся SQL-файлом при первом запуске на пустом томе. API и база общаются во внутренней сети.

## Сценарий проверки

```sh
bash verify.sh
```

Сценарий создаёт прибор, выдаёт его, проверяет ответ `409` при повторной выдаче, регистрирует возврат и сохраняет сводку. Затем он выполняет `docker compose down` и `up` и проверяет, что запись о возврате и сводка доступны по тем же идентификаторам. Команды и ответы записаны в [журнале запуска](docs/final-run.log).

## Материалы по контрольным точкам

- [Git: ветки и разрешение конфликта](git-conflict.md)
- [Linux: отчёт с доказательствами запуска](practice/linux/linux-report.pdf)
- [Один контейнер с Catalog API](practice/container)
- [Docker Compose: Catalog API и PostgreSQL](practice/compose)
- [Архитектура C4: описание, исходник и PDF](docs/architecture.md)
