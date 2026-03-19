@bdd @auth @baseline
Feature: Authentication baseline
  Проверка базового контракта авторизации перед внедрением 2FA.

  Background:
    Given открыт API приложения
    And зарегистрирован пользователь "fedorova@example.com" с паролем "fedorova"

  Scenario: Успешный вход с корректными учетными данными
    When пользователь логинится с email "fedorova@example.com" и паролем "fedorova"
    Then код ответа равен 200
    And возвращается JWT токен

  Scenario: Ошибка при неверном пароле
    When пользователь логинится с email "fedorova@example.com" и паролем "wrong-password"
    Then код ответа равен 400
    And возвращается ошибка типа "InvalidPasswordException"
