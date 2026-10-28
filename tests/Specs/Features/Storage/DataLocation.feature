Feature: App data location
  In order to keep puzzle text and inputs out of my public solutions repository
  As a user
  I want the app to refuse a data location that overlaps my solutions folder

  Scenario: App data location cannot be inside the solutions folder
    Given my solutions folder is "solutions"
    When I choose "solutions/data" as the app data location
    Then the app data location is refused

  Scenario: App data location outside the solutions folder is accepted
    Given my solutions folder is "solutions"
    When I choose "appdata" as the app data location
    Then the app data location is accepted
