# Simple C# Shell (SCSS)

Минималистичная командная оболочка на C# с базовыми функциями и поддержкой перенаправления ввода-вывода

## Особенности

- 🛠 **Встроенные команды**: `echo`, `exit`, `type`, `pwd`, `cd`
- 🚀 **Запуск внешних программ** через поиск в PATH
- 🔄 **Перенаправление вывода**:
  - `>` и `>>` для stdout (создание и дополнение)
  - `2>` и `2>>` для stderr
- 📂 **Парсинг сложных команд** с поддержкой:
  - Кавычек (одинарных и двойных)
  - Экранирования символов (`\`)
  - Пробелов в аргументах

## Установка и запуск

1. Убедитесь, что установлен [.NET SDK 6+](https://dotnet.microsoft.com/download)
2. Скачайте исходники
3. Соберите и запустите:

```bash
dotnet build
dotnet run
```

## Использование
```
$ команда [аргументы] [операторы_перенаправления]
```
### Примеры
```
# Запись вывода в файл
$ echo "Hello World!" > greeting.txt

# Просмотр типа команды
$ type program.cs

# Запись ошибок в лог
$ ls -l 2> errors.log

# Добавление вывода в конец файла
$ pwd >> current_dir.txt
```

## Встроенные команды

### echo [text]

```
$ echo Привет $USER!
Привет John!
```
### exit 0 
Для выхода из программы

### type <команда>
```
$ type echo
echo is a shell builtin

$ type git
git is /usr/bin/git
```

### pwd
```
$ pwd
/home/user/projects
```

### cd [директория]
```
$ cd ~/documents  # Переход в домашнюю директорию
$ cd ..           # На уровень выше
```

## Перенаправление вывода
| Оператор | Действие                   |
|----------|----------------------------|
| >        | Перезаписать stdout в файл |
| >>       | Дописать stdout в файл     |
| 2>       | Перезаписать stderr в файл |
| 2>>      | Дописать stderr в файл     |

## Обработка ошибок
### Неверные команды:
```
$ invalid_cmd
invalid_cmd: not found
```
### Ошибки файловой системы:
```
$ cd /wrong/path
cd: /wrong/path: No such file or directory
```

## Ограничения
### ❌ Нет поддержки:
- Конвейеров (|)
- Фоновых процессов
- Переменных окружения (кроме ~)
- Подстановок (*.txt)
- Системы автозаполнения

### P.S. Архитектура и нормальное разделение по классам будет добавлено позже, на данном этапе проект полностью работоспособный
