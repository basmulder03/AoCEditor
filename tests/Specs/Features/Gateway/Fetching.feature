Feature: Considerate fetching from Advent of Code
  In order to stay within the Advent of Code rules
  As a user
  I want the app to reuse what it already downloaded and to space out its requests

  Background:
    Given I am signed in

  Scenario: Something already downloaded is not downloaded again
    Given I have already downloaded a page
    When I open that page
    Then I see the saved copy
    And Advent of Code was not contacted

  Scenario: Refreshing asks Advent of Code again
    Given I have already downloaded a page
    When I explicitly refresh that page
    Then Advent of Code was contacted once

  Scenario: A second request waits for the minimum spacing
    Given I have just made a request
    When I make another request straight away
    Then it is held back
    And it goes ahead after the minimum spacing has passed

  Scenario: Failed responses are not saved
    Given Advent of Code reports that the page does not exist
    When I open that page
    Then I am told it was not found
    And nothing is saved for it
