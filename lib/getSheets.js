var writeCSV = require('./writeCSV');

function getPage(instance, totalCount, dataSet, pageSize, startLocation) {
    if (startLocation >= totalCount) {
        return dataSet;
    } else {
        var path = "app/object/table?filter=objectType eq 'sheet'&skip=" + startLocation + "&take=" + pageSize;
        return instance.Post(path, {
                    columns: [{
                            columnType: "Property",
                            definition: "app.id",
                            name: "app.id"
                        },
                        {
                            columnType: "Property",
                            definition: "engineObjectId",
                            name: "engineObjectId"
                        },
                        {
                            columnType: "Property",
                            definition: "name",
                            name: "name"
                        },
                        {
                            columnType: "Property",
                            definition: "owner.id",
                            name: "owner.id"
                        },
                        {
                            columnType: "Property",
                            definition: "published",
                            name: "published"
                        },
                        {
                            columnType: "Property",
                            definition: "approved",
                            name: "approved"
                        }
                    ],
                    entity: "App.Object"
                },
                'json')
            .then(function(result) {
                result.body.rows.forEach(function(element) {
                    var dataRow = [];
                    dataRow.push(element[0]);
                    dataRow.push(element[1]);
                    dataRow.push(element[2]);
                    dataRow.push(element[3]);
                    dataRow.push(element[4]);
                    dataRow.push(element[5]);
                    dataSet.push(dataRow);
                }, this);
                return getPage(instance, totalCount, dataSet, pageSize, startLocation + pageSize);
            })
            .catch(function(error) {
                console.log(error);
            });
    }
}

var sheetData = {

    writeToFile: function(qrsInteract, filePath) {
        return qrsInteractInstance.Get("/app/object/count?filter=objectType eq 'sheet'")
            .then(function(countResult) {
                console.log(countResult.body["value"]);

                var dataMatrix = [];
                getPage(qrsInteractInstance, countResult.body["value"], dataMatrix, pageSize, 0).then(function(dataMatrix) {
                    writeCSV.writeDataToFile(filePath, dataMatrix);
                }).catch(function(error) {
                    console.log(error);
                });;
            });
    }
}

module.exports = sheetData;