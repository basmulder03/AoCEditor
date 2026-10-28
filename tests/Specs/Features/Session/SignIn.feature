Feature: Signing in to Advent of Code
  In order to sync my progress and fetch my puzzles
  As a puzzle solver
  I want to sign in once and have the app remember me

  Scenario: Signing in with the browser
    Given Advent of Code accepts my login
    When I sign in with the browser
    Then I am signed in
    And a token is stored

  Scenario: An expired remembered login does not end the sign-in
    Given Advent of Code accepts my login
    And the browser still remembers an old login that has expired
    When I sign in with the browser
    Then I am signed in
    And a token is stored

  Scenario: Leaving the sign-in window open for too long
    Given I do not finish signing in
    When 11 minutes pass with the sign-in window open
    Then the sign-in timed out
    And no token is stored

  Scenario: Closing the browser window
    When I close the sign-in browser
    Then the sign-in is cancelled
    And no token is stored

  Scenario: Pasting a token that Advent of Code rejects
    Given Advent of Code rejects my login
    When I sign in by pasting the token "synthetic-token"
    Then I am told the token is not valid
    And no token is stored

  Scenario: Pasting a token copied together with its name and stray spaces
    Given Advent of Code accepts my login
    When I sign in by pasting the token "  session=synthetic-token  "
    Then I am signed in

  Scenario: No secure storage on this machine
    Given this machine has no secure storage
    When I sign in by pasting the token "synthetic-token"
    Then I am told secure storage is unavailable
    And Advent of Code was not asked about the token

  Scenario: The embedded browser is not available
    Given the embedded browser is not available
    When I sign in with the browser
    Then I am offered to paste the token instead

  Scenario: Signing out
    Given Advent of Code accepts my login
    And I have signed in with the browser
    When I sign out of Advent of Code
    Then no token is stored
    And the browser remembers nothing about me

  Scenario: Several failures in a row ask for sign-in once
    Given I am no longer signed in
    When Advent of Code refuses three requests in a row
    Then I am asked to sign in once
