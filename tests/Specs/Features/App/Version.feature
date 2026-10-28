Feature: Application version
  In order to report problems and confirm updates
  As a user
  I want to know which version of the app I am running

  Scenario: The app reports its version
    When I ask for the app version
    Then I get a valid version number
