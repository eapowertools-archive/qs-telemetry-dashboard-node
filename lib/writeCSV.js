var fs = require('fs');
var config = require('../config/config.js');

var writeCSV = {

    writeHeadersToFile: function (filename, headers) {
        var dataToWrite = "";
        headers.forEach(function (element) {
            dataToWrite += element + config.global.delimiter;
        }, this);
        dataToWrite = dataToWrite.substring(0, dataToWrite.length - 1);
        dataToWrite += "\n";

        fs.writeFileSync(filename, dataToWrite, {
            flag: 'w'
        }, function (err) {
            if (err) {
                return console.log(err);
            }
        });
        console.log(filename + " was saved.");
    },
    writeDataToFile: function (filename, data) {
        var dataToWrite = "";

        data.forEach(function (element) {
            element.forEach(function (element) {
                // remove linebreaks, linefeeds and tabs
                var cleanedElement = element;
                if (typeof cleanedElement == "string") {
                    cleanedElement = cleanedElement.replaceAll("\r\n", "");
                    cleanedElement = cleanedElement.replaceAll("\n", "\\n");
                    cleanedElement = cleanedElement.replaceAll("\t", "  ");
                }

                dataToWrite += cleanedElement + config.global.delimiter;
            }, this);
            dataToWrite = dataToWrite.substring(0, dataToWrite.length - 1);
            dataToWrite += "\n";
        }, this);

        fs.writeFileSync(filename, dataToWrite, {
            flag: 'a'
        }, function (err) {
            if (err) {
                return console.log(err);
            }
        });
        console.log(filename + " was saved.");
    }
}

module.exports = writeCSV;