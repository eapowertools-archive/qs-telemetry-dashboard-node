const promise = require('bluebird');
var writeCSV = require('./writeCSV');

var appEngineData = {
    writeToFile: function(appSession, appId, sessionObjectParams, vizPath, metricsPath) {
        return appSession.open().then((global) => {
            console.log("creating a session");

            return global.openDoc(appId, "", "", "", true).then((app) => {
                console.log("opening a doc.");

                return app.createSessionObject(sessionObjectParams).then((sheetList) => {
                    console.log("Created session object.");
                    return sheetList.getLayout().then((layout) => {
                        var vizDataMatrix = [];

                        return promise.each(layout['qAppObjectList']['qItems'], (sheetObject) => {
                            if (sheetObject != undefined) {
                                return promise.each(sheetObject['qData']['cells'], (cellData) => {
                                    if (cellData != undefined) {
                                        vizDataMatrix.push([sheetObject['qInfo']['qId'], cellData['name'], cellData['type']]);
                                    }
                                    return;
                                });
                            }
                        }).then(function() {
                            writeCSV.writeDataToFile(vizPath, vizDataMatrix);
                        });
                    });
                });
            });
        }).then(() => {
            appSession.close();
            console.log("Closed Session.");
        }).catch(function(err) {
            appSession.close();
            console.log("Error connecting to app with id '" + appId + "'.");
            console.log("Error message: " + err);
            throw err;
        });
    }
}

module.exports = appEngineData;