var writeCSV = require('./writeCSV');

var appData = {
    writeToFile: function(qrsInteract, filePath) {
        return qrsInteract.Get("app")
            .then(function(result) {
                var dataMatrix = [];
                result.body.forEach(function(element) {
                    var dataRow = [];
                    dataRow.push(element['id']);
                    dataRow.push(element['name']);
                    if (!element['published']) {
                        dataRow.push(element['published']);
                        dataRow.push('');
                        dataRow.push('');
                        dataRow.push('');
                    } else {
                        dataRow.push(element['published']);
                        dataRow.push(element['publishTime']);
                        dataRow.push(element['stream']['id']);
                        dataRow.push(element['stream']['name']);
                    }
                    dataMatrix.push(dataRow);
                }, this);
                writeCSV.writeDataToFile(filePath, dataMatrix);
                // select IDs from dataMatrix
                return dataMatrix.map(i => i[0]);
            })
            .catch(function(error) {
                console.log(error);
            });
    }
}

module.exports = appData;