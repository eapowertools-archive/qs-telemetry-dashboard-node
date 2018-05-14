var writeCSV = require('./writeCSV');

var userData = {

    writeToFile: function (qrsInteract, filePath) {
        qrsInteract.Get("user")
            .then(function (result) {
                var dataMatrix = [];
                result.body.forEach(function (element) {
                    var dataRow = [];
                    dataRow.push(element['id']);
                    dataRow.push(element['userId']);
                    dataRow.push(element['userDirectory']);
                    dataMatrix.push(dataRow);
                }, this);
                writeCSV.writeDataToFile(filePath, dataMatrix);
            })
            .catch(function (error) {
                console.log(error);
            });
    }
}

module.exports = userData;