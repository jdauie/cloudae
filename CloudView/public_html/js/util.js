
(function(JACERE) {
	
	THREE.Vector3.prototype.toString = function() {
		return String.format('[{0}]', this.toArray().map(function(n) {
			return +n.toFixed(2);
		}).join(', '));
	};

	THREE.Vector3.prototype.toStringLong = function() {
		return String.format('[{0}]', this.toArray().map(function(n) {
			return +n;
		}).join(', '));
	};

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
		//String.prototype.format = ...;
	}

	if (!Math.log2) {
		Math.log2 = function(x) {
			return Math.log(x) / Math.LN2;
		};
	}
	
	JACERE.Util = {
		
		createZeroArray: function(length) {
			var a, i;
			a = new Array(length);
			for (i = 0; i < length; ++i) {
				a[i] = 0;
			}
			return a;
		},

		clone: function(obj) {
			return JSON.parse(JSON.stringify(obj));
		},

		bytesToSize1: function(bytes) {
			var k = 1024;
			var sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
			if (bytes === 0) return '0 Bytes';
			var i = parseInt(Math.floor(Math.log(bytes) / Math.log(k)), 10);
			return (bytes / Math.pow(k, i)).toPrecision(3) + ' ' + sizes[i];
		},

		numberToCompactValue: function(n) {
			return JACERE.Util.numberToRep(n, 1000, 0, false, ['', 'k', 'm', 'b', 't']);
		},

		bytesToSizeInt: function(bytes) {
			return JACERE.Util.numberToRep(bytes, 1024, 0, true, ['B', 'KB', 'MB', 'GB', 'TB']);
		},

		bytesToSize: function(bytes) {
			return JACERE.Util.numberToRep(bytes, 1024, 3, true, ['B', 'KB', 'MB', 'GB', 'TB']);
		},

		numberToRep: function(n, k, p, s, sizes) {
			if (n === 0) return '0 ' + sizes[0];
			var i = ~~(Math.log(n) / Math.log(k));
			var r = (n / Math.pow(k, i));
			return ((p === 0) ? r : r.toPrecision(p)) + (s ? ' ' : '') + sizes[i];
		},

		createNamedSizes: function(startingSize, count) {
			var obj = {};
			for (var i = count - 1; i >= 0; i--) {
				var size = (startingSize << i);
				obj[JACERE.Util.bytesToSizeInt(size)] = size;
			}
			return obj;
		},

		createNamedMultiples: function(multiplier, values) {
			var obj = {};
			for (var i = values.length - 1; i >= 0; i--) {
				var value = (values[i] * multiplier);
				obj[JACERE.Util.numberToCompactValue(value)] = value;
			}
			return obj;
		}
		
	};
	
}(self.JACERE = self.JACERE || {}));
