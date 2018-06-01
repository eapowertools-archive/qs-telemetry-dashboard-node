# Qlik Sense Telemetry Dashboard

With the February 2018 of Qlik Sense, it is possible to capture granular usage metrics from the QIX in-memory engine based on configurable thresholds.  This provides the ability to capture CPU and RAM utilization of individual chart objects, CPU and RAM utilization of reload tasks, and more.

### Enable Telemetry Logging
*	In the Qlik Sense Management Console, navigate to Engines > choose an engine > Logging > QIX Performance log level.  Choose a value:
  * Off: No logging will occur  
  * Error: Activity meeting the ‘error’ threshold will be logged  
  * Warning: Activity meeting the ‘error’ and ‘warning’ thresholds will be logged  
  * Info: All activity will be logged
