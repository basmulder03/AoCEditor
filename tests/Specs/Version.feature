Feature: Application version
  Scenario: The app reports its version
    When the app version is requested
    Then the version is a valid semantic version
