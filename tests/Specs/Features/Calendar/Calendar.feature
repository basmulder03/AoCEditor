Feature: Year calendar
  In order to see how far I am in Advent of Code
  As a user
  I want a calendar per year that shows which days are completed, partial or locked

  Background:
    Given I am signed in

  Scenario: See which days are completed, partial or locked
    Given the date is 5 December 2026
    And my calendar for 2026 has two stars on day 1 and one star on day 2
    When I open the calendar
    Then day 1 shows "Two stars"
    And day 2 shows "One star"
    And day 3 shows "Open"
    And day 8 shows "Locked"

  Scenario: Switch between years
    Given the date is 28 October 2026
    And my calendar for 2025 has two stars on day 1 and one star on day 2
    And my calendar for 2024 has one star on day 1
    When I open the calendar
    And I switch to 2024
    Then day 1 shows "One star"
    When I switch to 2020
    Then the calendar says "Not synced yet"

  Scenario: Sync a year and see progress
    Given the date is 28 October 2026
    And Advent of Code allows the next request in 30 seconds
    And Advent of Code has a calendar for 2025 with two stars on day 1
    When I open the calendar
    And I sync this year
    Then the progress mentioned 2025 and 30 seconds
    And the sync result says "1 year synced, 12 days updated."
    And day 1 shows "Two stars"
