@bdd @lr4 @recovery
Feature: Password recovery
  Восстановление доступа через recovery flow.

  Scenario: Пользователь запрашивает восстановление пароля
    Given открыт API приложения
    And зарегистрирован пользователь "fedorova@example.com" с паролем "fedorova"
    When пользователь запрашивает восстановление пароля для "fedorova@example.com"
    Then код ответа равен 202
    And отправлен recovery токен

  Scenario: Смена пароля через recovery токен
    Given открыт API приложения
    And зарегистрирован пользователь "fedorova@example.com" с паролем "fedorova"
    And для пользователя выпущен recovery токен
    When пользователь задает новый пароль через recovery токен
    Then код ответа равен 200
    And вход со старым паролем недоступен
    And вход с новым паролем доступен
