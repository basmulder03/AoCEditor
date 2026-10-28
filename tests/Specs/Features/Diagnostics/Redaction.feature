Feature: Private data stays out of the logs
  In order to share diagnostics without leaking anything
  As a user
  I want secrets and personal folders removed from everything the app records

  Scenario: A session cookie never reaches the log
    When the app records a message containing my session cookie
    Then the recorded message hides the cookie
    And the log file hides the cookie

  Scenario: My home folder is shortened
    When the app records a message naming a file in my home folder
    Then the recorded message shows the file under a tilde

  Scenario: Copied diagnostics are clean
    When the app records a message containing my session cookie
    And I ask for the diagnostics
    Then the diagnostics hide the cookie
