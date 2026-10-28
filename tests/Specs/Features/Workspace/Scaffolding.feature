Feature: Solution scaffolding
  In order to start a day without losing my work
  As a user
  I want the app to create solution files only when I ask and never overwrite mine

  Scenario: Starting a day creates the template
    Given I have chosen a solutions folder
    When I start day 3 of 2024 in "python"
    Then the solution folder for day 3 of 2024 in "python" holds 1 new file

  Scenario: Starting a day again keeps my edits
    Given I have chosen a solutions folder
    And I started day 3 of 2024 in "python" and edited the file
    When I start day 3 of 2024 in "python"
    Then the solution folder for day 3 of 2024 in "python" holds 0 new files
    And my edits are still there

  Scenario: The solutions folder cannot overlap the app data
    When I choose my app data folder as the solutions folder
    Then the solutions folder is refused
