Feature: Sandboxed runs
  In order to run code I did not write myself without risking my files
  As a user
  I want a solution to be confined to its own folder and unable to reach the network

  Background:
    Given a sandbox is available on this computer

  Scenario: A solution cannot read my other files
    Given a private file outside the solution folder
    When I run a solution that reads the private file
    Then the solution did not get the contents of the private file

  Scenario: A solution cannot change my files
    When I run a solution that writes next to itself
    Then no file was created

  Scenario: A solution cannot reach the network
    Given a server is listening on this computer
    When I run a solution that connects to the server
    Then the server saw no connection

  Scenario: A runaway solution is stopped
    When I run a solution that uses the processor forever
    Then the run ended within 10 seconds
