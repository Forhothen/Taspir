# Taspir

Unity 6000.3.27f2, Universal Render Pipeline 17.3.0.

## Запуск

1. Открой в Unity Hub корень репозитория.
2. Открой `Assets/LobbyBalcony/Scenes/LobbyBalcony.unity`.
3. Нажми Play. Для исходной композиции выбери 4:3.

Лобби включено в общий список сцен сборки и Web-профиль.
Для браузера используй File → Build Profiles → Web → Build And Run.

## Структура

`Assets/LobbyBalcony` — самостоятельный модуль лобби: сцена, скрипты,
редакторские инструменты, материалы, шейдеры, меши и изображения.
Подробности настройки находятся в `Assets/LobbyBalcony/README.md`.

Новые мини-приложения размещай в отдельных папках `Assets/<ИмяМодуля>`
со своей папкой `Scenes` и локальными ресурсами. Сцены открываются независимо;
общий контекст игрока будет добавлен позже.

`Assets/Settings` содержит общие настройки рендеринга и профили сборки.
SampleScene, FirstPersonRoom и их отдельные ресурсы удалены.

## Проверка лобби

```sh
Unity -batchmode -projectPath /path/to/Taspir \
  -executeMethod LobbyBalconySceneBuilder.Validate -quit -logFile /tmp/lobby.log
```

Перед пакетной проверкой закрой этот проект в редакторе Unity.
Метод проверяет сохранённую сцену без её пересоздания и сохраняет QA-кадры
в системную временную папку `TaspirLobbyQA`.
