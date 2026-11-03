# Taspir

Unity 6000.3.27f2, Universal Render Pipeline 17.3.0.

## Веб-приложение

`web/` — отдельное React-приложение на Vite. `/` — ввод пароля,
`/play` — Unity внутри iframe. Временный пароль: `medok` (Enter или «Войти»).
Вход сохраняется в sessionStorage текущей вкладки; «Выйти» сбрасывает его.
Это клиентская проверка для прототипа, а не защита аккаунтов или файлов:
пароль виден в JavaScript, `/unity/index.html` доступен напрямую.
Для запуска нужен Node.js 22.12+:

```sh
cd web
npm install
npm run dev
```

`npm run build` создаёт статическую сборку в `web/dist/`.
Vite автоматически копирует `web/public/` в сборку, включая Unity.

### Экспорт Unity для сайта

1. File → Build Profiles → выбери Web-профиль и при необходимости Switch Profile.
   Web Build Support должен быть установлен в Unity Hub для 6000.3.27f2.
2. В списке сцен должна быть включена `Assets/LobbyBalcony/Scenes/LobbyBalcony.unity`
   первой сценой. Она уже прописана в текущем Web-профиле.
3. Для публикации отключи Development Build и Autoconnect Profiler.
4. В Player Settings активного профиля → Publishing Settings выставь
   Compression Format: **Disabled**. Текущий Web-профиль уже использует этот режим.
   Он работает одинаково в Vite и Netlify без специальных заголовков сжатия.
   Gzip/Brotli можно подключить позже вместе с настройками сервера.
5. Нажми **Build** и выбери папку `web/public/unity` в этом репозитории.
   Результат должен лежать непосредственно в ней, без дополнительной вложенной папки:

```text
web/public/unity/
├── index.html
├── Build/
│   ├── *.loader.js
│   ├── *.framework.js
│   ├── *.data
│   └── *.wasm
├── TemplateData/
└── StreamingAssets/    # если Unity создала эту папку
```

Используй стандартный Web Template: Default. Экспортируй всю сборку,
а не только `Build/`. При обновлении удаляй старый экспорт перед новым билдом,
чтобы в Git не накапливались ненужные файлы.
Не открывай HTML через file://: запусти `npm run dev` из `web/`,
открой адрес Vite, введи `medok`. До экспорта `/play` покажет заглушку.
При пересборке Unity перезагрузи страницу `/play`.

### Netlify через GitHub

Корневой `netlify.toml` задаёт base `web`, сборку `npm ci && npm run build`
и publish `dist`. Маршрут `/play` переписывается на React HTML,
а файлы `/unity/` обслуживаются как статические ресурсы.
Подключи репозиторий в Netlify и используй этот конфиг без дополнительных
переопределений в UI. Добавь экспорт `web/public/unity/` в Git вместе с исходниками.
После push Netlify соберёт React и опубликует его вместе с готовой Unity.
Сам Unity Editor на Netlify не запускается; после изменения сцены нужно
повторить Unity Build и закоммитить обновлённый экспорт.
GitHub ограничивает размер отдельного файла: файлы больше 100 MiB нельзя
добавить обычным push. При достижении лимита перенеси доставку Unity в CI/хранилище
либо отдельно настрой Git LFS и его загрузку при деплое.

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
