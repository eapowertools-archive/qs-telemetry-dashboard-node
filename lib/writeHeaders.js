var config = require('../config/config');
var writeCSV = require('./writeCSV');
const path = require('path')

var writeHeaders = {

    writeAllHeaders: function(outputDir) {
        var appsFilename = path.join(outputDir, config.filenames.apps_table);
        var appsHeaders = ['AppID', 'AppName', 'IsPublished', 'PublishedDate', 'StreamID', 'StreamName'];
        writeCSV.writeHeadersToFile(appsFilename, appsHeaders);

        var sheetsFilename = path.join(outputDir, config.filenames.sheets_table);
        var sheetsHeaders = ['AppID', 'SheetID', 'SheetName', 'OwnerID', 'Published', 'Approved'];
        writeCSV.writeHeadersToFile(sheetsFilename, sheetsHeaders);

        var usersFilename = path.join(outputDir, config.filenames.users_table);
        var usersHeaders = ['ID', 'UserId', 'UserDirectory'];
        writeCSV.writeHeadersToFile(usersFilename, usersHeaders);

        var visualizationsFilename = path.join(outputDir, config.filenames.visualizations_table);
        var visualizationsHeaders = ['SheetID', 'VisualizationID', 'Type'];
        writeCSV.writeHeadersToFile(visualizationsFilename, visualizationsHeaders);

        var customPropertyDefinitionsFilename = path.join(outputDir, config.filenames.customPropertyDefinitions_table);
        var customPropertyDefinitionsHeaders = ['CustomPropertyDefinitionID', 'Name', 'Type', 'Values'];
        writeCSV.writeHeadersToFile(customPropertyDefinitionsFilename, customPropertyDefinitionsHeaders);

        var customPropertyMapFilename = path.join(outputDir, config.filenames.entityCustomPropertyMap_table);
        var customPropertyMapHeaders = ['EntityID', 'EntityType', 'CustomPropertyDefinitionID', 'Value'];
        writeCSV.writeHeadersToFile(customPropertyMapFilename, customPropertyMapHeaders);

        var outputStatusFilename = path.join(outputDir, config.filenames.outputStatus_table);
        var outputStatusHeaders = ['AppID', 'Timestamp', 'Status', 'Message'];
        writeCSV.writeHeadersToFile(outputStatusFilename, outputStatusHeaders);

        var logLevelFilename = path.join(outputDir, config.filenames.logLevel_table);
        var logLevelHeaders = ['NodeName', 'ServiceName', 'LogType', 'Level'];
        writeCSV.writeHeadersToFile(logLevelFilename, logLevelHeaders);

        var systemInfoFilename = path.join(outputDir, config.filenames.systemInfo_table);
        var systemInfoHeaders = ['NodeName', 'Key', 'Value'];
        writeCSV.writeHeadersToFile(systemInfoFilename, systemInfoHeaders);
    }
}

module.exports = writeHeaders;