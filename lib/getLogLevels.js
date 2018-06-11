var writeCSV = require('./writeCSV');

function getLogLevelEnum(service, settingName, value) {
    if ((settingName.indexOf("AuditActivity") > -1 || settingName.indexOf("AuditSecurity") > -1) && service != "printing") {
        switch (value) {
            case 0:
                return "Off";
            case 1:
                return "Fatal";
            case 2:
                return "Error";
            case 3:
                return "Warning";
            case 4:
                return "Basic";
            case 5:
                return "Extended";
            default:
                return "AuditActivity-UnsupportedLogLevel";
        }
    } else if (settingName.indexOf("License") > -1) {
        switch (value) {
            case 0:
                return "Info";
            case 1:
                return "Debug";
            default:
                return "License-UnsupportedLogLevel";
        }
    } else {
        switch (value) {
            case 0:
                return "Off";
            case 1:
                return "Fatal";
            case 2:
                return "Error";
            case 3:
                return "Warning";
            case 4:
                return "Info";
            case 5:
                return "Debug";
            default:
                return "Service-UnsupportedLogLevel";
        }
    }
}

var logLevelData = {
    writeToFile: function (qrsInteract, filePath) {
        qrsInteract.Get("engineservice/full")
            .then(function (result) {
                var dataMatrix = [];
                result.body.forEach(function (element) {
                    for (var setting in element.settings) {
                        if (setting.indexOf("LogVerbosity") > -1) {
                            var dataRow = [];
                            dataRow.push(element.serverNodeConfiguration['name']);
                            dataRow.push('engine');
                            dataRow.push(setting);
                            dataRow.push(getLogLevelEnum('engine', setting, element.settings[setting]));
                            dataMatrix.push(dataRow);
                        }
                    }
                }, this);
                writeCSV.writeDataToFile(filePath, dataMatrix);
            })
            .catch(function (error) {
                console.log(error);
            });
    }
}

module.exports = logLevelData;