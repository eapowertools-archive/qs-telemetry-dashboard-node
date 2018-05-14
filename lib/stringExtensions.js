module.exports = function () {
    if (!String.prototype.startsWith) {
        String.prototype.startsWith = function (searchString, position) {
            position = position || 0;
            return this.substr(position, searchString.length) === searchString;
        };
    }

    String.prototype.replaceAll = function (search, replacement) {
        var target = this;
        return target.replace(new RegExp(search, 'g'), replacement);
    };
};