var writeCSV = require('./writeCSV');

var sheetData = {

    writeToFile: function (qrsInteract, filePath) {
        qrsInteract.Get("app/object/full?filter=objectType eq 'sheet'")
            .then(function (result) {
                var dataMatrix = [];
                result.body.forEach(function (element) {
                    var dataRow = [];
                    dataRow.push(element['app']['id']);
                    dataRow.push(element['engineObjectId']);
                    dataRow.push(element['name']);
                    dataRow.push(element['owner']['id']);
                    dataRow.push(element['published']);
                    dataRow.push(element['approved']);
                    dataMatrix.push(dataRow);
                }, this);
                writeCSV.writeDataToFile(filePath, dataMatrix);
            })
            .catch(function (error) {
                console.log(error);
            });
    }
}

module.exports = sheetData;