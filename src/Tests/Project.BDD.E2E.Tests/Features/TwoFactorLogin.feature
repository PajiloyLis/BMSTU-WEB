@bdd @lr4 @twofactor
Feature: Two-factor login
  Двухэтапная авторизация: пароль + OTP.

  Scenario: Логин запрашивает OTP после корректного пароля
    Given открыт API приложения
    And зарегистрирован пользователь "fedorova@example.com" с паролем "fedorova"
    When пользователь начинает вход с email "fedorova@example.com" и паролем "fedorova"
    Then код ответа равен 200
    And система сообщает что требуется OTP

  Scenario: Успешное завершение входа с валидным OTP
    Given открыт API приложения
    And зарегистрирован пользователь "fedorova@example.com" с паролем "fedorova"
    And пользователь начал вход и получил OTP challenge
    When пользователь подтверждает вход корректным OTP
    Then код ответа равен 200
    And возвращается JWT токен
