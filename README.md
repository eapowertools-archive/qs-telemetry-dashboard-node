# Qlik Sense Telemetry Dashboard
With the February 2018 of Qlik Sense, it is possible to capture granular usage metrics from the QIX in-memory engine based on configurable thresholds.  This provides the ability to capture CPU and RAM utilization of individual chart objects, CPU and RAM utilization of reload tasks, and more.

## Enable Telemetry Logging
 - In the Qlik Sense Management Console, navigate to Engines >  _choose an engine_ > Logging > QIX Performance log level.  Choose a value:
	 - **Off**: No logging will occur
	 - **Error:** Activity meeting the ‘error’ threshold will be logged
	 - **Warning**: Activity meeting the ‘error’ and ‘warning’ thresholds will be logged
	 - **Info**: All activity will be logged

	Note that log levels **Fatal** and **Debug** are not applicable in this scenario.

	Also note that the **Info** log level should be used only during troubleshooting as it can produce very large log files.  It is recommended during normal operations to use the Error or Warning settings.

 - Repeat for each engine for which telemetry should be enabled.


## Set Threshold Parameters

 - Edit C:\ProgramData\Qlik\Sense\Engine\Setings.ini.  If the file does not exist, create it.  You may need to open the file as an administrator to make changes.
 - Set the values below.  It is recommended to start with high threshold values and only decrease them as you become more aware of how your particular environment performs.  Too low of values will create very large log files.
    ```
    [Settings 7]
    ErrorPeakMemory=2147483648
    WarningPeakMemory=1073741824
    ErrorProcessTimeMs=60000
    WarningProcessTimeMs=30000
    ```
 - Save and close the file.
 - Restart the **Qlik Sense Engine Service** Windows service.
 - Repeat for each engine for which telemetry should be enabled.
