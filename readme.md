## Задачи по фичам
<details>
<summary>Ast</summary>
:white_check_mark: Тесты<br>
</details>
<details>
<summary>Native</summary>
:heavy_check_mark: Добавить все операторы(modulo)<br>
:heavy_check_mark: . Должна стать бинарным оператором доступа<br>
:heavy_check_mark: new - Expression<br>
:heavy_check_mark: assignExpression<br>
:heavy_check_mark: Другой синтаксис дженериков<br>
:heavy_check_mark: Добавление индексаторов<br>
:heavy_check_mark: Оператор каста<br>
:heavy_check_mark: Изменение порядка вызова метода<br>
:heavy_check_mark: Method chaining<br>
:heavy_check_mark: Поддержка var<br>
:white_check_mark: Тесты<br>
</details>
<details>
<summary>Compiler</summary>
:white_check_mark: Вынос компиляции<br>
:white_check_mark: Циклы for и while<br>
:white_check_mark: Операторы контроля потока<br>
:white_check_mark: Возможность вызова функций и рекурсии<br>
:white_check_mark: Тесты<br>
</details>
<details>
<summary>Assembly</summary>
:white_check_mark: Адекватная система сборок<br>
:white_check_mark: Наполнение стандартной библиотеки<br>
:white_check_mark: Тесты<br>
</details>
<details>
<summary>Validators</summary>
:white_check_mark: Вынос валидации дерева<br>
:white_check_mark: Циклы for и while<br>
:white_check_mark: Операторы контроля потока<br>
:white_check_mark: Возможность вызова функций и рекурсии<br>
:white_check_mark: Тесты<br>
</details>

## Родмап чуть дальше(Большие фичи, необходимые для мвп)
- Транслятор с джаста
- Самодокументируемость
 
## Дальний родмап(крупные версии)
- Режим дебага в интерпретаторе
- Поддержка property
    - Динамические модели(создание POCO типов в рантайме из скрипта)
1. Необратимая компиляция в низкоуровневое дерево перед компиляцией в лямбду
2. try catch
    - Интерполяция строк
    - Асинхронность <= финальный босс
- Упрощённый диалект для фронта
- Сборка из еврейского маппинга
- AOP (что-то типа аннотаций из джавы или декораторов из питона)
- Получение документации из summary
 
## Сайдквесты
- Тернарный Оператор
- Null access
- Null assign
- Null condition
- Lib leak(сожрать всё публичное апи любой сборки и добавить его к себе)
- Функциональщина в std
- Глобальные переменные для скрипта и создание контекста запуска (как в lua у редиса класс redis.Call)
- Управление загузкой и выгрузкой скомпилированных делегатов(нужно, чтобы экономить память, чтобы не было как в автомаппере)
 
## Мечты
- p-DE IDE для plamp
- тестирующий фреймворк
- атрибуты