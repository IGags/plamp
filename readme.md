## Задачи по фичам
<details>
<summary>Ast</summary>
:hammer_and_wrench: Тесты<br>
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
:hammer_and_wrench: Тесты<br>
</details>
<details>
<summary>Compiler</summary>
:hammer_and_wrench: Вынос компиляции<br>
:hammer_and_wrench: Циклы for и while<br>
:hammer_and_wrench: Операторы контроля потока<br>
:hammer_and_wrench: Возможность вызова функций и рекурсии<br>
:hammer_and_wrench: Тесты<br>
</details>
<details>
<summary>Assembly</summary>
:hammer_and_wrench: Адекватная система сборок<br>
:hammer_and_wrench: Наполнение стандартной библиотеки<br>
:hammer_and_wrench: Тесты<br>
</details>
<details>
<summary>Validators</summary>
:hammer_and_wrench: Вынос валидации дерева<br>
:hammer_and_wrench: Циклы for и while<br>
:hammer_and_wrench: Операторы контроля потока<br>
:hammer_and_wrench: Возможность вызова функций и рекурсии<br>
:hammer_and_wrench: Тесты<br>
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