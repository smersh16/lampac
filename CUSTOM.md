# Свои правки поверх lampac-nextgen

Ветка `main` зеркалит апстрим (sync-upstream.yml), свои правки живут здесь, в ветке `custom`, и в отдельных репозиториях-модулях.

## Правки в этой ветке (файлы апстрима)

Все файлы на сервере смонтированы поверх контейнера (`/lampac/module/Sync/...`, `/lampac/module/Online/plugin.js`).

| Файл | Что изменено | md5 (на 2026-11-15) |
| --- | --- | --- |
| `Modules/Sync/Sync/plugins/bookmark.js` | сверка всегда полной выгрузкой `/dump` (дельты `/changelog` не сохраняли порядок истории, на устройствах она расходилась) | `49da6a9a55fd322217bb9cd24f69f88a` |
| `Modules/Sync/Sync/plugins/sync_v2/sync.js` | выгрузка `sync_view` и при изменении `online_watched_last` | `a096a59ea4dba5d7309f9bd970b87934` |
| `Modules/Sync/TimeCode/plugin.js` | `update()` на списках без открытой карточки вызывает `updateAll()` (дамп отметок); арбитраж по `changed_at`, локальный прогресс не откатывается | `0557411ceb61de37b07c702bb73368b2` |
| `Online/plugin.js` | кнопка «Продолжить» на карточке: запуск последней (или следующей при просмотре ≥ 95%) серии тем же балансером и озвучкой из `online_choice_*`; при расхождении только открывает список | `09a857d14eaa86351c69d3d7bdc18d5c` (2026-11-22) |

## Правки в отдельных репозиториях (подключены в `module/repository.yaml`, ветка `main`)

| Репозиторий | Модули | Назначение |
| --- | --- | --- |
| `smersh16/lampac-adminpanel-plus` | `AdminPanelPlus` | улучшенная админка (не включать вместе с `Modules/AdminPanel`: маршруты `/adminpanel` те же) |
| `smersh16/lampac-aniskip` | `AniSkip` | метки заставок/титров (AniSkip для аниме, TheIntroDB для сериалов) |
| `smersh16/lampac-tizen-mods` | `mods/PhantomTs`, `mods/MirageTs` | правки Phantom/Mirage для Samsung Tizen |

## Файлы конфигурации на сервере (не в репозитории)

`init.conf`, `passwd`, `users.json`, `plugins/lampainit.js` (→ `plugins/override/lampainit.js`), `repository.yaml`, том `database`.

## После обновления образа или rebase

1. Проверить, менял ли апстрим патченные файлы:
   ```bash
   git diff <старая-база> origin/main -- Modules/Sync/TimeCode/plugin.js Modules/Sync/Sync/plugins/
   ```
   Пусто: правки применимы как есть. Не пусто: перенести правки в новую версию файла и обновить md5 в таблице.
2. Сверить md5 файлов на сервере с таблицей выше:
   ```bash
   cd /data/compose/14/lampac-docker/module/Sync
   md5sum TimeCode/plugin.js Sync/plugins/bookmark.js Sync/plugins/sync_v2/sync.js
   ```
3. Обновление ветки:
   ```bash
   git fetch origin
   git branch backup/custom-$(date +%F) custom
   git rebase origin/main
   git push --force-with-lease origin custom
   ```
   Откат: `git reset --hard backup/custom-<дата>` и повторный push с `--force-with-lease`.
