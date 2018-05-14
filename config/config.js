var config = {
    global: {
        delimiter: '`',
        // hostname: 'desktop-gs8laa5.local',
        hostname: 'localhost',
        userDirectory: 'INTERNAL',
        userId: 'sa_api',
        // certificatesPath: '/Users/eps/Documents/Certificates'
        certificatesPath: 'C:/ProgramData/Qlik/Sense/Repository/Exported Certificates/.Local Certificates/'
    },
    engine: {
        port: 4747,
        schemaPath: 'enigma.js/schemas/12.20.0.json'
    },
    repository: {
        port: 4242
    },
    filenames: {
        outputDir: "./output/",
        variables_table: "variables.csv",
        masterMetrics_table: "masterMetrics.csv",
        apps_table: "apps.csv",
        sheets_table: "sheets.csv",
        users_table: "users.csv",
        visualizations_table: "visualizations.csv",
        metrics_table: "metrics.csv",
        visualizationsMasterMetrics_table: "visualizations_masterMetrics.csv",
        customPropertyDefinitions_table: "customPropertyDefinitions.csv",
        entityCustomPropertyMap_table: "entity_customProperty.csv",
        nonMasterMetrics_table: "nonMasterMetrics.csv"
    }
};

module.exports = config;