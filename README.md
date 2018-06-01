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
 
 
 ### Parameter Descriptions
 - **ErrorPeakMemory**: Default 2147483648 bytes (2 Gb).  If an engine operation requires more than this value of Peak Memory, a record is logged with log level ‘error’.  Peak Memory is the maximum, transient amount of RAM an operation uses.
 - **WarningPeakMemory**: Default 1073741824 bytes (1 Gb).  If an engine operation requires more than this value of Peak Memory, a record is logged with log level ‘warning’.  Peak Memory is the maximum, transient amount of RAM an operation uses.
 - **ErrorProcessTimeMs**: Default 60000 millisecond (60 seconds).  If an engine operation requires more than this value of process time, a record is logged with log level ‘error’.  Process Time is the end-to-end clock time of a request.
 - **WarningProcessTimeMs**: Default 30000 millisecond (30 seconds).  If an engine operation requires more than this value of process time, a record is logged with log level ‘warning’.  Process Time is the end-to-end clock time of a request.

Note that is possible to track _only_ process time or peak memory.  It is not required to track both metrics. However, if you set **ErrorPeakMemory**, you must set **WarningPeakMemory**. If you set **ErrorProcessTimeMs**, you must set **WarningProcessTimeMs**.


## Reading the logs
 - Telemetry data is logged to C:\ProgramData\Qlik\Sense\Log\Engine\Trace\<hostname>_QixPerformance_Engine.txt and rolls to the ArchiveLog folder in your ServiceCluster share.
 - In addition to the common fields found described here (http://help.qlik.com/en-US/sense/November2017/Subsystems/PlanningQlikSenseDeployments/Content/Deployment/Server-Logging-Tracing-Log-File-Format-Common-Fields.htm), fields relevent to telemetry are:
	 - **Level**: The logging level threshold the engine operation met.
	 - **ActiveUserId**: The User ID of the user performing the operation.
	 - **Method**: The engine operation itself. See _Important Engine Operations_ below for more.
	 - **DocId**: The ID of the Qlik application.
	 - **ObjectId**: For chart objects, the Object ID of chart object.
	 - **PeakRAM**: The maximum RAM an engine operation used.
	 - **NetRAM**: The net RAM an engine operation used. For hypercubes that support a chart object, the Net RAM is often lower than Peak RAM as temporary RAM can be used to perform set analysis, intermediate aggregations, and other calculations.
	 - **ProcessTime**: The end-to-end clock time for a request including internal engine operations to return the result.
	 - **WorkTime**: Effectively the same as ProcessTime excluding internal engine operations to return the result.  Will report very slightly shorter time than ProcessTime.
	 - **TraverseTime**: Time spent running the inference engine (i.e, the green, white, and grey).

### Important Engine Operations
