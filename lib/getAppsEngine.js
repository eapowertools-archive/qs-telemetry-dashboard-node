const promise = require('bluebird');
var writeCSV = require('./writeCSV');

var appData = {
    writeToFile: function (appSession, appId, sessionObjectParams, vizPath, metricsPath) {
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

                                        return app.getObject({
                                            "qId": cellData['name']
                                        }).then((getObjectReturn) => {
                                            return getObjectReturn.getLayout().then((objectLayout) => {
                                                if (objectLayout['visualization'] == "filterpane") {
                                                    console.log("Found a filterpane, ignoring fields.");
                                                } else if (objectLayout['visualization'] == "ideviomap") {
                                                    console.log("Found a map, ignoring fields.");
                                                } else {
                                                    if (objectLayout['qHyperCube'] != undefined && objectLayout['qHyperCube']['qDimensionInfo'] != undefined) {
                                                        var dataMatrix = [];
                                                        for (var i = 0; i < objectLayout['qHyperCube']['qDimensionInfo'].length; i++) {
                                                            var dataRow = [];
                                                            dataRow.push(cellData['name']);
                                                            dataRow.push("dimension");
                                                            dataRow.push(objectLayout['qHyperCube']['qDimensionInfo'][i]['cId']);
                                                            dataRow.push("");
                                                            dataRow.push(objectLayout['qHyperCube']['qDimensionInfo'][i]['qFallbackTitle']);
                                                            dataMatrix.push(dataRow);
                                                        }
                                                        for (var i = 0; i < objectLayout['qHyperCube']['qMeasureInfo'].length; i++) {
                                                            var dataRow = [];
                                                            dataRow.push(cellData['name']);
                                                            dataRow.push("measure");
                                                            dataRow.push(objectLayout['qHyperCube']['qMeasureInfo'][i]['cId']);
                                                            dataRow.push("");
                                                            dataRow.push(objectLayout['qHyperCube']['qMeasureInfo'][i]['qFallbackTitle']);
                                                            dataMatrix.push(dataRow);
                                                        }
                                                        writeCSV.writeDataToFile(metricsPath, dataMatrix);
                                                    }
                                                }
                                            });
                                        });
                                    }
                                });
                            }
                        }).then(function () {
                            writeCSV.writeDataToFile(vizPath, vizDataMatrix);
                        });
                    });
                });
            });
        }).then(() => {
            appSession.close();
            console.log("Closed Session.");
        });
    }
}

module.exports = appData;