var config = require('../config/config');
var writeCSV = require('./writeCSV');


var writeHeaders = {

    writeAllHeaders: function(outputDir) {
        var appsFilename = outputDir + config.filenames.apps_table;
        var appsHeaders = ['AppID', 'AppName', 'ModifiedDate', 'FileSize', 'LastReloadTime', 'OwnerID', 'IsPublished', 'PublishedDate', 'StreamID', 'StreamName'];
        writeCSV.writeHeadersToFile(appsFilename, appsHeaders);

        var sheetsFilename = outputDir + config.filenames.sheets_table;
        var sheetsHeaders = ['AppID', 'SheetID', 'SheetName', 'OwnerID', 'Published', 'Approved'];
        writeCSV.writeHeadersToFile(sheetsFilename, sheetsHeaders);

        var usersFilename = outputDir + config.filenames.users_table;
        var usersHeaders = ['ID', 'UserId', 'UserDirectory'];
        writeCSV.writeHeadersToFile(usersFilename, usersHeaders);

        var visualizationsFilename = outputDir + config.filenames.visualizations_table;
        var visualizationsHeaders = ['SheetID', 'VisualizationID', 'type'];
        writeCSV.writeHeadersToFile(visualizationsFilename, visualizationsHeaders);

        var customPropertyDefinitionsFilename = outputDir + config.filenames.customPropertyDefinitions_table;
        var customPropertyDefinitionsHeaders = ['CustomPropertyDefinitionID', 'Name', 'Type', 'Values'];
        writeCSV.writeHeadersToFile(customPropertyDefinitionsFilename, customPropertyDefinitionsHeaders);

        var customPropertyMapFilename = outputDir + config.filenames.entityCustomPropertyMap_table;
        var customPropertyMapHeaders = ['EntityID', 'EntityType', 'CustomPropertyDefinitionID', 'Value'];
        writeCSV.writeHeadersToFile(customPropertyMapFilename, customPropertyMapHeaders);

        var outputStatusFilename = outputDir + config.filenames.outputStatus_table;
        var outputStatusHeaders = ['AppID', 'Timestamp', 'Status', 'Message'];
        writeCSV.writeHeadersToFile(outputStatusFilename, outputStatusHeaders);

        var logLevelFilename = outputDir + config.filenames.logLevel_table;
        var logLevelHeaders = ['NodeName', 'ServiceName', 'LogType', 'Level'];
        writeCSV.writeHeadersToFile(logLevelFilename, logLevelHeaders);

        var systemInfoFilename = outputDir + config.filenames.systemInfo_table;
        var systemInfoHeaders = ['NodeName', 'Key', 'Value'];
        writeCSV.writeHeadersToFile(systemInfoFilename, systemInfoHeaders);
    }
}

module.exports = writeHeaders;