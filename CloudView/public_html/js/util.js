if (!String.format) {
  String.format = function(format) {
    var args = Array.prototype.slice.call(arguments, 1);
    return format.replace(/{(\d+)}/g, function(match, number) { 
      return typeof args[number] !== 'undefined'
        ? args[number] 
        : match
      ;
    });
  };
  //String.prototype.format = String.format;
}

if (!Math.log2) {
	Math.log2 = function(x) {
		return Math.log(x) / Math.LN2;
	};
}

function createZeroArray(length) {
    var a, i;
    a = new Array(length);
    for (i = 0; i < length; ++i) {
        a[i] = 0;
    }
    return a;
}

