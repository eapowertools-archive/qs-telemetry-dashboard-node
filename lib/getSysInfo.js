var writeCSV = require('./writeCSV');
const os = require('os');

var systemData = {

    writeToFile: function (qrsInteract, filePath) {
        var dataMatrix = [];
        var dataRow = [];
        dataRow.push('localhost');
        dataRow.push('TotalMemory (GB)');
        dataRow.push((os.totalmem() / 1024 / 1024 / 1024));
        dataMatrix.push(dataRow);
        writeCSV.writeDataToFile(filePath, dataMatrix);

        qrsInteract.Get("engineservice/full")
            .then(function (result) {
                dataMatrix = [];
                result.body.forEach(function (element) {
                    var dataRow = [];
                    dataRow.push(element.serverNodeConfiguration['name']);
                    dataRow.push('Working Set Size Low (%)');
                    dataRow.push(element.settings['workingSetSizeLoPct']);
                    dataMatrix.push(dataRow);

                    dataRow = [];
                    dataRow.push(element.serverNodeConfiguration['name']);
                    dataRow.push('Working Set Size High (%)');
                    dataRow.push(element.settings['workingSetSizeHiPct']);
                    dataMatrix.push(dataRow);

                }, this);
                writeCSV.writeDataToFile(filePath, dataMatrix);
            })
            .catch(function (error) {
                console.log(error);
            });
    }
}

module.exports = systemData;