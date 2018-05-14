var writeCSV = require('./writeCSV');

var customPropertyDefs = {

    writeToFile: function (qrsInteract, filePath) {
        qrsInteract.Get('custompropertydefinition')
            .then(function (result) {
                var dataMatrix = [];
                result.body.forEach(function (element) {
                    var dataRow = [];
                    dataRow.push(element['id']);
                    dataRow.push(element['name']);
                    dataRow.push(element['valueType']);
                    var optionsString = "";
                    element.choiceValues.forEach(function (choice) {
                        optionsString += choice + ";"
                    });
                    dataRow.push(optionsString);
                    dataMatrix.push(dataRow);
                }, this);
                writeCSV.writeDataToFile(filePath, dataMatrix);
            })
            .catch(function (error) {
                console.log(error);
            });
    }
}

module.exports = customPropertyDefs;