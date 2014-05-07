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

function clone(obj) {
	return JSON.parse(JSON.stringify(obj));
};

function bytesToSize1(bytes) {
	var k = 1024;
	var sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
	if (bytes === 0) return '0 Bytes';
	var i = parseInt(Math.floor(Math.log(bytes) / Math.log(k)), 10);
	return (bytes / Math.pow(k, i)).toPrecision(3) + ' ' + sizes[i];
}

function bytesToSize(bytes) {
	return numberToRep(bytes, 1024, ['B', 'KB', 'MB', 'GB', 'TB']);
}

function numberToRep(n, k, sizes) {
	if (n === 0) return '0 ' + sizes[0];
	var i = ~~Math.floor(Math.log(n) / Math.log(k));
	return (n / Math.pow(k, i)).toPrecision(3) + ' ' + sizes[i];
}

