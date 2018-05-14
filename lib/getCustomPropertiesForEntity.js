var writeCSV = require('./writeCSV');

var customProperties = {

    writeToFile: function (qrsInteract, entityType, filePath) {
        qrsInteract.Get(entityType + "/full")
            .then(function (result) {
                var dataMatrix = [];
                result.body.forEach(function (element) {
                    var app = element;
                    app['customProperties'].forEach(function (customProp) {
                        var dataRow = [];
                        dataRow.push(app['id']);
                        dataRow.push(entityType);
                        dataRow.push(customProp['definition']['id']);
                        dataRow.push(customProp['value']);
                        dataMatrix.push(dataRow);
                    });
                }, this);
                writeCSV.writeDataToFile(filePath, dataMatrix);
            })
            .catch(function (error) {
                console.log(error);
            });
    }
}

module.exports = customProperties;