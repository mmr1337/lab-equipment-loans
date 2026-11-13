# Разрешение конфликта Git

Ветки `feature/catalog` и `feature/loans` заменили одну и ту же строку в `api-plan.md`. Сначала `feature/catalog` была слита в `main` с `--no-ff`. Затем `git merge --no-ff feature/loans` остановился на конфликте содержимого.

В итоговом файле оставлены оба абзаца, для каталога и для выдачи. Маркеры конфликта удалены. После `git add api-plan.md` создан merge-коммит `9f13253` с двумя родителями.

[Граф веток GitHub Network](https://github.com/mmr1337/lab-equipment-loans/network)
