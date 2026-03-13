@bdd @lr4 @password-change
Feature: Planned password change
  Принудительная плановая смена пароля по истечении периода.

  Scenario: Система требует смену пароля после истечения срока
    Given открыт API приложения
    And зарегистрирован пользователь "fedorova@example.com" с паролем "fedorova"
    And для пользователя истек срок действия пароля
    When пользователь логинится с email "fedorova@example.com" и паролем "fedorova"
    Then код ответа равен 403
    And система сообщает что требуется смена пароля

  Scenario: После смены пароля доступ восстанавливается
    Given открыт API приложения
    And зарегистрирован пользователь "fedorova@example.com" с паролем "fedorova"
    And для пользователя истек срок действия пароля
    When пользователь меняет пароль на "fedorova-new"
    And пользователь логинится с email "fedorova@example.com" и паролем "fedorova-new"
    Then код ответа равен 200
