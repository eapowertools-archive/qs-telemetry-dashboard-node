const enigma = require('enigma.js');
const WebSocket = require('ws');
const path = require('path');
const fs = require('fs')
const promise = require('bluebird');
const qrsInteract = require('qrs-interact');

const config = require('./config/config.js');

// load libraries
const schema = require(config.engine.schemaPath);
const stringExtensions = require('./lib/stringExtensions');
const writeHeaders = require('./lib/writeHeaders');
const sysInfo = require('./lib/getSysInfo');
const customPropertyDefinitions = require('./lib/getCustomPropertyDefinitions');
const entityCustomPropertyValues = require('./lib/getCustomPropertiesForEntity');
const logLevels = require('./lib/getLogLevels');
const users = require('./lib/getUsers');
const sheets = require('./lib/getSheets');
const apps = require('./lib/getApps');
const appsEngine = require('./lib/getAppsEngine');
var writeCSV = require('./lib/writeCSV');

stringExtensions();

// repository connection setup
var qrsConfig = {
    hostname: config.global.hostname,
    certificates: {
        certFile: (path.isAbsolute(config.global.certificatesPath) ? config.global.certificatesPath : path.join(__dirname, config.global.certificatesPath)) + "client.pem",
        keyFile: (path.isAbsolute(config.global.certificatesPath) ? config.global.certificatesPath : path.join(__dirname, config.global.certificatesPath)) + "client_key.pem"
    }
}
var qrsInteractInstance = new qrsInteract(qrsConfig);


// Helper function to read the contents of the certificate files:
const readCert = filename => fs.readFileSync((path.isAbsolute(config.global.certificatesPath) ? config.global.certificatesPath : path.join(__dirname, config.global.certificatesPath)) + filename);

// App specific session func
function createSession(appId) {
    return enigma.create({
        schema,
        url: `wss://${config.global.hostname}:${config.engine.port}/app/${appId}`,
        createSocket: url => new WebSocket(url, {
            ca: [readCert('root.pem')],
            key: readCert('client_key.pem'),
            cert: readCert('client.pem'),
            headers: {
                'X-Qlik-User': `UserDirectory=${encodeURIComponent(config.global.userDirectory)}; UserId=${encodeURIComponent(config.global.userId)}`,
            },
            rejectUnauthorized: false
        }),
    });
}

// session app object to reuse later
var sessionObjectParams = {
    "qInfo": {
        "qType": "SheetList"
    },
    "qAppObjectListDef": {
        "qType": "sheet",
        "qData": {
            "title": "/qMetaDef/title",
            "description": "/qMetaDef/description",
            "thumbnail": "/thumbnail",
            "cells": "/cells",
            "rank": "/rank",
            "columns": "/columns",
            "rows": "/rows"
        }
    }
};

// Start making requests and building metadata files
// create output folder if it doesn't exist
try {
    fs.mkdirSync(config.filenames.outputDir);
} catch (err) {
    console.log("Output folder already created.");
}

// delete all files in folder
var files = fs.readdirSync(config.filenames.outputDir)
files.forEach(function (file) {
    fs.unlinkSync(path.join(config.filenames.outputDir, file));
});

writeHeaders.writeAllHeaders(config.filenames.outputDir);

var systemInfoPath = config.filenames.outputDir + config.filenames.systemInfo_table;
sysInfo.writeToFile(qrsInteractInstance, systemInfoPath);

var customPropertyDefinitionPath = config.filenames.outputDir + config.filenames.customPropertyDefinitions_table;
customPropertyDefinitions.writeToFile(qrsInteractInstance, customPropertyDefinitionPath);

var customPropertiesPath = config.filenames.outputDir + config.filenames.entityCustomPropertyMap_table;
entityCustomPropertyValues.writeToFile(qrsInteractInstance, "app", customPropertiesPath);

var logLevelsPath = config.filenames.outputDir + config.filenames.logLevel_table;
logLevels.writeToFile(qrsInteractInstance, logLevelsPath);

var usersPath = config.filenames.outputDir + config.filenames.users_table;
users.writeToFile(qrsInteractInstance, usersPath);

var sheetsPath = config.filenames.outputDir + config.filenames.sheets_table;
sheets.writeToFile(qrsInteractInstance, sheetsPath);

var appsPath = config.filenames.outputDir + config.filenames.apps_table;
apps.writeToFile(qrsInteractInstance, appsPath).then(function (ids) {
    var visualizationsPath = config.filenames.outputDir + config.filenames.visualizations_table;
    var metricsPath = config.filenames.outputDir + config.filenames.metrics_table;
    var dataMatrix = [];
    return promise.each(ids, (element, index) => {
        console.log("Getting data for app: " + element);
        var appSession = createSession(element);
        var dataRow = [];
        dataRow.push(element);
        dataRow.push(new Date().toJSON());
        return appsEngine.writeToFile(appSession, element, sessionObjectParams, visualizationsPath, metricsPath).then(function () {
            console.log("Done app " + (index + 1) + " of " + ids.length);
            dataRow.push("Success");
            dataRow.push("OK");
        }).catch(function (err) {
            dataRow.push("Fail");
            dataRow.push(err);
        }).then(function () {
            dataMatrix.push(dataRow);
        });
    }).then(function () {
        writeCSV.writeDataToFile(config.filenames.outputDir + config.filenames.outputStatus_table, dataMatrix);
    });
});