# Лобби с балконом

Сцена: `Assets/Scenes/LobbyBalcony.unity`.

Открой проект в Unity 6000.6.6f1, в окне Project дважды нажми на сцену,
перейди в Game и нажми Play. Для композиции без полей выбери соотношение 4:3.
Покачивание очень медленное: понаблюдай 15–30 секунд.
При других пропорциях камера показывает изображение целиком с тёмными полями,
не растягивая и не обрезая его. Пересоздавать сцену перед запуском не требуется.

## Слои и настройка

- `Outside - replace this group for a new location` — отдельное окружение за окном.
  Сейчас это плоскость с процедурным небом и облаками. Для смены локации замени
  содержимое этой группы. Плоскость находится на Z = 2, позади балкона на Z = 0;
  положительный Z направлен от камеры. Все плоскости имеют размер 13⅓ × 10.
- `Balcony artwork` — исходная PNG-картинка с прозрачными оконными проёмами.
  Unlit-материал сохраняет нарисованное освещение. PNG не редактировался:
  настроен только импорт (исходные размеры 1980 × 1485, sRGB, альфа, без сжатия
  и mipmaps, Clamp). Проёмы используют исходную прозрачность, дополнительной
  геометрической маски нет.
- `Soft morning light - painted surface overlay` — три мягкие полосы на правой
  стене и слабый рассеянный свет под подоконником. Это полупрозрачный аддитивный
  слой, подходящий для плоской картинки с уже нарисованным светом. Лучи
  ограничены стеной; картина защищена маской, оконные проёмы — альфой PNG.
  Это художественная имитация света, без физических источников и объёмного тумана.
- `Lobby Camera` — ортографическая камера с `LobbyCameraFit`, который сохраняет
  композицию при изменении размеров окна. Постобработка и HDR отключены только
  у этой камеры, чтобы не менять цвета исходного изображения.

В `Materials/MorningClouds.mat` доступны цвета, `Sway in image UV` и
`Sway period (seconds)` (по умолчанию 70 секунд). В `Materials/MorningLight.mat`
можно настроить тепло, силу и амплитуду света (период 65 секунд).
Анимация вычисляется шейдерами по времени Unity, дополнительных объектов
анимации и внешних ресурсов нет. Тестовые сцены, список сцен сборки, пакеты
и глобальные настройки рендеринга не изменялись.

## Файлы этой задачи

Созданы, вместе с соответствующими `.meta`:

- `Assets/Scenes/LobbyBalcony.unity`;
- `Assets/LobbyBalcony/Scripts/LobbyCameraFit.cs`;
- `Assets/LobbyBalcony/Editor/LobbyBalconySceneBuilder.cs`;
- `Assets/LobbyBalcony/Shaders/LobbyQuad.hlsl`;
- `Assets/LobbyBalcony/Shaders/BalconyUnlit.shader`;
- `Assets/LobbyBalcony/Shaders/MorningClouds.shader`;
- `Assets/LobbyBalcony/Shaders/MorningLight.shader`;
- `Assets/LobbyBalcony/Materials/Balcony.mat`;
- `Assets/LobbyBalcony/Materials/MorningClouds.mat`;
- `Assets/LobbyBalcony/Materials/MorningLight.mat`;
- `Assets/LobbyBalcony/README.md`;
- метаданные папок и `taspir_balcony_morning_transparent_1980px.png.meta`.

Ранее существовавшие файлы проекта и сам PNG не изменялись.

## Проверка

Проверено в Unity 6000.6.6f1, URP 17.6.0: компиляция C# и шейдеров,
рендеринг в профилях PC (Deferred) и Mobile (Forward) на текущем Mac,
кадры анимации на 0-й и 19-й секунде, форматы 4:3, 16:9 и 9:16.
Проверка Mobile здесь означает профиль качества, а не запуск на телефоне.

Меню `Taspir → Create Lobby Balcony` открывает сохранённую сцену; если файла
нет, создаёт её. Editor-скрипт не входит в сборку и не нужен для работы сцены.
Для повторной пакетной проверки существующей сцены:

```sh
Unity -batchmode -projectPath /path/to/Taspir \
  -executeMethod LobbyBalconySceneBuilder.Validate -quit -logFile /tmp/lobby.log
```

Метод `CreateAndValidate` сначала заново создаёт сцену; не используй его для
проверки сцены с ручными правками. QA-кадры сохраняются во временную папку
`TaspirLobbyQA`; полный путь выводится в лог при успешной проверке.
