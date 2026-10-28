Feature: Manual calendar sync
  In order to see my progress on the calendar
  As a user
  I want to press Sync and have my stars fetched for every year of the event

  Background:
    Given I am signed in

  Scenario: Syncing shows my stars for every year
    Given the event ran in 2023 and 2024
    And I have two stars on day 1 and one star on day 2 of 2024
    And I have no stars in 2023
    When I sync my calendar
    Then 2 years were synced
    And day 1 of 2024 is complete
    And day 2 of 2024 is half done
    And day 3 of 2024 is open

  Scenario: Days that are not unlocked yet are locked
    Given today is 28 October 2026
    And the event ran in 2026
    When I sync my calendar
    Then day 1 of 2026 is locked

  Scenario: Syncing only asks for what the user requested
    Given the event ran in 2023 and 2024
    When I sync my calendar
    Then the event list was requested once
    And each year was requested once

  Scenario: A year that cannot be read does not stop the others
    Given the event ran in 2023 and 2024
    And Advent of Code shows an unexpected page for 2024
    When I sync my calendar
    Then 1 year was synced
    And 2024 is reported as unreadable

  Scenario: Not signed in
    Given I am not signed in
    When I sync my calendar
    Then I am told to sign in
