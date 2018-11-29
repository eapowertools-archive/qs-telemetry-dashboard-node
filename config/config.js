var config = {
    global: {
        delimiter: '`',
        hostname: 'localhost',
        userDirectory: 'INTERNAL',
        userId: 'sa_api',
        certificatesPath: './certs/'
    },
    engine: {
        port: 4747,
        schemaPath: 'enigma.js/schemas/12.20.0.json'
    },
    repository: {
        port: 4242
    },
    filenames: {
        outputDir: "outputFolderPlaceholder",
        outputStatus_table: "outputStatus.csv",
        apps_table: "apps.csv",
        sheets_table: "sheets.csv",
        users_table: "users.csv",
        visualizations_table: "visualizations.csv",
        metrics_table: "metrics.csv",
        customPropertyDefinitions_table: "customPropertyDefinitions.csv",
        entityCustomPropertyMap_table: "entity_customProperty.csv",
        logLevel_table: "logLevels.csv",
        systemInfo_table: "systemInfo.csv"
        // variables_table: "variables.csv",
        // masterMetrics_table: "masterMetrics.csv",
        // visualizationsMasterMetrics_table: "visualizations_masterMetrics.csv",
        // nonMasterMetrics_table: "nonMasterMetrics.csv",
    }
};

module.exports = config;