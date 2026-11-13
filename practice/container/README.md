# Контрольная точка «Контейнер»

Catalog API возвращает список учебного оборудования. В этой практике он работает с примером `LAB-001`, поэтому для запуска одного контейнера PostgreSQL не нужна. Исходники и написанный вручную Dockerfile находятся в [services/catalog](../../services/catalog).

## Запуск без Docker

Из корня проекта:

```sh
dotnet run --project services/catalog --urls http://127.0.0.1:5080
curl -fsS http://127.0.0.1:5080/equipment
```

## Запуск контейнера

```sh
docker build -t lab-catalog-demo ./services/catalog
docker run -d --name lab-catalog-demo -p 127.0.0.1:8080:8080 lab-catalog-demo
docker ps --filter name=lab-catalog-demo
curl -fsS http://127.0.0.1:8080/equipment
docker logs lab-catalog-demo
docker stop lab-catalog-demo
docker rm lab-catalog-demo
```

`Dockerfile` сначала собирает приложение в образе SDK, затем копирует результат в образ ASP.NET Core. `ENTRYPOINT` запускает `Catalog.dll`, `-p` связывает порт на машине с портом приложения в контейнере.

[Журнал сборки, запуска и запроса](run.log) · [Локальная проверка без Docker](local-run.log) · [Сценарий проверки](verify.sh)
