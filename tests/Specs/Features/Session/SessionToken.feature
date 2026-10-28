Feature: Session token storage
  In order to stay signed in to Advent of Code
  As a puzzle solver
  I want my session token kept in the operating system secret store

  Scenario: A saved token is available later
    When I save the session token "synthetic-token"
    Then the app can read the session token "synthetic-token"

  Scenario: Signing out forgets the token
    Given I have saved the session token "synthetic-token"
    When I sign out
    Then the app has no session token

  Scenario: No secret service on this machine
    Given the system has no secret service
    When I try to save the session token "synthetic-token"
    Then I am told the secret store is unavailable
    And the app has no session token
