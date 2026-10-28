Feature: Debug mode
  In order to find out why something went wrong
  As a user
  I want to switch on detailed logging without losing it on restart

  Scenario: Detailed messages are hidden by default
    Given debug mode is off
    When the app records a detailed message
    Then the detailed message is not kept

  Scenario: Detailed messages are kept in debug mode
    Given debug mode is on
    When the app records a detailed message
    Then the detailed message is kept

  Scenario: The launch flag turns debug mode on
    Given the app is started with the debug flag
    Then debug mode is on

  Scenario: The choice survives a restart
    Given debug mode is off
    When I switch debug mode on
    And the app is restarted
    Then debug mode is on

  Scenario: Switching takes effect at once
    Given debug mode is off
    When I switch debug mode on
    And the app records a detailed message
    Then the detailed message is kept
